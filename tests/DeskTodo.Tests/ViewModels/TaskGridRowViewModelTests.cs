using System.Collections.ObjectModel;
using DeskTodo.App.ViewModels;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Tests.ViewModels;

public class TaskGridRowViewModelTests
{
    private static readonly IReadOnlyList<TaskPriority> Priorities = Enum.GetValues<TaskPriority>();
    private static readonly ObservableCollection<CategoryOption> Categories = [CategoryOption.None];

    private static TaskGridRowViewModel CreateRow(TaskItem task) => new(task, Priorities, Categories);

    [Fact]
    public void StatusDisplay_WhenCompleted_IsDone()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Task" };
        task.Complete();

        Assert.Equal("Done", CreateRow(task).StatusDisplay);
    }

    [Fact]
    public void StatusDisplay_WhenIncompleteWithNoDueDate_IsNoDueDate()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Task" };

        Assert.Equal("No due date", CreateRow(task).StatusDisplay);
    }

    [Fact]
    public void StatusDisplay_WhenIncompleteWithAPastDueDate_IsOverdue()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Task", DueDate = DateTime.Now.AddDays(-2) };

        Assert.Equal("Overdue", CreateRow(task).StatusDisplay);
    }

    [Fact]
    public void StatusDisplay_WhenIncompleteWithATodayDueDate_IsDueToday()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Task", DueDate = DateTime.Now };

        Assert.Equal("Due Today", CreateRow(task).StatusDisplay);
    }

    [Fact]
    public void StatusDisplay_WhenIncompleteWithAFutureDueDate_IsUpcoming()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Task", DueDate = DateTime.Now.AddDays(3) };

        Assert.Equal("Upcoming", CreateRow(task).StatusDisplay);
    }

    [Fact]
    public void StatusDisplay_RecomputesWhenIsCompletedChanges()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Task", DueDate = DateTime.Now.AddDays(-2) };
        var row = CreateRow(task);
        Assert.Equal("Overdue", row.StatusDisplay);

        row.IsCompleted = true;

        Assert.Equal("Done", row.StatusDisplay);
    }

    [Fact]
    public void ProgressDisplay_WithNoChecklist_IsADash()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Task" };

        Assert.Equal("—", CreateRow(task).ProgressDisplay);
    }

    [Fact]
    public void ProgressDisplay_WithAChecklist_ShowsCheckedOverTotal()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Task" };
        task.ChecklistItems.Add(new ChecklistItem { TaskId = task.Id, Text = "A", IsChecked = true });
        task.ChecklistItems.Add(new ChecklistItem { TaskId = task.Id, Text = "B", IsChecked = false });
        task.ChecklistItems.Add(new ChecklistItem { TaskId = task.Id, Text = "C", IsChecked = true });

        Assert.Equal("2/3", CreateRow(task).ProgressDisplay);
    }
}
