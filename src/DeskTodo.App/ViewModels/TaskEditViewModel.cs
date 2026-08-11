using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the full-field task editor (description, priority, category, due
/// date, estimated time, notes, checklist, tags, color, recurrence, subtasks,
/// dependencies, attachments — everything <see cref="TaskItemViewModel"/>'s
/// inline title edit doesn't cover). Depends on <see cref="ICategoryRepository"/>
/// directly (rather than only <see cref="ITaskService"/>) purely to populate
/// the category dropdown — a plain read, not a use case, so routing it
/// through the service layer would just be ceremony.
/// </summary>
/// <remarks>
/// Checklist items, tags, subtasks, dependencies, and attachments are all
/// persisted immediately as the user adds/toggles/removes them (each is its
/// own small unit of work against the relevant service), the same
/// "immediate, not staged" convention every other row-level mutation in this
/// app follows (Pin, Complete, Delete, ...). Everything else on this screen
/// (title, description, priority, category, due date, notes, color,
/// recurrence, parent task, favorite) stays staged in the ViewModel's own
/// properties until <see cref="SaveAsync"/>, matching this editor's
/// pre-existing batched-save behavior for those fields.
/// </remarks>
public sealed partial class TaskEditViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IChecklistService _checklistService;
    private readonly ITagService _tagService;
    private readonly ITaskTemplateService _templateService;
    private readonly ITaskDependencyService _taskDependencyService;
    private readonly IAttachmentService _attachmentService;
    private readonly IMilestoneService _milestoneService;
    private readonly IProjectService _projectService;
    private readonly ILogger<TaskEditViewModel> _logger;
    private readonly ILogger<ChecklistItemRowViewModel> _checklistItemLogger;
    private Guid _taskId;

    public TaskEditViewModel(
        ITaskService taskService,
        ICategoryRepository categoryRepository,
        IChecklistService checklistService,
        ITagService tagService,
        ITaskTemplateService templateService,
        ITaskDependencyService taskDependencyService,
        IAttachmentService attachmentService,
        IMilestoneService milestoneService,
        IProjectService projectService,
        ILogger<TaskEditViewModel> logger,
        ILogger<ChecklistItemRowViewModel> checklistItemLogger)
    {
        _taskService = taskService;
        _categoryRepository = categoryRepository;
        _checklistService = checklistService;
        _tagService = tagService;
        _templateService = templateService;
        _taskDependencyService = taskDependencyService;
        _attachmentService = attachmentService;
        _milestoneService = milestoneService;
        _projectService = projectService;
        _logger = logger;
        _checklistItemLogger = checklistItemLogger;
    }

    public ObservableCollection<CategoryOption> Categories { get; } = [CategoryOption.None];

    public IReadOnlyList<TaskPriority> Priorities { get; } = Enum.GetValues<TaskPriority>();

    public IReadOnlyList<TaskType> TaskTypes { get; } = Enum.GetValues<TaskType>();

    public IReadOnlyList<RecurrenceFrequency> RecurrenceFrequencies { get; } = Enum.GetValues<RecurrenceFrequency>();

    /// <summary>A small fixed palette rather than a full color picker — consistent with the built-in category colors, and enough choice for visually grouping tasks at a glance.</summary>
    public IReadOnlyList<string> ColorPresets { get; } = ["#EF4444", "#F97316", "#F59E0B", "#22C55E", "#3B82F6", "#8B5CF6", "#EC4899", "#64748B"];

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }

    /// <summary>Read by <c>TaskEditWindow</c> on <see cref="StartTimerRequested"/> to preselect this task in the Focus Timer — see <c>FocusTimerViewModel.PreselectTask</c>.</summary>
    public Guid TaskId => _taskId;

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    /// <summary>Toggles the Notes field between its plain <c>TextBox</c> and a hand-rolled Markdown-rendered preview (bold/italic/bullet lines) — see <c>NotesPreviewConverter</c>.</summary>
    [ObservableProperty]
    public partial bool IsNotesPreview { get; set; }

    [ObservableProperty]
    public partial TaskPriority Priority { get; set; } = TaskPriority.Medium;

    [ObservableProperty]
    public partial TaskType Type { get; set; } = TaskType.Task;

    [ObservableProperty]
    public partial CategoryOption SelectedCategory { get; set; } = CategoryOption.None;

    [ObservableProperty]
    public partial DateTimeOffset? DueDate { get; set; }

    [ObservableProperty]
    public partial decimal? EstimatedMinutes { get; set; }

    /// <summary>
    /// Phase 23's Time Tracking — read-only (unlike <see cref="EstimatedMinutes"/>): it
    /// accumulates from completed <c>FocusSession</c>s linked to this task
    /// (<c>IFocusSessionService.CompleteSessionAsync</c>), not something typed in here
    /// directly, so it isn't part of this editor's staged title/notes/etc. save flow.
    /// </summary>
    [ObservableProperty]
    public partial int? ActualMinutes { get; set; }

    /// <summary>Null means "use the category's color" — see <see cref="Domain.Entities.TaskItem.ColorHex"/>.</summary>
    [ObservableProperty]
    public partial string? SelectedColorHex { get; set; }

    [ObservableProperty]
    public partial RecurrenceFrequency SelectedRecurrenceFrequency { get; set; } = RecurrenceFrequency.None;

    /// <summary>"Every N" — meaningless (and disabled in the view) while <see cref="SelectedRecurrenceFrequency"/> is None.</summary>
    [ObservableProperty]
    public partial decimal RecurrenceInterval { get; set; } = 1;

    [ObservableProperty]
    public partial DateTimeOffset? RecurrenceEndDate { get; set; }

    /// <summary>The task's checklist — each row persists its own check/remove immediately; see the class remarks.</summary>
    public ObservableCollection<ChecklistItemRowViewModel> ChecklistItems { get; } = [];

    [ObservableProperty]
    public partial string NewChecklistItemText { get; set; } = string.Empty;

    /// <summary>The task's assigned tags — added/removed immediately; see the class remarks.</summary>
    public ObservableCollection<TagChip> Tags { get; } = [];

    [ObservableProperty]
    public partial string NewTagText { get; set; } = string.Empty;

    /// <summary>"Save as Template" is enabled once this has text — reusing the current task's shape under a new template name.</summary>
    [ObservableProperty]
    public partial string TemplateNameToSave { get; set; } = string.Empty;

    /// <summary>Other tasks on the same day — feeds both <see cref="SelectedParentTask"/> and the "add blocker" picker, one fetch shared by both.</summary>
    public ObservableCollection<TaskOption> SameDayTasks { get; } = [TaskOption.None];

    [ObservableProperty]
    public partial TaskOption SelectedParentTask { get; set; } = TaskOption.None;

    /// <summary>Every milestone, for the "Milestone" picker — see <see cref="Domain.Entities.Milestone"/>.</summary>
    public ObservableCollection<MilestoneOption> MilestoneOptions { get; } = [MilestoneOption.None];

    [ObservableProperty]
    public partial MilestoneOption SelectedMilestone { get; set; } = MilestoneOption.None;

    /// <summary>Every non-archived project, for the "Project" picker — see <see cref="Domain.Entities.Project"/>.</summary>
    public ObservableCollection<ProjectOption> ProjectOptions { get; } = [ProjectOption.None];

    [ObservableProperty]
    public partial ProjectOption SelectedProject { get; set; } = ProjectOption.None;

    /// <summary>Child tasks — added/removed immediately; see the class remarks.</summary>
    public ObservableCollection<SubtaskRowViewModel> Subtasks { get; } = [];

    [ObservableProperty]
    public partial string NewSubtaskTitle { get; set; } = string.Empty;

    /// <summary>Tasks that must be completed before this one can be — added/removed immediately; see the class remarks.</summary>
    public ObservableCollection<BlockerChip> Blockers { get; } = [];

    [ObservableProperty]
    public partial TaskOption? SelectedBlockerToAdd { get; set; }

    /// <summary>True while any <see cref="Blockers"/> entry is still incomplete — mirrors <see cref="Domain.Entities.TaskItem.IsBlocked"/>, recomputed on every Blockers change since ObservableCollection doesn't raise this on its own.</summary>
    public bool IsBlocked => Blockers.Any(b => !b.IsComplete);

    /// <summary>Attached files — added/removed immediately; see the class remarks.</summary>
    public ObservableCollection<AttachmentRowViewModel> Attachments { get; } = [];

    /// <summary>Raised when a row's "Open" is clicked — opening a file with the OS default handler needs a live <c>TopLevel</c>, which <c>TaskEditWindow</c> (not this ViewModel) owns.</summary>
    public event EventHandler<string>? AttachmentOpenRequested;

    /// <summary>Raised after a successful save; the view closes itself in response.</summary>
    public event EventHandler? Saved;

    public event EventHandler? CancelRequested;

    /// <summary>Raised when "Start Timer" is clicked (Phase 23) — opening/focusing the Focus Timer window needs the DI-singleton <c>FocusTimerViewModel</c>, which this ViewModel has no reason to depend on directly; <c>TaskEditWindow</c> handles it.</summary>
    public event EventHandler? StartTimerRequested;

    [RelayCommand]
    private void StartTimer() => StartTimerRequested?.Invoke(this, EventArgs.Empty);

    public async Task LoadAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        _taskId = taskId;

        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        Categories.Clear();
        Categories.Add(CategoryOption.None);
        foreach (var category in categories)
        {
            Categories.Add(new CategoryOption(category.Id, category.Name, category.ColorHex));
        }

        var task = await _taskService.GetTaskAsync(taskId, cancellationToken);
        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} could not be loaded for editing (it may have just been deleted)", taskId);
            CancelRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        Title = task.Title;
        Description = task.Description ?? string.Empty;
        Notes = task.Notes ?? string.Empty;
        IsNotesPreview = false;
        Priority = task.Priority;
        Type = task.Type;
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == task.CategoryId) ?? CategoryOption.None;
        DueDate = task.DueDate is { } due ? new DateTimeOffset(due) : null;
        EstimatedMinutes = task.EstimatedMinutes;
        ActualMinutes = task.ActualMinutes;
        SelectedColorHex = task.ColorHex;
        SelectedRecurrenceFrequency = task.RecurrenceFrequency;
        RecurrenceInterval = task.RecurrenceInterval;
        RecurrenceEndDate = task.RecurrenceEndDate is { } end ? new DateTimeOffset(end.ToDateTime(TimeOnly.MinValue)) : null;
        TemplateNameToSave = string.Empty;

        // task.ChecklistItems/Tags/Subtasks/BlockedByDependencies come pre-loaded from
        // ITaskService.GetTaskAsync — no extra round trip needed just to display them.
        ChecklistItems.Clear();
        foreach (var item in task.ChecklistItems.OrderBy(c => c.Order))
        {
            ChecklistItems.Add(new ChecklistItemRowViewModel(item, _checklistService, _checklistItemLogger, RemoveChecklistRow));
        }

        Tags.Clear();
        foreach (var tag in task.Tags.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
        {
            Tags.Add(new TagChip(tag.Id, tag.Name, _taskId, _tagService, _logger, RemoveTagRow));
        }

        var sameDayTasks = await _taskService.GetTasksForDateAsync(task.PlanDate, cancellationToken);
        SameDayTasks.Clear();
        SameDayTasks.Add(TaskOption.None);
        // A task can't become its own parent, and can't be parented by one of its own
        // subtasks (that would form an immediate cycle in this one-level relationship).
        foreach (var candidate in sameDayTasks.Where(t => t.Id != taskId && t.ParentTaskId != taskId))
        {
            SameDayTasks.Add(new TaskOption(candidate.Id, candidate.Title));
        }

        SelectedParentTask = SameDayTasks.FirstOrDefault(o => o.Id == task.ParentTaskId) ?? TaskOption.None;

        var milestones = await _milestoneService.GetMilestonesAsync(cancellationToken);
        MilestoneOptions.Clear();
        MilestoneOptions.Add(MilestoneOption.None);
        foreach (var milestone in milestones.OrderBy(m => m.TargetDate ?? DateOnly.MaxValue))
        {
            MilestoneOptions.Add(new MilestoneOption(milestone.Id, milestone.Title));
        }

        SelectedMilestone = MilestoneOptions.FirstOrDefault(o => o.Id == task.MilestoneId) ?? MilestoneOption.None;

        var projects = await _projectService.GetProjectsAsync(cancellationToken);
        ProjectOptions.Clear();
        ProjectOptions.Add(ProjectOption.None);
        // Archived projects are hidden from the picker (they're "put away," not deleted) —
        // except the one already assigned to this task, so an existing assignment stays
        // visible/selected rather than silently reading as "None" the moment its project
        // gets archived.
        foreach (var project in projects.Where(p => !p.IsArchived || p.Id == task.ProjectId).OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
        {
            ProjectOptions.Add(new ProjectOption(project.Id, project.Name));
        }

        SelectedProject = ProjectOptions.FirstOrDefault(o => o.Id == task.ProjectId) ?? ProjectOption.None;

        Subtasks.Clear();
        foreach (var subtask in task.Subtasks.OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase))
        {
            Subtasks.Add(new SubtaskRowViewModel(subtask, _taskService, _logger, RemoveSubtaskRow));
        }

        Blockers.Clear();
        foreach (var dependency in task.BlockedByDependencies)
        {
            if (dependency.BlockingTask is not { } blockingTask)
            {
                continue;
            }

            Blockers.Add(new BlockerChip(dependency.Id, blockingTask.Id, blockingTask.Title, blockingTask.IsCompleted, _taskDependencyService, _logger, RemoveBlockerRow));
        }

        OnPropertyChanged(nameof(IsBlocked));

        var attachments = await _attachmentService.GetAttachmentsAsync(taskId, cancellationToken);
        Attachments.Clear();
        foreach (var attachment in attachments)
        {
            Attachments.Add(new AttachmentRowViewModel(attachment, _attachmentService.GetAbsolutePath(attachment), _attachmentService, _logger, RemoveAttachmentRow, OpenAttachmentRow));
        }

        IsLoaded = true;
    }

    private void RemoveChecklistRow(ChecklistItemRowViewModel row) => ChecklistItems.Remove(row);

    private void RemoveTagRow(TagChip chip) => Tags.Remove(chip);

    private void RemoveSubtaskRow(SubtaskRowViewModel row) => Subtasks.Remove(row);

    private void RemoveBlockerRow(BlockerChip chip)
    {
        Blockers.Remove(chip);
        OnPropertyChanged(nameof(IsBlocked));
    }

    private void RemoveAttachmentRow(AttachmentRowViewModel row) => Attachments.Remove(row);

    private void OpenAttachmentRow(AttachmentRowViewModel row) => AttachmentOpenRequested?.Invoke(this, row.AbsolutePath);

    [RelayCommand]
    private void SelectColor(string? hex) => SelectedColorHex = hex;

    [RelayCommand]
    private void ToggleNotesPreview() => IsNotesPreview = !IsNotesPreview;

    [RelayCommand]
    private async Task AddChecklistItemAsync()
    {
        var text = NewChecklistItemText.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        try
        {
            var item = await _checklistService.AddItemAsync(_taskId, text);
            if (item is not null)
            {
                ChecklistItems.Add(new ChecklistItemRowViewModel(item, _checklistService, _checklistItemLogger, RemoveChecklistRow));
                NewChecklistItemText = string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add a checklist item to task {TaskId}", _taskId);
        }
    }

    [RelayCommand]
    private async Task AddTagAsync()
    {
        var name = NewTagText.Trim();
        if (string.IsNullOrEmpty(name) || Tags.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        try
        {
            await _tagService.AssignTagAsync(_taskId, name);
            var assigned = await _tagService.GetTagsForTaskAsync(_taskId);
            var newTag = assigned.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
            if (newTag is not null)
            {
                Tags.Add(new TagChip(newTag.Id, newTag.Name, _taskId, _tagService, _logger, RemoveTagRow));
            }

            NewTagText = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign tag '{TagName}' to task {TaskId}", name, _taskId);
        }
    }

    [RelayCommand]
    private async Task AddSubtaskAsync()
    {
        var title = NewSubtaskTitle.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        try
        {
            var parent = await _taskService.GetTaskAsync(_taskId);
            if (parent is null)
            {
                return;
            }

            var subtask = await _taskService.CreateTaskAsync(parent.PlanDate, title, parentTaskId: _taskId);
            Subtasks.Add(new SubtaskRowViewModel(subtask, _taskService, _logger, RemoveSubtaskRow));
            NewSubtaskTitle = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add a subtask to task {TaskId}", _taskId);
        }
    }

    [RelayCommand]
    private async Task AddBlockerAsync()
    {
        if (SelectedBlockerToAdd is not { Id: { } blockingTaskId })
        {
            return;
        }

        if (Blockers.Any(b => b.BlockingTaskId == blockingTaskId))
        {
            SelectedBlockerToAdd = null;
            return;
        }

        try
        {
            await _taskDependencyService.AddBlockerAsync(_taskId, blockingTaskId);

            var blockers = await _taskDependencyService.GetBlockersAsync(_taskId);
            var added = blockers.FirstOrDefault(d => d.BlockingTaskId == blockingTaskId);
            if (added?.BlockingTask is { } blockingTask)
            {
                Blockers.Add(new BlockerChip(added.Id, blockingTask.Id, blockingTask.Title, blockingTask.IsCompleted, _taskDependencyService, _logger, RemoveBlockerRow));
                OnPropertyChanged(nameof(IsBlocked));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add blocker {BlockingTaskId} to task {TaskId}", blockingTaskId, _taskId);
        }
        finally
        {
            SelectedBlockerToAdd = null;
        }
    }

    /// <summary>Called from <c>TaskEditWindow</c>'s code-behind once the OS file picker returns a local path — not a <c>[RelayCommand]</c>, since the path comes from a picker the Window owns, not a bindable property.</summary>
    public async Task AddAttachmentAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var attachment = await _attachmentService.AddAttachmentAsync(_taskId, sourceFilePath, cancellationToken);
            if (attachment is not null)
            {
                Attachments.Add(new AttachmentRowViewModel(attachment, _attachmentService.GetAbsolutePath(attachment), _attachmentService, _logger, RemoveAttachmentRow, OpenAttachmentRow));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to attach file '{SourcePath}' to task {TaskId}", sourceFilePath, _taskId);
        }
    }

    [RelayCommand]
    private async Task SaveAsTemplateAsync()
    {
        var name = TemplateNameToSave.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        try
        {
            await _templateService.SaveAsTemplateAsync(_taskId, name);
            TemplateNameToSave = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save task {TaskId} as template '{TemplateName}'", _taskId, name);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var title = Title.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        try
        {
            var task = await _taskService.GetTaskAsync(_taskId);
            if (task is null)
            {
                _logger.LogWarning("Task {TaskId} no longer exists; discarding edit", _taskId);
                CancelRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            task.Title = title;
            task.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
            task.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
            task.Priority = Priority;
            task.Type = Type;
            task.CategoryId = SelectedCategory.Id;
            task.DueDate = DueDate?.DateTime;
            task.EstimatedMinutes = EstimatedMinutes.HasValue ? (int)EstimatedMinutes.Value : null;
            task.ColorHex = string.IsNullOrEmpty(SelectedColorHex) ? null : SelectedColorHex;
            task.RecurrenceFrequency = SelectedRecurrenceFrequency;
            task.RecurrenceInterval = Math.Max(1, (int)RecurrenceInterval);
            task.RecurrenceEndDate = RecurrenceEndDate is { } end ? DateOnly.FromDateTime(end.DateTime) : null;
            task.ParentTaskId = SelectedParentTask.Id;
            task.MilestoneId = SelectedMilestone.Id;
            task.ProjectId = SelectedProject.Id;

            await _taskService.UpdateTaskAsync(task);
            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save edits for task {TaskId}", _taskId);
        }
    }

    [RelayCommand]
    private void Cancel() => CancelRequested?.Invoke(this, EventArgs.Empty);
}
