using CommunityToolkit.Mvvm.Input;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// A read-only task row shared across every Phase 21 planner tab (Agenda/Timeline/Kanban/
/// Matrix) that needs to show "a task, briefly" — title, priority color, category, due
/// date. None of these tabs edit a task's fields directly (that stays the full-field
/// editor's job); <see cref="SelectCommand"/> only ever navigates the widget to the task's
/// day, the same "click a cell/row to jump there" interaction every planner view uses.
/// </summary>
public sealed class PlannerTaskRowViewModel
{
    public PlannerTaskRowViewModel(TaskItem task, Action<DateOnly> requestSelect)
    {
        Id = task.Id;
        Title = task.Title;
        IsCompleted = task.IsCompleted;
        Priority = task.Priority;
        PriorityColorHex = PriorityColors.ForPriority(task.Priority);
        PlanDate = task.PlanDate;
        DueDate = task.DueDate;
        CategoryName = task.Category?.Name;
        SelectCommand = new RelayCommand(() => requestSelect(task.PlanDate));
    }

    public Guid Id { get; }

    public string Title { get; }

    public bool IsCompleted { get; }

    public TaskPriority Priority { get; }

    public string PriorityColorHex { get; }

    public DateOnly PlanDate { get; }

    public DateTime? DueDate { get; }

    public string? CategoryName { get; }

    public IRelayCommand SelectCommand { get; }
}
