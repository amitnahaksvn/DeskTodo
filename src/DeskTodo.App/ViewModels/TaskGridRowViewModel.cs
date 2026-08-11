using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// A single editable row in the Excel-style grid view (<see cref="GridViewModel"/>).
/// Carries its own copies of <see cref="Priorities"/> and a reference to the shared
/// <see cref="Categories"/> list so each cell's editing <c>ComboBox</c> can bind its
/// <c>ItemsSource</c> directly (the cell template's DataContext is this row, not the
/// parent <see cref="GridViewModel"/>) — the same "give the item what it needs
/// directly" reasoning <see cref="TagChip"/>/<see cref="ChecklistItemRowViewModel"/>
/// use, chosen over an ambient parent-DataContext XAML binding.
/// </summary>
public sealed partial class TaskGridRowViewModel : ObservableObject
{
    public TaskGridRowViewModel(TaskItem task, IReadOnlyList<TaskPriority> priorities, ObservableCollection<CategoryOption> categories)
    {
        Id = task.Id;
        Priorities = priorities;
        Categories = categories;

        Title = task.Title;
        PlanDate = task.PlanDate.ToDateTime(TimeOnly.MinValue);
        Priority = task.Priority;
        Category = categories.FirstOrDefault(c => c.Id == task.CategoryId) ?? CategoryOption.None;
        DueDate = task.DueDate is { } due ? new DateTimeOffset(due) : null;
        IsCompleted = task.IsCompleted;
        Notes = task.Notes ?? string.Empty;
        ChecklistTotalCount = task.ChecklistItems.Count;
        ChecklistCheckedCount = task.ChecklistItems.Count(c => c.IsChecked);
        IsFavorite = task.IsFavorite;
        IsPinned = task.IsPinned;
        ProjectId = task.ProjectId;
    }

    public Guid Id { get; }

    public IReadOnlyList<TaskPriority> Priorities { get; }

    public ObservableCollection<CategoryOption> Categories { get; }

    /// <summary>Bound to the row's selection checkbox column — pure view state, never persisted (see <see cref="GridViewModel"/>'s property-changed handler).</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlanDateDisplay))]
    public partial DateTimeOffset PlanDate { get; set; }

    public string PlanDateDisplay => PlanDate.ToString("MMM d, yyyy");

    [ObservableProperty]
    public partial TaskPriority Priority { get; set; }

    [ObservableProperty]
    public partial CategoryOption Category { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DueDateDisplay))]
    [NotifyPropertyChangedFor(nameof(StatusDisplay))]
    public partial DateTimeOffset? DueDate { get; set; }

    public string DueDateDisplay => DueDate?.ToString("MMM d, yyyy") ?? "—";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusDisplay))]
    public partial bool IsCompleted { get; set; }

    [ObservableProperty]
    public partial string Notes { get; set; }

    /// <summary>Derived, not a raw field — Phase 20's "Status column" ask. Recomputed whenever <see cref="IsCompleted"/>/<see cref="DueDate"/> change.</summary>
    public string StatusDisplay => IsCompleted switch
    {
        true => "Done",
        false when DueDate is { } due && due.Date < DateTimeOffset.Now.Date => "Overdue",
        false when DueDate is { } due && due.Date == DateTimeOffset.Now.Date => "Due Today",
        false when DueDate is not null => "Upcoming",
        _ => "No due date",
    };

    /// <summary>How many of this task's checklist items are checked off — "—" for a task with no checklist. Fixed at load time; the grid doesn't edit checklists (that stays in the full-field editor), so this never needs to change mid-session.</summary>
    public int ChecklistTotalCount { get; }

    public int ChecklistCheckedCount { get; }

    public string ProgressDisplay => ChecklistTotalCount == 0 ? "—" : $"{ChecklistCheckedCount}/{ChecklistTotalCount}";

    /// <summary>For Phase 25's Smart Lists ("Favorites" quick filter) — not shown as its own grid column/cell (no editing here; toggled from the widget row or full-field editor).</summary>
    public bool IsFavorite { get; }

    /// <summary>For Phase 25's Smart Lists ("Pinned" quick filter) — same non-editable-here reasoning as <see cref="IsFavorite"/>.</summary>
    public bool IsPinned { get; }

    /// <summary>For the search bar's project filter and the "No Project" Smart List — no editable Project column here, same as Milestone (only Category got one; see <see cref="GridViewModel"/>'s doc comment).</summary>
    public Guid? ProjectId { get; }
}
