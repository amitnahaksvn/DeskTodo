using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Application.Settings;
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
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WidgetViewModel> _logger;
    private readonly ILogger<TaskItemViewModel> _taskItemLogger;
    private readonly DispatcherTimer _dayRolloverTimer;

    // Tracks what "today" was as of the last check, separately from PlanDate (the day
    // currently being viewed) — see OnDayRolloverTick for why the two need to be distinct.
    private DateOnly _lastKnownToday;

    // Session-only (not persisted): which overdue tasks have already fired a notification,
    // and which calendar day the "N tasks today" summary was last sent for. Both reset on
    // app restart — acceptable, since re-notifying once after a restart isn't harmful, and
    // persisting them would need its own storage for little benefit.
    private readonly HashSet<Guid> _notifiedOverdueTaskIds = [];
    private DateOnly? _lastDailySummaryDate;

    public WidgetViewModel(ITaskService taskService, ICategoryRepository categoryRepository, ISettingsService settingsService, INotificationService notificationService, TimeProvider timeProvider, ILogger<WidgetViewModel> logger, ILogger<TaskItemViewModel> taskItemLogger)
    {
        _taskService = taskService;
        _categoryRepository = categoryRepository;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
        _logger = logger;
        _taskItemLogger = taskItemLogger;

        _lastKnownToday = Today();
        PlanDate = _lastKnownToday;

        // Polls rather than scheduling a single timer for exactly midnight so a
        // sleeping/suspended machine still catches the rollover soon after waking. The same
        // timer also drives the overdue-task notification check (OnNotificationCheckTick) —
        // one 30-second poll, two independent Tick subscribers, rather than a second timer
        // for what's the same "check periodically" need.
        _dayRolloverTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _dayRolloverTimer.Tick += OnDayRolloverTick;
        _dayRolloverTimer.Tick += OnNotificationCheckTick;
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

    // These On<Property>Changed hooks are safe from the constructor-persistence footgun
    // TaskItemViewModel's doc comments warn about: property *initializers* (the "= ..."
    // above) set the backing field directly and don't invoke the setter, so they never
    // fire from construction — only genuine later changes (typing in the search box,
    // picking a dropdown value) do.
    partial void OnSearchTextChanged(string value) => RefreshVisibleTasks();

    partial void OnSelectedStatusFilterChanged(TaskStatusFilter value) => RefreshVisibleTasks();

    partial void OnSelectedSortOptionChanged(TaskSortOption value) => RefreshVisibleTasks();

    partial void OnSelectedCategoryFilterChanged(CategoryFilterOption value) => RefreshVisibleTasks();

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

    [ObservableProperty]
    public partial string NewTaskTitle { get; set; } = string.Empty;

    /// <summary>Bound to the "add task" row's Enter key. A blank title is a no-op rather than an error.</summary>
    [RelayCommand]
    private async Task AddTaskAsync()
    {
        var title = NewTaskTitle.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        try
        {
            await _taskService.CreateTaskAsync(PlanDate, title);
            NewTaskTitle = string.Empty;
            await LoadTasksAsync();
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

            var tasks = await _taskService.GetTasksForDateAsync(PlanDate, cancellationToken);

            foreach (var existing in Tasks)
            {
                existing.PropertyChanged -= OnTaskItemPropertyChanged;
            }

            Tasks.Clear();
            foreach (var task in tasks)
            {
                var itemViewModel = new TaskItemViewModel(task, _taskService, _taskItemLogger, () => _ = LoadTasksAsync(), id => TaskEditRequested?.Invoke(this, id))
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

        query = SelectedSortOption switch
        {
            TaskSortOption.Priority => query.OrderByDescending(t => t.Priority),
            TaskSortOption.DueDate => query.OrderBy(t => t.DueDate ?? DateTime.MaxValue),
            TaskSortOption.Title => query.OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase),
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WidgetBackgroundHex))]
    public partial double WidgetOpacity { get; set; } = 0.95;

    /// <summary>"#AARRGGBB" — <see cref="WidgetOpacity"/> blended into an otherwise-white background, bound to the widget's outer <c>Border.Background</c>. Text stays fully opaque; only the card behind it fades.</summary>
    public string WidgetBackgroundHex => $"#{(byte)Math.Round(WidgetOpacity * 255):X2}FFFFFF";

    /// <summary>Last-known window bounds from settings — null (all four) means "use the built-in default." Read once by <c>WidgetWindow</c> after <see cref="LoadSettingsAsync"/> to restore position/size; the accent color is applied separately since that's an <c>Application.Resources</c> side effect, not a per-window one.</summary>
    public double? WindowLeft { get; private set; }

    public double? WindowTop { get; private set; }

    public double? WindowWidth { get; private set; }

    public double? WindowHeight { get; private set; }

    [ObservableProperty]
    public partial bool NotificationsEnabled { get; set; } = true;

    /// <summary>Bound to <c>WidgetWindow</c>'s own <c>ShowInTaskbar</c> — see <see cref="Application.Settings.AppSettings.ShowInTaskbar"/> for why this defaults to <c>true</c>.</summary>
    [ObservableProperty]
    public partial bool ShowInTaskbar { get; set; } = true;

    /// <summary>Raised when the header's gear icon is clicked. Mirrors <see cref="TaskEditRequested"/> — a ViewModel shouldn't construct Views, so this just bubbles the request up to <c>WidgetWindow</c>.</summary>
    public event EventHandler? SettingsRequested;

    [RelayCommand]
    private void OpenSettings() => SettingsRequested?.Invoke(this, EventArgs.Empty);

    public async Task LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            AccentColorHex = settings.AccentColorHex;
            WidgetOpacity = settings.WidgetOpacity;
            WindowLeft = settings.WindowLeft;
            WindowTop = settings.WindowTop;
            WindowWidth = settings.WindowWidth;
            WindowHeight = settings.WindowHeight;
            NotificationsEnabled = settings.NotificationsEnabled;
            ShowInTaskbar = settings.ShowInTaskbar;
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

            if (!_notifiedOverdueTaskIds.Add(task.Id))
            {
                continue;
            }

            await _notificationService.NotifyAsync("Task overdue", $"\"{task.Title}\" was due.");
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
        await _notificationService.NotifyAsync("Today's tasks", message, cancellationToken);
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
