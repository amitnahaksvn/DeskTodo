using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Display/interaction wrapper around a single <see cref="TaskItem"/> for
/// the widget's task list. Owns persisting its own state changes (complete,
/// rename, pin, archive, delete, duplicate) so the view can bind to it
/// directly without <see cref="WidgetViewModel"/> needing a command per row.
/// Delete/duplicate/archive change *which* tasks belong on today's list, so
/// they call back into the constructor's <c>requestListRefresh</c> callback
/// rather than trying to surgically patch the parent's collection themselves.
/// </summary>
public sealed partial class TaskItemViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TaskItemViewModel> _logger;
    private readonly Action _requestListRefresh;
    private readonly Action<Guid> _requestFullEdit;
    private readonly IUndoRedoService _undoRedoService;

    public TaskItemViewModel(TaskItem task, ITaskService taskService, ILogger<TaskItemViewModel> logger, Action requestListRefresh, Action<Guid> requestFullEdit, IUndoRedoService undoRedoService)
    {
        _taskService = taskService;
        _logger = logger;
        _requestListRefresh = requestListRefresh;
        _requestFullEdit = requestFullEdit;
        _undoRedoService = undoRedoService;

        Id = task.Id;
        DisplayNumber = task.DayOrder + 1;
        Priority = task.Priority;
        Type = task.Type;
        PriorityColorHex = GetPriorityColorHex(task.Priority);
        CategoryId = task.CategoryId;
        CategoryName = task.Category?.Name;
        CategoryColorHex = task.Category?.ColorHex;
        ProjectId = task.ProjectId;
        Notes = task.Notes;
        Description = task.Description;
        DueDate = task.DueDate;
        Title = task.Title;
        IsCompleted = task.IsCompleted;
        IsPinned = task.IsPinned;
        IsFavorite = task.IsFavorite;
        SnoozedUntil = task.SnoozedUntil;
        DisplayColorHex = task.ColorHex ?? PriorityColorHex;
        TagIds = task.Tags.Select(t => t.Id).ToList();
        IsSubtask = task.ParentTaskId.HasValue;
        SubtaskCount = task.Subtasks.Count;
        IsBlocked = task.IsBlocked;
    }

    public Guid Id { get; }

    public int DisplayNumber { get; }

    /// <summary>Raw priority, for the search bar's priority filter and "sort by priority" — <see cref="PriorityColorHex"/> is what the row itself displays.</summary>
    public TaskPriority Priority { get; }

    public string PriorityColorHex { get; }

    /// <summary>What kind of activity this is — see <see cref="Domain.Enums.TaskType"/>.</summary>
    public TaskType Type { get; }

    /// <summary>True for every type except the plain, default <see cref="Domain.Enums.TaskType.Task"/> — drives <see cref="TypeIcon"/>'s row visibility, matching how <see cref="SubtaskCount"/>/<see cref="IsBlocked"/> only render something for their non-default case.</summary>
    public bool HasNonDefaultType => Type != TaskType.Task;

    /// <summary>Small row indicator — one icon, not a per-type flag like <see cref="IsBlocked"/>/<see cref="IsFavorite"/>/<see cref="IsPinned"/>, since a task only ever has one <see cref="Type"/> at a time.</summary>
    public string TypeIcon => Type switch
    {
        TaskType.Event => "📅",
        TaskType.Reminder => "⏰",
        TaskType.Note => "📝",
        TaskType.Meeting => "👥",
        _ => "",
    };

    /// <summary>For the search bar's category filter — <see cref="CategoryColorHex"/> is what the row itself displays.</summary>
    public Guid? CategoryId { get; }

    /// <summary>For "sort/group by category" — null sorts to the end, after every real category.</summary>
    public string? CategoryName { get; }

    public string? CategoryColorHex { get; }

    /// <summary>For the search bar's project filter — see <see cref="Domain.Entities.Project"/>.</summary>
    public Guid? ProjectId { get; }

    /// <summary>For the search bar's tag filter — the row itself doesn't display tags inline (only visible in the full-field editor).</summary>
    public IReadOnlyList<Guid> TagIds { get; } = [];

    /// <summary>True when this task is nested under a parent — the row indents itself when set.</summary>
    public bool IsSubtask { get; }

    /// <summary>How many child tasks this one has — shown as a small count badge; 0 renders nothing.</summary>
    public int SubtaskCount { get; }

    /// <summary>Mirrors <see cref="Domain.Entities.TaskItem.IsBlocked"/> as of the last load — shown as a 🔒 row indicator; completing a blocked task is refused server-side regardless of whether this happens to be stale.</summary>
    public bool IsBlocked { get; }

    /// <summary>Not shown in the row itself — searched against by the search bar.</summary>
    public string? Notes { get; }

    /// <summary>Not shown in the row itself — searched against by the search bar.</summary>
    public string? Description { get; }

    /// <summary>For "sort by due date" — the row itself doesn't display this yet (full-field details aren't shown inline).</summary>
    public DateTime? DueDate { get; }

    // Multi-select state. Display-only, like IsCompleted/IsPinned above — never triggers
    // persistence, since selection is a pure view-state concept with nothing to save.
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// Mirrors <c>WidgetViewModel.IsSelectMode</c> — cascaded down (see
    /// <c>WidgetViewModel.ToggleSelectMode</c>/<c>LoadTasksAsync</c>) rather than reached
    /// via an ancestor XAML binding, so the row template can swap its drag-handle for a
    /// selection checkbox with a plain same-DataContext binding.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelectModeActive { get; set; }

    // These display-only properties never trigger persistence from their setters — an
    // On<Property>Changed hook would also fire from this class's own constructor
    // assignments above, silently re-persisting each task's own just-loaded state on
    // every load. Every mutation instead goes through an explicit [RelayCommand] below.
    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial bool IsCompleted { get; set; }

    [ObservableProperty]
    public partial bool IsPinned { get; set; }

    [ObservableProperty]
    public partial bool IsFavorite { get; set; }

    /// <summary>Phase 26's Snooze — while in the future, suppresses this task's overdue re-notification; see <see cref="Domain.Entities.TaskItem.SnoozedUntil"/>.</summary>
    [ObservableProperty]
    public partial DateTime? SnoozedUntil { get; set; }

    /// <summary>True once this task is actually overdue (not just any task) — the row's Snooze action only makes sense to offer then.</summary>
    public bool IsOverdue => !IsCompleted && DueDate is { } due && due < DateTime.UtcNow;

    /// <summary>What the row's priority dot actually renders — <see cref="Domain.Entities.TaskItem.ColorHex"/> (the editor's color picker) when set, else the priority color it used to always show.</summary>
    public string DisplayColorHex { get; }

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string EditingTitle { get; set; } = string.Empty;

    /// <summary>Bound to the row's CheckBox <c>Command</c> (not <c>IsChecked</c> two-way) so only a genuine user click persists anything.</summary>
    [RelayCommand]
    private async Task ToggleCompleteAsync()
    {
        var newValue = !IsCompleted;

        if (await TryAsync(() => newValue ? _taskService.CompleteTaskAsync(Id) : _taskService.ReopenTaskAsync(Id), "toggle completion for"))
        {
            IsCompleted = newValue;

            // Feature 43's Undo/Redo Engine. Undoing a completion that created a recurring
            // next occurrence (TaskService.CompleteTaskAsync) doesn't remove that occurrence —
            // a narrow, documented limitation, not a silent one.
            var id = Id;
            _undoRedoService.Record(
                newValue ? $"Complete \"{Title}\"" : $"Reopen \"{Title}\"",
                undo: () => newValue ? _taskService.ReopenTaskAsync(id) : _taskService.CompleteTaskAsync(id),
                redo: () => newValue ? _taskService.CompleteTaskAsync(id) : _taskService.ReopenTaskAsync(id));
        }
    }

    /// <summary>Enters inline title-edit mode (bound to the title's double-tap gesture).</summary>
    [RelayCommand]
    private void BeginEdit()
    {
        EditingTitle = Title;
        IsEditing = true;
    }

    /// <summary>Opens the full-field editor (description/priority/category/due date/notes) — bound to the context menu's "Edit" item.</summary>
    [RelayCommand]
    private void OpenEditor() => _requestFullEdit(Id);

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    /// <summary>Confirms the inline edit (Enter key). A blank title cancels instead of saving.</summary>
    [RelayCommand]
    private async Task CommitEditAsync()
    {
        var newTitle = EditingTitle.Trim();

        if (string.IsNullOrEmpty(newTitle) || newTitle == Title)
        {
            IsEditing = false;
            return;
        }

        var oldTitle = Title;
        if (await TryAsync(() => _taskService.RenameTaskAsync(Id, newTitle), "rename"))
        {
            Title = newTitle;

            var id = Id;
            _undoRedoService.Record(
                $"Rename \"{oldTitle}\" to \"{newTitle}\"",
                undo: () => _taskService.RenameTaskAsync(id, oldTitle),
                redo: () => _taskService.RenameTaskAsync(id, newTitle));
        }

        IsEditing = false;
    }

    [RelayCommand]
    private async Task TogglePinAsync()
    {
        var newValue = !IsPinned;

        if (await TryAsync(() => newValue ? _taskService.PinTaskAsync(Id) : _taskService.UnpinTaskAsync(Id), "toggle pin for"))
        {
            IsPinned = newValue;

            var id = Id;
            _undoRedoService.Record(
                newValue ? $"Pin \"{Title}\"" : $"Unpin \"{Title}\"",
                undo: () => newValue ? _taskService.UnpinTaskAsync(id) : _taskService.PinTaskAsync(id),
                redo: () => newValue ? _taskService.PinTaskAsync(id) : _taskService.UnpinTaskAsync(id));
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        var newValue = !IsFavorite;

        if (await TryAsync(() => newValue ? _taskService.FavoriteTaskAsync(Id) : _taskService.UnfavoriteTaskAsync(Id), "toggle favorite for"))
        {
            IsFavorite = newValue;
        }
    }

    /// <summary>
    /// Snoozes this task's overdue re-notification for one hour — bound to the row's
    /// context-menu "Snooze" item, only shown while <see cref="IsOverdue"/>. Local time, not
    /// UTC — <c>WidgetViewModel.CheckForOverdueTaskNotificationsAsync</c> compares
    /// <see cref="Domain.Entities.TaskItem.SnoozedUntil"/> against <c>DueDate</c> using the
    /// same local "now" it already uses for the overdue check itself, so this has to match.
    /// </summary>
    [RelayCommand]
    private async Task SnoozeAsync()
    {
        var until = DateTime.Now.AddHours(1);

        if (await TryAsync(() => _taskService.SnoozeTaskAsync(Id, until), "snooze"))
        {
            SnoozedUntil = until;
        }
    }

    [RelayCommand]
    private async Task ArchiveAsync()
    {
        if (await TryAsync(() => _taskService.ArchiveTaskAsync(Id), "archive"))
        {
            _requestListRefresh();
        }
    }

    [RelayCommand]
    private async Task DuplicateAsync()
    {
        if (await TryAsync(() => _taskService.DuplicateTaskAsync(Id), "duplicate"))
        {
            _requestListRefresh();
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (await TryAsync(() => _taskService.DeleteTaskAsync(Id), "delete"))
        {
            var id = Id;
            var title = Title;
            _undoRedoService.Record(
                $"Delete \"{title}\"",
                undo: () => _taskService.RestoreTaskAsync(id),
                redo: () => _taskService.DeleteTaskAsync(id));

            _requestListRefresh();
        }
    }

    private async Task<bool> TryAsync(Func<Task> action, string verbPhrase)
    {
        try
        {
            await action();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to {VerbPhrase} task {TaskId}", verbPhrase, Id);
            return false;
        }
    }

    private static string GetPriorityColorHex(TaskPriority priority) => PriorityColors.ForPriority(priority);
}
