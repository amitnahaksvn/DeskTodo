using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Application.Settings;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backing view model for the always-visible desktop widget: a chosen day's
/// date and task list, with live completion progress, per-task CRUD
/// (create/rename/pin/archive/delete/duplicate — see <see cref="TaskItemViewModel"/>
/// for the per-row operations), the full-field editor hand-off,
/// drag-to-reorder, previous/today/next/calendar day navigation,
/// search/filter/sort/multi-select over the day's list, appearance
/// settings (accent color, background opacity, remembered window bounds),
/// and native notifications (overdue-task alerts, a once-daily summary).
/// </summary>
public sealed partial class WidgetViewModel : ViewModelBase, IDisposable
{
    private readonly ITaskService _taskService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProjectService _projectService;
    private readonly ITagService _tagService;
    private readonly ITaskTemplateService _templateService;
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WidgetViewModel> _logger;
    private readonly ILogger<TaskItemViewModel> _taskItemLogger;
    private readonly IUndoRedoService _undoRedoService;
    private readonly IDuplicateDetectionService _duplicateDetectionService;
    private readonly DispatcherTimer _dayRolloverTimer;

    // Tracks what "today" was as of the last check, separately from PlanDate (the day
    // currently being viewed) — see OnDayRolloverTick for why the two need to be distinct.
    private DateOnly _lastKnownToday;

    // Session-only (not persisted): which overdue tasks have already fired a notification,
    // which calendar day the "N tasks today" summary was last sent for, and which calendar
    // day overdue tasks were last auto-rescheduled for. All reset on app restart —
    // acceptable, since re-doing any of these once after a restart isn't harmful, and
    // persisting them would need its own storage for little benefit.
    private readonly HashSet<Guid> _notifiedOverdueTaskIds = [];
    private DateOnly? _lastDailySummaryDate;
    private DateOnly? _lastRescheduleCheckDate;

    // Phase 23's Break/Water/Stretch Reminders — cached from AppSettings on each
    // LoadSettingsAsync (not re-read from disk on every 30-second tick) alongside their own
    // session-only "when did this last fire" timestamps, same reasoning as the fields above.
    private bool _breakReminderEnabled;
    private int _breakReminderIntervalMinutes = 60;
    private DateTime? _lastBreakReminderAt;
    private bool _waterReminderEnabled;
    private int _waterReminderIntervalMinutes = 45;
    private DateTime? _lastWaterReminderAt;
    private bool _stretchReminderEnabled;
    private int _stretchReminderIntervalMinutes = 90;
    private DateTime? _lastStretchReminderAt;

    // Phase 26's Sound Notification setting — cached the same way as the reminder settings
    // above, not re-read from disk on every notification.
    private bool _notificationSoundEnabled = true;

    public WidgetViewModel(
        ITaskService taskService,
        ICategoryRepository categoryRepository,
        IProjectService projectService,
        ITagService tagService,
        ITaskTemplateService templateService,
        ISettingsService settingsService,
        INotificationService notificationService,
        TimeProvider timeProvider,
        ILogger<WidgetViewModel> logger,
        ILogger<TaskItemViewModel> taskItemLogger,
        IUndoRedoService undoRedoService,
        IDuplicateDetectionService duplicateDetectionService)
    {
        _taskService = taskService;
        _categoryRepository = categoryRepository;
        _projectService = projectService;
        _tagService = tagService;
        _templateService = templateService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
        _logger = logger;
        _taskItemLogger = taskItemLogger;
        _undoRedoService = undoRedoService;
        _duplicateDetectionService = duplicateDetectionService;
        _undoRedoService.StateChanged += (_, _) =>
        {
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        };

        _lastKnownToday = Today();
        PlanDate = _lastKnownToday;

        // Polls rather than scheduling a single timer for exactly midnight so a
        // sleeping/suspended machine still catches the rollover soon after waking. The same
        // timer also drives the overdue-task notification check (OnNotificationCheckTick)
        // and, as of Phase 23, the Break/Water/Stretch reminder check
        // (OnWellnessReminderCheckTick) — one 30-second poll, three independent Tick
        // subscribers, rather than a separate timer per "check periodically" need.
        _dayRolloverTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _dayRolloverTimer.Tick += OnDayRolloverTick;
        _dayRolloverTimer.Tick += OnNotificationCheckTick;
        _dayRolloverTimer.Tick += OnWellnessReminderCheckTick;
        _dayRolloverTimer.Start();

        // Deliberately not "_ = LoadTasksAsync();" here: constructors kicking off
        // fire-and-forget async work make callers (including tests) race against it.
        // WidgetWindow triggers the initial load explicitly on Opened instead.
    }

    public DateOnly PlanDate { get; private set; }

    public string DayOfWeekText => PlanDate.ToDateTime(TimeOnly.MinValue).ToString("dddd", CultureInfo.CurrentCulture);

    public string DateText => PlanDate.ToDateTime(TimeOnly.MinValue).ToString("d MMMM yyyy", CultureInfo.CurrentCulture);

    public bool IsToday => PlanDate == Today();

    // Every "what day is it right now" query in this class goes through this (and,
    // transitively, _timeProvider) rather than DateOnly.FromDateTime(DateTime.Now) directly,
    // so a test can fake "now" and exercise the midnight-rollover-follows-today logic
    // deterministically — see OnDayRolloverTick and WidgetViewModelTests.
    private DateOnly Today() => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    /// <summary>Bound (two-way) to the header's <c>CalendarDatePicker</c>; picking a date navigates to it.</summary>
    public DateTime? SelectedDate
    {
        get => PlanDate.ToDateTime(TimeOnly.MinValue);
        set
        {
            if (value.HasValue)
            {
                _ = NavigateToAsync(DateOnly.FromDateTime(value.Value));
            }
        }
    }

    [RelayCommand]
    private Task GoToPreviousDay() => NavigateToAsync(PlanDate.AddDays(-1));

    [RelayCommand]
    private Task GoToNextDay() => NavigateToAsync(PlanDate.AddDays(1));

    [RelayCommand]
    private Task GoToToday() => NavigateToAsync(Today());

