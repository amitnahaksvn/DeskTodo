using DeskTodo.Domain.Entities;

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
}
