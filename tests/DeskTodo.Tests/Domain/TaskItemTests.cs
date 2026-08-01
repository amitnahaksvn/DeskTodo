using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Tests.Domain;

public class TaskItemTests
{
    private static TaskItem CreateTask(DateTime? dueDate = null) => new()
    {
        PlanDate = DateOnly.FromDateTime(DateTime.UtcNow),
        Title = "Test task",
        DueDate = dueDate,
    };

    [Fact]
    public void Complete_SetsIsCompletedAndCompletedAt()
    {
        var task = CreateTask();

        task.Complete();

        Assert.True(task.IsCompleted);
        Assert.NotNull(task.CompletedAt);
    }

    [Fact]
    public void Reopen_UndoesCompletion()
    {
        var task = CreateTask();
        task.Complete();

        task.Reopen();

        Assert.False(task.IsCompleted);
        Assert.Null(task.CompletedAt);
    }

    [Fact]
    public void Pin_ThenUnpin_TogglesIsPinned()
    {
        var task = CreateTask();

        task.Pin();
        Assert.True(task.IsPinned);

        task.Unpin();
        Assert.False(task.IsPinned);
    }

    [Fact]
    public void Archive_ThenRestore_ClearsArchivedAndDeleted()
    {
        var task = CreateTask();
        task.Archive();
        task.SoftDelete();

        task.Restore();

        Assert.False(task.IsArchived);
        Assert.False(task.IsDeleted);
    }

    [Fact]
    public void SoftDelete_SetsIsDeleted()
    {
        var task = CreateTask();

        task.SoftDelete();

        Assert.True(task.IsDeleted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IsOverdue_TrueOnlyWhenPastDueAndNotCompleted(bool complete)
    {
        var task = CreateTask(dueDate: DateTime.UtcNow.AddDays(-1));
        if (complete)
        {
            task.Complete();
        }

        Assert.Equal(!complete, task.IsOverdue);
    }

    [Fact]
    public void IsOverdue_FalseWhenNoDueDate()
    {
        var task = CreateTask(dueDate: null);

        Assert.False(task.IsOverdue);
    }

    [Fact]
    public void IsOverdue_FalseWhenDueDateInFuture()
    {
        var task = CreateTask(dueDate: DateTime.UtcNow.AddDays(1));

        Assert.False(task.IsOverdue);
    }

    [Fact]
    public void Touch_AdvancesModifiedAt()
    {
        var task = CreateTask();
        var before = task.ModifiedAt;

        Thread.Sleep(5);
        task.Touch();

        Assert.True(task.ModifiedAt > before);
    }

    [Fact]
    public void MarkFavorite_ThenUnmarkFavorite_TogglesIsFavorite()
    {
        var task = CreateTask();

        task.MarkFavorite();
        Assert.True(task.IsFavorite);

        task.UnmarkFavorite();
        Assert.False(task.IsFavorite);
    }

    [Fact]
    public void GetNextOccurrencePlanDate_WhenNotRecurring_ReturnsNull()
    {
        var task = CreateTask();
        task.RecurrenceFrequency = RecurrenceFrequency.None;

        Assert.Null(task.GetNextOccurrencePlanDate());
    }

    [Theory]
    [InlineData(RecurrenceFrequency.Daily, 1, "2026-01-02")]
    [InlineData(RecurrenceFrequency.Daily, 3, "2026-01-04")]
    [InlineData(RecurrenceFrequency.Weekly, 1, "2026-01-08")]
    [InlineData(RecurrenceFrequency.Weekly, 2, "2026-01-15")]
    [InlineData(RecurrenceFrequency.Monthly, 1, "2026-02-01")]
    public void GetNextOccurrencePlanDate_AdvancesByFrequencyAndInterval(RecurrenceFrequency frequency, int interval, string expected)
    {
        var task = CreateTask();
        task.PlanDate = new DateOnly(2026, 1, 1);
        task.RecurrenceFrequency = frequency;
        task.RecurrenceInterval = interval;

        Assert.Equal(DateOnly.Parse(expected), task.GetNextOccurrencePlanDate());
    }

    [Fact]
    public void GetNextOccurrencePlanDate_PastRecurrenceEndDate_ReturnsNull()
    {
        var task = CreateTask();
        task.PlanDate = new DateOnly(2026, 1, 1);
        task.RecurrenceFrequency = RecurrenceFrequency.Daily;
        task.RecurrenceEndDate = new DateOnly(2026, 1, 1); // Next occurrence (Jan 2) is past this.

        Assert.Null(task.GetNextOccurrencePlanDate());
    }

    [Fact]
    public void GetNextOccurrencePlanDate_OnRecurrenceEndDate_StillReturnsIt()
    {
        var task = CreateTask();
        task.PlanDate = new DateOnly(2026, 1, 1);
        task.RecurrenceFrequency = RecurrenceFrequency.Daily;
        task.RecurrenceEndDate = new DateOnly(2026, 1, 2); // Next occurrence lands exactly on the end date.

        Assert.Equal(new DateOnly(2026, 1, 2), task.GetNextOccurrencePlanDate());
    }

    [Fact]
    public void GetNextOccurrencePlanDate_WithZeroOrNegativeInterval_TreatsItAsOne()
    {
        var task = CreateTask();
        task.PlanDate = new DateOnly(2026, 1, 1);
        task.RecurrenceFrequency = RecurrenceFrequency.Daily;
        task.RecurrenceInterval = 0;

        Assert.Equal(new DateOnly(2026, 1, 2), task.GetNextOccurrencePlanDate());
    }

    [Fact]
    public void IsBlocked_WithNoDependencies_IsFalse()
    {
        var task = CreateTask();

        Assert.False(task.IsBlocked);
    }

    [Fact]
    public void IsBlocked_WithAnIncompleteBlocker_IsTrue()
    {
        var task = CreateTask();
        var blocker = CreateTask();
        task.BlockedByDependencies.Add(new TaskDependency { BlockingTaskId = blocker.Id, BlockingTask = blocker, BlockedTaskId = task.Id });

        Assert.True(task.IsBlocked);
    }

    [Fact]
    public void IsBlocked_WhenEveryBlockerIsComplete_IsFalse()
    {
        var task = CreateTask();
        var blocker = CreateTask();
        blocker.Complete();
        task.BlockedByDependencies.Add(new TaskDependency { BlockingTaskId = blocker.Id, BlockingTask = blocker, BlockedTaskId = task.Id });

        Assert.False(task.IsBlocked);
    }
}