    private async Task NavigateToAsync(DateOnly newDate)
    {
        if (newDate == PlanDate)
        {
            return;
        }

        PlanDate = newDate;
        OnPropertyChanged(nameof(PlanDate));
        OnPropertyChanged(nameof(DayOfWeekText));
        OnPropertyChanged(nameof(DateText));
        OnPropertyChanged(nameof(IsToday));
        OnPropertyChanged(nameof(EmptyStateText));
        OnPropertyChanged(nameof(SelectedDate));
        await LoadTasksAsync();
    }

    /// <summary>The full, unfiltered list of tasks for <see cref="PlanDate"/>, in DayOrder.</summary>
    public ObservableCollection<TaskItemViewModel> Tasks { get; } = [];

    /// <summary>What the row list actually binds to — <see cref="Tasks"/> after search/filter/sort. Equal to <see cref="Tasks"/>'s contents whenever no search/filter is active and sort is Manual.</summary>
    public ObservableCollection<TaskItemViewModel> VisibleTasks { get; } = [];

    public ObservableCollection<CategoryFilterOption> Categories { get; } = [CategoryFilterOption.All];

    public ObservableCollection<ProjectFilterOption> Projects { get; } = [ProjectFilterOption.All];

    public ObservableCollection<TagFilterOption> Tags { get; } = [TagFilterOption.All];

    /// <summary>Saved templates for the "New from template" picker — a plain ComboBox rather than a separate picker window, since selecting an item is the whole interaction.</summary>
    public ObservableCollection<TemplateOption> Templates { get; } = [];

    public IReadOnlyList<TaskStatusFilter> StatusFilters { get; } = Enum.GetValues<TaskStatusFilter>();

    public IReadOnlyList<TaskSortOption> SortOptions { get; } = Enum.GetValues<TaskSortOption>();

    [ObservableProperty]
    public partial bool IsSearchBarVisible { get; set; }

    [ObservableProperty]
    public partial bool IsSelectMode { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial TaskStatusFilter SelectedStatusFilter { get; set; } = TaskStatusFilter.All;

    [ObservableProperty]
    public partial TaskSortOption SelectedSortOption { get; set; } = TaskSortOption.Manual;

    [ObservableProperty]
    public partial CategoryFilterOption SelectedCategoryFilter { get; set; } = CategoryFilterOption.All;

    [ObservableProperty]
    public partial ProjectFilterOption SelectedProjectFilter { get; set; } = ProjectFilterOption.All;

    [ObservableProperty]
    public partial TagFilterOption SelectedTagFilter { get; set; } = TagFilterOption.All;

    /// <summary>Null = no selection (the ComboBox's placeholder state). Picking a template creates a task from it, then this resets to null so the same template can be re-picked later.</summary>
    [ObservableProperty]
    public partial TemplateOption? SelectedTemplateToApply { get; set; }

    // These On<Property>Changed hooks are safe from the constructor-persistence footgun
    // TaskItemViewModel's doc comments warn about: property *initializers* (the "= ..."
    // above) set the backing field directly and don't invoke the setter, so they never
    // fire from construction — only genuine later changes (typing in the search box,
    // picking a dropdown value) do.
    partial void OnSearchTextChanged(string value) => RefreshVisibleTasks();

    partial void OnSelectedStatusFilterChanged(TaskStatusFilter value) => RefreshVisibleTasks();

    partial void OnSelectedSortOptionChanged(TaskSortOption value) => RefreshVisibleTasks();

    partial void OnSelectedCategoryFilterChanged(CategoryFilterOption value) => RefreshVisibleTasks();

    partial void OnSelectedProjectFilterChanged(ProjectFilterOption value) => RefreshVisibleTasks();

    partial void OnSelectedTagFilterChanged(TagFilterOption value) => RefreshVisibleTasks();

    partial void OnSelectedTemplateToApplyChanged(TemplateOption? value)
    {
        if (value is not null)
        {
            _ = CreateTaskFromTemplateAsync(value);
        }
    }

    private async Task CreateTaskFromTemplateAsync(TemplateOption template)
    {
        try
        {
            await _templateService.CreateTaskFromTemplateAsync(template.Id, PlanDate);
            await LoadTasksAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create a task from template {TemplateId}", template.Id);
        }
        finally
        {
            SelectedTemplateToApply = null;
        }
    }

    [RelayCommand]
    private void ToggleSearchBar() => IsSearchBarVisible = !IsSearchBarVisible;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressPercentage))]
    [NotifyPropertyChangedFor(nameof(ProgressSummaryText))]
    public partial int CompletedCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressPercentage))]
    [NotifyPropertyChangedFor(nameof(ProgressSummaryText))]
    public partial int TotalCount { get; set; }

    public int ProgressPercentage => TotalCount == 0 ? 0 : (int)Math.Round(CompletedCount * 100.0 / TotalCount);

    public string ProgressSummaryText => $"{CompletedCount} / {TotalCount} Completed";

    public bool HasNoTasks => !IsLoading && VisibleTasks.Count == 0;

    public string EmptyStateText => Tasks.Count > 0
        ? "No matching tasks"
        : IsToday ? "No tasks for today" : "No tasks for this day";

    /// <summary>
    /// Raised when a row's context menu "Edit" is chosen. WidgetWindow owns
    /// actually showing the editor window — a ViewModel shouldn't construct
    /// Views — so this just bubbles the request up.
    /// </summary>
    public event EventHandler<Guid>? TaskEditRequested;

    private const int MaxRecentlyViewed = 5;

    /// <summary>
    /// The last few tasks opened in the full-field editor, most-recent-first — shown as a
    /// chip row in the search bar. Deliberately session-only (reset on restart, like
    /// <c>_notifiedOverdueTaskIds</c> above): re-showing an empty list once after a restart
    /// isn't harmful, and persisting it would need its own storage for very little benefit.
    /// </summary>
    public ObservableCollection<RecentTaskOption> RecentlyViewed { get; } = [];

    private void RequestTaskEdit(Guid taskId, string title)
    {
        var existing = RecentlyViewed.FirstOrDefault(r => r.Id == taskId);
        if (existing is not null)
        {
            RecentlyViewed.Remove(existing);
        }

        RecentlyViewed.Insert(0, new RecentTaskOption(taskId, title, option => RequestTaskEdit(option.Id, option.Title)));
        while (RecentlyViewed.Count > MaxRecentlyViewed)
        {
            RecentlyViewed.RemoveAt(RecentlyViewed.Count - 1);
        }

        TaskEditRequested?.Invoke(this, taskId);
    }

    /// <summary>
    /// Phase 35's Drag File/Drag Browser Tab to Create Task — called by <c>WidgetWindow</c>'s
    /// new external-drop handler (a file from Finder/Explorer, or a URL from a browser)
    /// after it's already determined the drop is external, not the existing in-window
    /// row-reorder drag. Returns the created task so the caller can attach a dropped file to
    /// it (see <c>IAttachmentService</c>) — this method itself has no attachment dependency,
    /// matching <c>TaskEditWindow.OnAttachmentOpenRequested</c>'s "resolve platform/storage
    /// services directly in code-behind" pattern rather than growing this ViewModel's
    /// constructor for a File/browser-drop-only need.
    /// </summary>
    public async Task<TaskItem> CreateTaskFromDropAsync(string title, string? description = null, CancellationToken cancellationToken = default)
    {
        var task = await _taskService.CreateTaskAsync(PlanDate, title, description, cancellationToken: cancellationToken);
        await LoadTasksAsync(cancellationToken);
        return task;
    }

    [ObservableProperty]
    public partial string NewTaskTitle { get; set; } = string.Empty;

    /// <summary>
    /// Feature 47's Smart Duplicate Detection — set right after a task is added if a likely
    /// duplicate was found among that day's existing tasks, cleared on the next add. A
    /// non-blocking notice, not the spec's own "Use Existing / Create Anyway" modal: the task
    /// is always created (Enter-to-add is this app's fastest, most-used path, and gating it
    /// behind a dialog on every merely-similar title would slow down the common case far more
    /// than the occasional true duplicate costs), and this message just flags what to go clean
    /// up if it *was* one.
    /// </summary>
    [ObservableProperty]
    public partial string? DuplicateWarningMessage { get; set; }

    /// <summary>Bound to the "add task" row's Enter key. A blank title is a no-op rather than an error.</summary>
    [RelayCommand]
    private async Task AddTaskAsync()
    {
        var title = NewTaskTitle.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        DuplicateWarningMessage = null;

        try
        {
            var existingTasks = await _taskService.GetTasksForDateAsync(PlanDate);
            var duplicates = _duplicateDetectionService.FindPossibleDuplicates(title, PlanDate, categoryId: null, existingTasks.Where(t => !t.IsCompleted));

            await _taskService.CreateTaskAsync(PlanDate, title);
            NewTaskTitle = string.Empty;
            await LoadTasksAsync();

            if (duplicates.Count > 0)
            {
                DuplicateWarningMessage = $"Possible duplicate of \"{duplicates[0].Task.Title}\"";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add task '{Title}' for {PlanDate}", title, PlanDate);
        }
    }

    public async Task LoadTasksAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        OnPropertyChanged(nameof(HasNoTasks));

        try
        {
            await RefreshCategoriesAsync(cancellationToken);
            await RefreshProjectsAsync(cancellationToken);
            await RefreshTagsAsync(cancellationToken);
            await RefreshTemplatesAsync(cancellationToken);
            await MaybeRescheduleOverdueTasksAsync(cancellationToken);

            var tasks = await _taskService.GetTasksForDateAsync(PlanDate, cancellationToken);

            foreach (var existing in Tasks)
            {
                existing.PropertyChanged -= OnTaskItemPropertyChanged;
            }

            Tasks.Clear();
            foreach (var task in tasks)
            {
                var itemViewModel = new TaskItemViewModel(task, _taskService, _taskItemLogger, () => _ = LoadTasksAsync(), id => RequestTaskEdit(id, task.Title), _undoRedoService)
                {
                    IsSelectModeActive = IsSelectMode,
                };
                itemViewModel.PropertyChanged += OnTaskItemPropertyChanged;
                Tasks.Add(itemViewModel);
            }

            UpdateProgress();
            RefreshVisibleTasks();
            await MaybeSendDailySummaryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tasks for {PlanDate}", PlanDate);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoTasks));
        }
    }

    /// <summary>
    /// Updates <see cref="Categories"/> in place (add missing / remove stale / rename
    /// changed) rather than <c>Clear()</c>-then-rebuild. A <c>Clear()</c> momentarily
    /// removes the currently-selected item from the bound collection, which desyncs the
    /// search bar's category <c>ComboBox</c>'s two-way <c>SelectedItem</c> binding —
    /// Avalonia leaves it on "nothing selected" even once the list is repopulated with an
    /// equal item a moment later (caught via headless render testing: the box rendered
    /// with no text, even though <c>SelectedCategoryFilter</c> was correctly set again by
    /// the time this method returned — the ComboBox's own desync happened after).
    /// </summary>
    private async Task RefreshCategoriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            var previousSelectionId = SelectedCategoryFilter.Id;

            var desired = new List<CategoryFilterOption> { CategoryFilterOption.All };
            desired.AddRange(categories
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Select(c => new CategoryFilterOption(c.Id, c.Name)));

            foreach (var item in desired)
            {
                if (!Categories.Any(c => c.Id == item.Id))
                {
                    Categories.Add(item);
                }
            }

            for (var i = Categories.Count - 1; i >= 0; i--)
            {
                if (!desired.Any(d => d.Id == Categories[i].Id))
                {
                    Categories.RemoveAt(i);
                }
            }

            for (var i = 0; i < Categories.Count; i++)
            {
                var renamed = desired.FirstOrDefault(d => d.Id == Categories[i].Id && d.Name != Categories[i].Name);
                if (renamed is not null)
                {
                    Categories[i] = renamed;
                }
            }

            // Keep the current filter selection if it still exists (e.g. wasn't deleted), else reset to All.
            SelectedCategoryFilter = Categories.FirstOrDefault(c => c.Id == previousSelectionId) ?? CategoryFilterOption.All;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load categories for the filter dropdown");
        }
    }

    /// <summary>Same in-place-update reasoning as <see cref="RefreshCategoriesAsync"/> — see its doc comment. Archived projects are excluded (same as the "no longer usable" reasoning below for a deleted category), except one already selected as the active filter.</summary>
    private async Task RefreshProjectsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var projects = await _projectService.GetProjectsAsync(cancellationToken);
            var previousSelectionId = SelectedProjectFilter.Id;

            var desired = new List<ProjectFilterOption> { ProjectFilterOption.All };
            desired.AddRange(projects
                .Where(p => !p.IsArchived || p.Id == previousSelectionId)
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Select(p => new ProjectFilterOption(p.Id, p.Name)));

            foreach (var item in desired)
            {
                if (!Projects.Any(p => p.Id == item.Id))
                {
                    Projects.Add(item);
                }
            }

            for (var i = Projects.Count - 1; i >= 0; i--)
            {
                if (!desired.Any(d => d.Id == Projects[i].Id))
                {
                    Projects.RemoveAt(i);
                }
            }

            for (var i = 0; i < Projects.Count; i++)
            {
                var renamed = desired.FirstOrDefault(d => d.Id == Projects[i].Id && d.Name != Projects[i].Name);
                if (renamed is not null)
                {
                    Projects[i] = renamed;
                }
            }

            SelectedProjectFilter = Projects.FirstOrDefault(p => p.Id == previousSelectionId) ?? ProjectFilterOption.All;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load projects for the filter dropdown");
        }
    }

    /// <summary>Same in-place-update reasoning as <see cref="RefreshCategoriesAsync"/> — see its doc comment.</summary>
    private async Task RefreshTagsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tags = await _tagService.GetAllTagsAsync(cancellationToken);
            var previousSelectionId = SelectedTagFilter.Id;

            var desired = new List<TagFilterOption> { TagFilterOption.All };
            desired.AddRange(tags
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .Select(t => new TagFilterOption(t.Id, t.Name)));

            foreach (var item in desired)
            {
                if (!Tags.Any(t => t.Id == item.Id))
                {
                    Tags.Add(item);
                }
            }

            for (var i = Tags.Count - 1; i >= 0; i--)
            {
                if (!desired.Any(d => d.Id == Tags[i].Id))
                {
                    Tags.RemoveAt(i);
                }
            }

            for (var i = 0; i < Tags.Count; i++)
            {
                var renamed = desired.FirstOrDefault(d => d.Id == Tags[i].Id && d.Name != Tags[i].Name);
                if (renamed is not null)
                {
                    Tags[i] = renamed;
                }
            }

            SelectedTagFilter = Tags.FirstOrDefault(t => t.Id == previousSelectionId) ?? TagFilterOption.All;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tags for the filter dropdown");
        }
    }

    private async Task RefreshTemplatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var templates = await _templateService.GetTemplatesAsync(cancellationToken);

            Templates.Clear();
            foreach (var template in templates)
            {
                Templates.Add(new TemplateOption(template.Id, template.Name));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load templates for the \"New from template\" picker");
        }
    }

    /// <summary>
    /// Only runs while actually viewing today (browsing a past/future day to plan ahead
    /// shouldn't trigger it) and only once per calendar day the app stays open — same
    /// guard shape as <see cref="MaybeSendDailySummaryAsync"/>. Runs before this method's
    /// caller (<see cref="LoadTasksAsync"/>) fetches today's list, so any just-rescheduled
    /// tasks show up immediately rather than needing a second reload.
    /// </summary>
    private async Task MaybeRescheduleOverdueTasksAsync(CancellationToken cancellationToken)
    {
        if (!AutoRescheduleOverdueTasks || PlanDate != Today() || _lastRescheduleCheckDate == PlanDate)
        {
            return;
        }

        _lastRescheduleCheckDate = PlanDate;

        try
        {
            var movedCount = await _taskService.RescheduleOverdueTasksAsync(PlanDate, cancellationToken);
            if (movedCount > 0)
            {
                _logger.LogInformation("Auto-rescheduled {Count} overdue task(s) onto {PlanDate}", movedCount, PlanDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-reschedule overdue tasks");
        }
    }

    /// <summary>Recomputes <see cref="VisibleTasks"/> from <see cref="Tasks"/> using the current search text, filters and sort — called whenever any of those change, or the underlying task list reloads.</summary>
    private void RefreshVisibleTasks()
    {
        IEnumerable<TaskItemViewModel> query = Tasks;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            query = query.Where(t =>
                t.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (t.Notes?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        query = SelectedStatusFilter switch
        {
            TaskStatusFilter.Active => query.Where(t => !t.IsCompleted),
            TaskStatusFilter.Completed => query.Where(t => t.IsCompleted),
            _ => query,
        };

        if (SelectedCategoryFilter.Id.HasValue)
        {
            var categoryId = SelectedCategoryFilter.Id.Value;
            query = query.Where(t => t.CategoryId == categoryId);
        }

        if (SelectedProjectFilter.Id.HasValue)
        {
            var projectId = SelectedProjectFilter.Id.Value;
            query = query.Where(t => t.ProjectId == projectId);
        }

        if (SelectedTagFilter.Id.HasValue)
        {
            var tagId = SelectedTagFilter.Id.Value;
            query = query.Where(t => t.TagIds.Contains(tagId));
        }

        query = SelectedSortOption switch
        {
            TaskSortOption.Priority => query.OrderByDescending(t => t.Priority),
            TaskSortOption.DueDate => query.OrderBy(t => t.DueDate ?? DateTime.MaxValue),
            TaskSortOption.Title => query.OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase),
            // "Group by category": same-category tasks cluster together since equal sort keys
            // preserve their relative order; uncategorized tasks (null) sort last.
            TaskSortOption.Category => query.OrderBy(t => t.CategoryName is null).ThenBy(t => t.CategoryName, StringComparer.OrdinalIgnoreCase),
            // Manual: Tasks is already DayOrder-ordered and Where() preserves source order, so no explicit sort is needed.
            _ => query,
        };

        VisibleTasks.Clear();
        foreach (var task in query)
        {
            VisibleTasks.Add(task);
        }

        OnPropertyChanged(nameof(HasNoTasks));
        OnPropertyChanged(nameof(EmptyStateText));
    }

    private void OnTaskItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(TaskItemViewModel.IsCompleted):
                UpdateProgress();
                RefreshVisibleTasks(); // completion can move a row in/out of the Active/Completed filter
                break;
            case nameof(TaskItemViewModel.IsSelected):
                OnPropertyChanged(nameof(SelectedCount));
                OnPropertyChanged(nameof(HasSelection));
                break;
        }
    }

    private void UpdateProgress()
    {
        TotalCount = Tasks.Count;
        CompletedCount = Tasks.Count(t => t.IsCompleted);
    }

    public int SelectedCount => Tasks.Count(t => t.IsSelected);

    public bool HasSelection => SelectedCount > 0;

    [RelayCommand]
    private void ToggleSelectMode()
    {
        IsSelectMode = !IsSelectMode;
        foreach (var task in Tasks)
        {
            task.IsSelectModeActive = IsSelectMode;
            if (!IsSelectMode)
            {
                task.IsSelected = false;
            }
        }
    }

    [RelayCommand]
    private void SelectAllVisible()
    {
        foreach (var task in VisibleTasks)
        {
            task.IsSelected = true;
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var task in Tasks)
        {
            task.IsSelected = false;
        }
    }

    /// <summary>Completes every selected, not-yet-completed task (reuses each row's own <c>ToggleCompleteCommand</c> rather than duplicating its persistence logic).</summary>
    [RelayCommand]
    private async Task BulkCompleteAsync()
    {
        var incompleteSelected = Tasks.Where(t => t.IsSelected && !t.IsCompleted).ToList();
        foreach (var task in incompleteSelected)
        {
            await task.ToggleCompleteCommand.ExecuteAsync(null);
        }

        // Not via ToggleSelectModeCommand: this needs the same "cascade to existing rows"
        // behavior, but without re-flipping IsSelectMode back on.
        IsSelectMode = false;
        foreach (var task in Tasks)
        {
            task.IsSelectModeActive = false;
            task.IsSelected = false;
        }
    }

    [RelayCommand]
    private async Task BulkDeleteAsync()
    {
        var selectedIds = Tasks.Where(t => t.IsSelected).Select(t => t.Id).ToList();
        if (selectedIds.Count == 0)
        {
            return;
        }

        try
        {
            foreach (var id in selectedIds)
            {
                await _taskService.DeleteTaskAsync(id);
            }

            IsSelectMode = false;
            await LoadTasksAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk-delete {Count} selected tasks", selectedIds.Count);
        }
    }

    [ObservableProperty]
    public partial string AccentColorHex { get; set; } = "#3B82F6";

    /// <summary>See <see cref="Application.Settings.AppSettings.Theme"/> — "System"/"Light"/"Dark". Applied at the <c>Application</c> level by <c>App.ApplyTheme</c>, not read directly by any View; kept here only so it round-trips through <see cref="LoadSettingsAsync"/> the same way every other setting does.</summary>
    [ObservableProperty]
    public partial string Theme { get; set; } = "System";

    /// <summary>
    /// Whether the *actual*, resolved theme (after "System" is settled against the real OS
    /// appearance) is dark — set by <c>WidgetWindow</c> from its own <c>ActualThemeVariant</c>
    /// after <c>App.ApplyTheme</c> runs, since resolving "System" needs a live Avalonia
    /// <c>TopLevel</c> this ViewModel deliberately doesn't depend on. Exists only to pick
    /// <see cref="WidgetBackgroundHex"/>'s blend base — every other themed color in the app
    /// comes from <c>DynamicResource</c> lookups in XAML, which don't need this at all.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WidgetBackgroundHex))]
    public partial bool IsDarkTheme { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WidgetBackgroundHex))]
    public partial double WidgetOpacity { get; set; } = 0.95;

    /// <summary>"#AARRGGBB" — <see cref="WidgetOpacity"/> blended into an otherwise white (light theme) or dark-slate (dark theme) background, bound to the widget's outer <c>Border.Background</c>. Text stays fully opaque; only the card behind it fades.</summary>
    public string WidgetBackgroundHex => $"#{(byte)Math.Round(WidgetOpacity * 255):X2}{(IsDarkTheme ? "1E293B" : "FFFFFF")}";

    /// <summary>Last-known window bounds from settings — null (all four) means "use the built-in default." Read once by <c>WidgetWindow</c> after <see cref="LoadSettingsAsync"/> to restore position/size; the accent color is applied separately since that's an <c>Application.Resources</c> side effect, not a per-window one.</summary>
    public double? WindowLeft { get; private set; }

    public double? WindowTop { get; private set; }

    public double? WindowWidth { get; private set; }

    public double? WindowHeight { get; private set; }

    /// <summary>See <see cref="Application.Settings.AppSettings.PreferredMonitorId"/>. Read once by <c>App.axaml.cs</c> at startup to place the widget on the chosen monitor instead of the raw <see cref="WindowLeft"/>/<see cref="WindowTop"/> coordinates, which can go stale if the monitor arrangement changed since they were saved.</summary>
    public string? PreferredMonitorId { get; private set; }

    [ObservableProperty]
    public partial bool NotificationsEnabled { get; set; } = true;

    /// <summary>Bound to <c>WidgetWindow</c>'s own <c>ShowInTaskbar</c> — see <see cref="Application.Settings.AppSettings.ShowInTaskbar"/> for why this defaults to <c>true</c>.</summary>
    [ObservableProperty]
    public partial bool ShowInTaskbar { get; set; } = true;

    /// <summary>See <see cref="Application.Settings.AppSettings.AutoRescheduleOverdueTasks"/>.</summary>
    [ObservableProperty]
    public partial bool AutoRescheduleOverdueTasks { get; set; }

    /// <summary>See <see cref="Application.Settings.AppSettings.IsMiniWidgetMode"/>. <c>WidgetWindow</c> reacts to changes on this to shrink/restore its own bounds — a ViewModel shouldn't touch Window.Height directly.</summary>
    [ObservableProperty]
    public partial bool IsMiniWidgetMode { get; set; }

    [RelayCommand]
    private async Task ToggleMiniWidgetModeAsync(CancellationToken cancellationToken = default)
    {
        IsMiniWidgetMode = !IsMiniWidgetMode;

        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            settings.IsMiniWidgetMode = IsMiniWidgetMode;
            await _settingsService.SaveAsync(settings, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist mini widget mode");
        }
    }

    /// <summary>Raised when the header's gear icon is clicked. Mirrors <see cref="TaskEditRequested"/> — a ViewModel shouldn't construct Views, so this just bubbles the request up to <c>WidgetWindow</c>.</summary>
    public event EventHandler? SettingsRequested;

    [RelayCommand]
    private void OpenSettings() => SettingsRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised when the header's grid icon is clicked (Phase 20's Excel-style all-tasks view). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? GridViewRequested;

    [RelayCommand]
    private void OpenGridView() => GridViewRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised when the header's calendar icon is clicked (Phase 21's month-grid calendar). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? CalendarViewRequested;

    [RelayCommand]
    private void OpenCalendarView() => CalendarViewRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised when the header's planner icon is clicked (Phase 21's Week/Year/Agenda/Timeline/Kanban/Matrix views). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? PlannerViewRequested;

    [RelayCommand]
    private void OpenPlannerView() => PlannerViewRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised when the header's timer icon is clicked (Phase 23's Focus Timer). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? FocusTimerRequested;

    [RelayCommand]
    private void OpenFocusTimer() => FocusTimerRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised when the header's chart icon is clicked (Phase 24's Analytics Dashboard). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? AnalyticsRequested;

    [RelayCommand]
    private void OpenAnalytics() => AnalyticsRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised by Cmd/Ctrl+K (Phase 28's Command Palette). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/> — <c>WidgetWindow</c> builds the palette's entry list from this instance's own commands.</summary>
    public event EventHandler? CommandPaletteRequested;

    [RelayCommand]
    private void OpenCommandPalette() => CommandPaletteRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised from the tray menu's "Clipboard History…" item and the Command Palette (Phase 28). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? ClipboardHistoryRequested;

    [RelayCommand]
    private void OpenClipboardHistory() => ClipboardHistoryRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised by the quick-add row's "Groups" button and the Command Palette. Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? TaskGroupsRequested;

    [RelayCommand]
    private void OpenTaskGroups() => TaskGroupsRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised from the tray menu's "Trash…" item and the Command Palette (Roadmap-39-100.md's Feature 46). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? TrashRequested;

    [RelayCommand]
    private void OpenTrash() => TrashRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised from the tray menu's "Backups…" item and the Command Palette (Feature 67, Roadmap-39-100.md). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? BackupRequested;

    [RelayCommand]
    private void OpenBackups() => BackupRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised from the tray menu's "Data Integrity Check…" item and the Command Palette (Feature 70, Roadmap-39-100.md). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? IntegrityCheckRequested;

    [RelayCommand]
    private void OpenIntegrityCheck() => IntegrityCheckRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Feature 43's Undo/Redo Engine — undoes the most recent complete/reopen, rename,
    /// pin/unpin, or delete/restore action recorded by a <see cref="TaskItemViewModel"/> row.
    /// Reloads today's list afterward since the undone action (e.g. a delete) may change which
    /// rows belong on the currently-viewed day.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private async Task UndoAsync()
    {
        if (await _undoRedoService.UndoAsync())
        {
            await LoadTasksAsync();
        }
    }

    private bool CanUndo() => _undoRedoService.CanUndo;

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private async Task RedoAsync()
    {
        if (await _undoRedoService.RedoAsync())
        {
            await LoadTasksAsync();
        }
    }

    private bool CanRedo() => _undoRedoService.CanRedo;

    /// <summary>Raised from the tray menu's "Inbox…" item and the Command Palette (Feature 39, Roadmap-39-100.md). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? InboxRequested;

    [RelayCommand]
    private void OpenInbox() => InboxRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised from the tray menu's "Archive Vault…" item and the Command Palette (Feature 45, Roadmap-39-100.md). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? ArchiveVaultRequested;

    [RelayCommand]
    private void OpenArchiveVault() => ArchiveVaultRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised from the Command Palette (Feature 61, Roadmap-39-100.md). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? ActivityTimelineRequested;

    [RelayCommand]
    private void OpenActivityTimeline() => ActivityTimelineRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised from the tray menu's "Database Maintenance…" item and the Command Palette (Feature 69, Roadmap-39-100.md). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? DatabaseMaintenanceRequested;

    [RelayCommand]
    private void OpenDatabaseMaintenance() => DatabaseMaintenanceRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised from the Command Palette (Feature 65, Roadmap-39-100.md). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? WorkSessionHistoryRequested;

    [RelayCommand]
    private void OpenWorkSessionHistory() => WorkSessionHistoryRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised from the Command Palette (Features 51/52/53/55/56, Roadmap-39-100.md). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? PlanningInsightsRequested;

    [RelayCommand]
    private void OpenPlanningInsights() => PlanningInsightsRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised from the Command Palette (Feature 57, Roadmap-39-100.md). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? DecisionLogRequested;

    [RelayCommand]
    private void OpenDecisionLog() => DecisionLogRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised from the Command Palette (Feature 60, Roadmap-39-100.md). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? JournalRequested;

    [RelayCommand]
    private void OpenJournal() => JournalRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised from the Command Palette (Feature 62, Roadmap-39-100.md). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? AchievementsRequested;

    [RelayCommand]
    private void OpenAchievements() => AchievementsRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised from the Command Palette (Feature 64, Roadmap-39-100.md). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? DistractionLogRequested;

    [RelayCommand]
    private void OpenDistractionLog() => DistractionLogRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised from the Command Palette (Feature 63, Roadmap-39-100.md). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? ContextsRequested;

    [RelayCommand]
    private void OpenContexts() => ContextsRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised from the Command Palette (Feature 77, Roadmap-39-100.md). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? KeyboardShortcutsRequested;

    [RelayCommand]
    private void OpenKeyboardShortcuts() => KeyboardShortcutsRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Feature 83's Saved Views, generalized from the grid (<c>GridViewModel.GetSavedViewsAsync</c>) to this widget's own search/filter/sort bar.</summary>
    public async Task<IReadOnlyList<WidgetSavedView>> GetSavedViewsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken);
        return settings.WidgetSavedViews;
    }

    /// <summary>Saves the widget's current search/filter/sort state as one named preset, overwriting any existing preset with the same name (case-insensitive) — same semantics as <c>GridViewModel.SaveCurrentViewAsync</c>.</summary>
    public async Task SaveCurrentViewAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim();
        if (string.IsNullOrEmpty(trimmedName))
        {
            return;
        }

        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            settings.WidgetSavedViews.RemoveAll(v => string.Equals(v.Name, trimmedName, StringComparison.OrdinalIgnoreCase));
            settings.WidgetSavedViews.Add(new WidgetSavedView
            {
                Name = trimmedName,
                SearchText = string.IsNullOrEmpty(SearchText) ? null : SearchText,
                CategoryId = SelectedCategoryFilter.Id,
                TagId = SelectedTagFilter.Id,
                ProjectId = SelectedProjectFilter.Id,
                StatusFilter = SelectedStatusFilter.ToString(),
                SortOption = SelectedSortOption.ToString(),
            });
            await _settingsService.SaveAsync(settings, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save widget view '{Name}'", trimmedName);
        }
    }

    public async Task DeleteSavedViewAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            settings.WidgetSavedViews.RemoveAll(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));
            await _settingsService.SaveAsync(settings, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete widget view '{Name}'", name);
        }
    }

    /// <summary>Applies a saved view's search/filter/sort state — plain data-bound ViewModel state, so setting it here is immediately reflected in the UI, same as <c>GridViewModel.ApplyViewAsync</c>'s filter-bar half.</summary>
    public async Task ApplyViewAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            var view = settings.WidgetSavedViews.FirstOrDefault(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));
            if (view is null)
            {
                return;
            }

            SearchText = view.SearchText ?? string.Empty;
            SelectedCategoryFilter = Categories.FirstOrDefault(c => c.Id == view.CategoryId) ?? CategoryFilterOption.All;
            SelectedTagFilter = Tags.FirstOrDefault(t => t.Id == view.TagId) ?? TagFilterOption.All;
            SelectedProjectFilter = Projects.FirstOrDefault(p => p.Id == view.ProjectId) ?? ProjectFilterOption.All;
            SelectedStatusFilter = Enum.TryParse<TaskStatusFilter>(view.StatusFilter, out var statusFilter) ? statusFilter : TaskStatusFilter.All;
            SelectedSortOption = Enum.TryParse<TaskSortOption>(view.SortOption, out var sortOption) ? sortOption : TaskSortOption.Manual;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply widget view '{Name}'", name);
        }
    }

    /// <summary>Raised from the tray menu and Command Palette (Feature 58, Roadmap-39-100.md). Same "ViewModel shouldn't construct Views" reasoning as <see cref="SettingsRequested"/>.</summary>
    public event EventHandler? MeetingModeRequested;

    [RelayCommand]
    private void OpenMeetingMode() => MeetingModeRequested?.Invoke(this, EventArgs.Empty);

    public async Task LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            AccentColorHex = settings.AccentColorHex;
            Theme = settings.Theme;
            WidgetOpacity = settings.WidgetOpacity;
            WindowLeft = settings.WindowLeft;
            WindowTop = settings.WindowTop;
            WindowWidth = settings.WindowWidth;
            WindowHeight = settings.WindowHeight;
            PreferredMonitorId = settings.PreferredMonitorId;
            NotificationsEnabled = settings.NotificationsEnabled;
            ShowInTaskbar = settings.ShowInTaskbar;
            AutoRescheduleOverdueTasks = settings.AutoRescheduleOverdueTasks;
            IsMiniWidgetMode = settings.IsMiniWidgetMode;
            _breakReminderEnabled = settings.BreakReminderEnabled;
            _breakReminderIntervalMinutes = settings.BreakReminderIntervalMinutes;
            _waterReminderEnabled = settings.WaterReminderEnabled;
            _waterReminderIntervalMinutes = settings.WaterReminderIntervalMinutes;
            _stretchReminderEnabled = settings.StretchReminderEnabled;
            _stretchReminderIntervalMinutes = settings.StretchReminderIntervalMinutes;
            _notificationSoundEnabled = settings.NotificationSoundEnabled;

            // Baselines the "when did this last fire" clock to now, not left null — null
            // would mean the very first 30-second poll after enabling a reminder fires it
            // immediately instead of waiting a full interval. ??= (not =) since
            // LoadSettingsAsync re-runs after the Settings window closes, and that shouldn't
            // push a real, already-ticking reminder further out each time.
            var now = _timeProvider.GetLocalNow().DateTime;
            _lastBreakReminderAt ??= now;
            _lastWaterReminderAt ??= now;
            _lastStretchReminderAt ??= now;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
        }
    }

    /// <summary>
    /// Persists the widget window's current bounds so it reopens where the
    /// user left it — called from <c>WidgetWindow.OnClosing</c>. Reloads
    /// settings first rather than reconstructing an <see cref="AppSettings"/>
    /// from this ViewModel's own fields, so a concurrent edit made in the
    /// Settings window (accent color, opacity) during this session isn't
    /// clobbered by a stale copy.
    /// </summary>
    public async Task SaveWindowBoundsAsync(double left, double top, double width, double height, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            settings.WindowLeft = left;
            settings.WindowTop = top;
            settings.WindowWidth = width;
            settings.WindowHeight = height;
            await _settingsService.SaveAsync(settings, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save window bounds");
        }
    }

    /// <summary>
    /// Moves <paramref name="draggedTaskId"/> to sit at <paramref name="targetTaskId"/>'s
    /// current position and persists the resulting order. Called from the view's
    /// drag-drop handlers (see <c>WidgetWindow.axaml.cs</c>).
    /// </summary>
    public async Task ReorderAsync(Guid draggedTaskId, Guid targetTaskId)
    {
        if (draggedTaskId == targetTaskId)
        {
            return;
        }

        var orderedIds = Tasks.Select(t => t.Id).ToList();
        if (!orderedIds.Contains(draggedTaskId) || !orderedIds.Contains(targetTaskId))
        {
            return;
        }

        // Re-find targetTaskId's index after removal (it shifts down by one if it
        // originally came after draggedTaskId) rather than reusing the pre-removal index.
        orderedIds.Remove(draggedTaskId);
        orderedIds.Insert(orderedIds.IndexOf(targetTaskId), draggedTaskId);

        try
        {
            await _taskService.ReorderTasksAsync(PlanDate, orderedIds);
            await LoadTasksAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder tasks for {PlanDate}", PlanDate);
        }
    }

    /// <summary>
    /// "Automatically switch to the next day at midnight" only applies while the widget
    /// is actually following today — if the user has navigated to a past or future day to
    /// plan ahead or review history, midnight passing in the real world shouldn't yank
    /// them back to today out from under them.
    /// </summary>
    internal void OnDayRolloverTick(object? sender, EventArgs e)
    {
        var today = Today();
        if (today == _lastKnownToday)
        {
            return;
        }

        var wasFollowingToday = PlanDate == _lastKnownToday;
        _lastKnownToday = today;

        if (!wasFollowingToday)
        {
            OnPropertyChanged(nameof(IsToday));
            OnPropertyChanged(nameof(EmptyStateText));
            return;
        }

        _logger.LogInformation("Day rolled over from {PreviousDate} to {NewDate}; reloading widget", PlanDate, today);
        _ = NavigateToAsync(today);
    }

    private void OnNotificationCheckTick(object? sender, EventArgs e) => _ = CheckForOverdueTaskNotificationsAsync();

    private void OnWellnessReminderCheckTick(object? sender, EventArgs e) => _ = CheckWellnessRemindersAsync();

    /// <summary>
    /// Phase 23's Break/Water/Stretch Reminders — each fires independently once its own
    /// interval has elapsed since it last fired (or since app start, for the first one).
    /// Deliberately not gated on <see cref="NotificationsEnabled"/> the way overdue-task
    /// alerts and the daily summary are — a user who's turned off task-due notifications
    /// hasn't necessarily also opted out of wellness nudges, since these are enabled/disabled
    /// independently in Settings and default to off regardless. Internal for the same
    /// direct-test-without-a-real-timer reason as <see cref="CheckForOverdueTaskNotificationsAsync"/>.
    /// </summary>
    internal async Task CheckWellnessRemindersAsync()
    {
        var now = _timeProvider.GetLocalNow().DateTime;

        if (ShouldRemind(_breakReminderEnabled, _breakReminderIntervalMinutes, _lastBreakReminderAt, now))
        {
            _lastBreakReminderAt = now;
            await _notificationService.NotifyAsync("Break Reminder", "Time for a short break.", _notificationSoundEnabled);
        }

        if (ShouldRemind(_waterReminderEnabled, _waterReminderIntervalMinutes, _lastWaterReminderAt, now))
        {
            _lastWaterReminderAt = now;
            await _notificationService.NotifyAsync("Water Reminder", "Remember to drink some water.", _notificationSoundEnabled);
        }

        if (ShouldRemind(_stretchReminderEnabled, _stretchReminderIntervalMinutes, _lastStretchReminderAt, now))
        {
            _lastStretchReminderAt = now;
            await _notificationService.NotifyAsync("Stretch Reminder", "Take a moment to stretch.", _notificationSoundEnabled);
        }
    }

    private static bool ShouldRemind(bool enabled, int intervalMinutes, DateTime? lastReminderAt, DateTime now) =>
        enabled && (lastReminderAt is null || (now - lastReminderAt.Value).TotalMinutes >= intervalMinutes);

    /// <summary>
    /// Fires once per task the first time its due time passes while still incomplete —
    /// <see cref="_notifiedOverdueTaskIds"/> is what makes it "once" rather than every
    /// 30-second poll. Runs against the already-loaded <see cref="Tasks"/> in memory, not a
    /// fresh query, so it only ever catches tasks for whatever day is currently being
    /// viewed — acceptable, since the widget only shows one day's tasks at a time anyway.
    /// Internal (not private) so tests can await it directly instead of going through the
    /// fire-and-forget timer tick — see <see cref="OnDayRolloverTick"/>'s equivalent remark.
    /// </summary>
    internal async Task CheckForOverdueTaskNotificationsAsync()
    {
        if (!NotificationsEnabled)
        {
            return;
        }

        var now = _timeProvider.GetLocalNow().DateTime;
        foreach (var task in Tasks)
        {
            if (task.IsCompleted || task.DueDate is not { } due || due >= now)
            {
                continue;
            }

            if (task.SnoozedUntil is { } snoozedUntil)
            {
                if (snoozedUntil > now)
                {
                    continue;
                }

                // The snooze window has passed — allow one more notification even if this
                // task was already notified once before being snoozed.
                _notifiedOverdueTaskIds.Remove(task.Id);
            }

            if (!_notifiedOverdueTaskIds.Add(task.Id))
            {
                continue;
            }

            await _notificationService.NotifyAsync("Task overdue", $"\"{task.Title}\" was due.", _notificationSoundEnabled);
        }
    }

    /// <summary>
    /// A once-per-calendar-day "you have N tasks today" notification — only while actually
    /// viewing today (browsing a past/future day to plan ahead shouldn't trigger it) and
    /// only if there's anything left to do. Checked from <see cref="LoadTasksAsync"/> rather
    /// than only on day-rollover, so it also fires on a normal morning app-open, not just
    /// when the app happens to already be running at midnight.
    /// </summary>
    private async Task MaybeSendDailySummaryAsync(CancellationToken cancellationToken)
    {
        if (!NotificationsEnabled || PlanDate != Today() || _lastDailySummaryDate == PlanDate)
        {
            return;
        }

        _lastDailySummaryDate = PlanDate;

        var incompleteCount = Tasks.Count(t => !t.IsCompleted);
        if (incompleteCount == 0)
        {
            return;
        }

        var message = incompleteCount == 1 ? "You have 1 task today." : $"You have {incompleteCount} tasks today.";
        await _notificationService.NotifyAsync("Today's tasks", message, _notificationSoundEnabled, cancellationToken);
    }

    public void Dispose()
    {
        _dayRolloverTimer.Stop();
        _dayRolloverTimer.Tick -= OnDayRolloverTick;
        _dayRolloverTimer.Tick -= OnNotificationCheckTick;

        foreach (var task in Tasks)
        {
            task.PropertyChanged -= OnTaskItemPropertyChanged;
        }
    }
}
