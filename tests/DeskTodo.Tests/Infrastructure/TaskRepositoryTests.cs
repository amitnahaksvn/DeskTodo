using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Tests.Infrastructure;

// A fresh SqliteInMemoryFixture per test (xUnit creates a new test class
// instance per test method) keeps tests isolated from each other.
public class TaskRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly TaskRepository _sut;

    public TaskRepositoryTests()
    {
        _sut = new TaskRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AddAsync_ThenGetByDateAsync_ReturnsTheTask()
    {
        var planDate = new DateOnly(2026, 7, 27);
        var task = new TaskItem { PlanDate = planDate, Title = "Drink Water" };

        await _sut.AddAsync(task);
        var results = await _sut.GetByDateAsync(planDate);

        Assert.Single(results);
        Assert.Equal("Drink Water", results[0].Title);
    }

    [Fact]
    public async Task GetByDateAsync_OrdersByDayOrder()
    {
        var planDate = new DateOnly(2026, 7, 27);
        await _sut.AddAsync(new TaskItem { PlanDate = planDate, Title = "Second", DayOrder = 1 });
        await _sut.AddAsync(new TaskItem { PlanDate = planDate, Title = "First", DayOrder = 0 });

        var results = await _sut.GetByDateAsync(planDate);

        Assert.Equal(["First", "Second"], results.Select(t => t.Title));
    }

    [Fact]
    public async Task GetByDateAsync_ExcludesDeletedAndArchivedTasks()
    {
        var planDate = new DateOnly(2026, 7, 27);
        var deleted = new TaskItem { PlanDate = planDate, Title = "Deleted" };
        deleted.SoftDelete();
        var archived = new TaskItem { PlanDate = planDate, Title = "Archived" };
        archived.Archive();
        await _sut.AddAsync(deleted);
        await _sut.AddAsync(archived);
        await _sut.AddAsync(new TaskItem { PlanDate = planDate, Title = "Visible" });

        var results = await _sut.GetByDateAsync(planDate);

        Assert.Equal(["Visible"], results.Select(t => t.Title));
    }

    [Fact]
    public async Task GetMaxDayOrderAsync_OnEmptyDay_ReturnsMinusOne()
    {
        var maxOrder = await _sut.GetMaxDayOrderAsync(new DateOnly(2026, 7, 27));

        Assert.Equal(-1, maxOrder);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChangesMadeToADetachedTask()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Team Meeting" };
        await _sut.AddAsync(task);

        var fetched = await _sut.GetByIdAsync(task.Id);
        Assert.NotNull(fetched);
        fetched.Complete();
        await _sut.UpdateAsync(fetched);

        var reFetched = await _sut.GetByIdAsync(task.Id);
        Assert.NotNull(reFetched);
        Assert.True(reFetched.IsCompleted);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesChecklistItemsOrderedAndTags()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Plan trip" };
        task.ChecklistItems.Add(new ChecklistItem { TaskId = task.Id, Text = "Second", Order = 1 });
        task.ChecklistItems.Add(new ChecklistItem { TaskId = task.Id, Text = "First", Order = 0 });
        await _sut.AddAsync(task);

        var fetched = await _sut.GetByIdAsync(task.Id);

        Assert.NotNull(fetched);
        Assert.Equal(["First", "Second"], fetched.ChecklistItems.Select(c => c.Text));
    }

    [Fact]
    public async Task GetAllAsync_IncludesChecklistItems()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Plan trip" };
        task.ChecklistItems.Add(new ChecklistItem { TaskId = task.Id, Text = "Pack bags", IsChecked = true });
        task.ChecklistItems.Add(new ChecklistItem { TaskId = task.Id, Text = "Book flights", IsChecked = false });
        await _sut.AddAsync(task);

        var results = await _sut.GetAllAsync();

        var fetched = Assert.Single(results);
        Assert.Equal(2, fetched.ChecklistItems.Count);
        Assert.Equal(1, fetched.ChecklistItems.Count(c => c.IsChecked));
    }

    [Fact]
    public async Task GetByIdAsync_IncludesSubtasksAndBlockers()
    {
        var parent = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Ship release" };
        await _sut.AddAsync(parent);
        var subtask = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Write changelog", ParentTaskId = parent.Id };
        await _sut.AddAsync(subtask);
        var blocker = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Finish tests" };
        await _sut.AddAsync(blocker);

        await using (var context = _fixture.ContextFactory.CreateDbContext())
        {
            context.TaskDependencies.Add(new DeskTodo.Domain.Entities.TaskDependency { BlockingTaskId = blocker.Id, BlockedTaskId = parent.Id });
            await context.SaveChangesAsync();
        }

        var fetched = await _sut.GetByIdAsync(parent.Id);

        Assert.NotNull(fetched);
        Assert.Equal(["Write changelog"], fetched.Subtasks.Select(t => t.Title));
        Assert.True(fetched.IsBlocked);
    }

    [Fact]
    public async Task GetByDateAsync_IncludesSubtasksAndBlockers()
    {
        var planDate = new DateOnly(2026, 7, 27);
        var parent = new TaskItem { PlanDate = planDate, Title = "Ship release" };
        await _sut.AddAsync(parent);
        var subtask = new TaskItem { PlanDate = planDate, Title = "Write changelog", ParentTaskId = parent.Id };
        await _sut.AddAsync(subtask);
        var blocker = new TaskItem { PlanDate = planDate, Title = "Finish tests" };
        await _sut.AddAsync(blocker);

        await using (var context = _fixture.ContextFactory.CreateDbContext())
        {
            context.TaskDependencies.Add(new DeskTodo.Domain.Entities.TaskDependency { BlockingTaskId = blocker.Id, BlockedTaskId = parent.Id });
            await context.SaveChangesAsync();
        }

        var results = await _sut.GetByDateAsync(planDate);

        var fetchedParent = results.Single(t => t.Id == parent.Id);
        Assert.Single(fetchedParent.Subtasks);
        Assert.True(fetchedParent.IsBlocked);
    }

    [Fact]
    public async Task GetIncompleteBeforeDateAsync_ReturnsOnlyIncompleteNonArchivedNonDeletedPastTasks()
    {
        var past = new DateOnly(2026, 7, 20);
        var today = new DateOnly(2026, 7, 27);

        var overdue = new TaskItem { PlanDate = past, Title = "Overdue" };
        var completed = new TaskItem { PlanDate = past, Title = "Completed" };
        completed.Complete();
        var archived = new TaskItem { PlanDate = past, Title = "Archived" };
        archived.Archive();
        var futureTask = new TaskItem { PlanDate = today, Title = "Future" };

        await _sut.AddAsync(overdue);
        await _sut.AddAsync(completed);
        await _sut.AddAsync(archived);
        await _sut.AddAsync(futureTask);

        var results = await _sut.GetIncompleteBeforeDateAsync(today);

        Assert.Equal(["Overdue"], results.Select(t => t.Title));
    }

    [Fact]
    public async Task ReorderAsync_ReassignsDayOrderToMatchTheGivenSequence()
    {
        var planDate = new DateOnly(2026, 7, 27);
        var a = new TaskItem { PlanDate = planDate, Title = "A", DayOrder = 0 };
        var b = new TaskItem { PlanDate = planDate, Title = "B", DayOrder = 1 };
        await _sut.AddAsync(a);
        await _sut.AddAsync(b);

        await _sut.ReorderAsync(planDate, [b.Id, a.Id]);

        var results = await _sut.GetByDateAsync(planDate);
        Assert.Equal(["B", "A"], results.Select(t => t.Title));
    }

    [Fact]
    public async Task GetDeletedAsync_ReturnsOnlySoftDeletedTasks_MostRecentlyDeletedFirst()
    {
        var planDate = new DateOnly(2026, 7, 27);
        var olderDeleted = new TaskItem { PlanDate = planDate, Title = "Older" };
        olderDeleted.SoftDelete();
        await _sut.AddAsync(olderDeleted);

        await Task.Delay(10); // Ensures a distinct, later DeletedAt for the second one.
        var newerDeleted = new TaskItem { PlanDate = planDate, Title = "Newer" };
        newerDeleted.SoftDelete();
        await _sut.AddAsync(newerDeleted);

        await _sut.AddAsync(new TaskItem { PlanDate = planDate, Title = "Not deleted" });

        var results = await _sut.GetDeletedAsync();

        Assert.Equal(["Newer", "Older"], results.Select(t => t.Title));
    }

    [Fact]
    public async Task RemoveAsync_PermanentlyDeletesTheTask()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Gone forever" };
        task.SoftDelete();
        await _sut.AddAsync(task);

        await _sut.RemoveAsync(task.Id);

        var results = await _sut.GetDeletedAsync();
        Assert.Empty(results);
    }

    [Fact]
    public async Task RemoveAsync_OnATaskWithDependencies_RemovesTheDependencyRowsTooInsteadOfThrowing()
    {
        var blocked = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Blocked task" };
        blocked.SoftDelete();
        await _sut.AddAsync(blocked);
        var blocker = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Blocker task" };
        await _sut.AddAsync(blocker);

        await using (var context = _fixture.ContextFactory.CreateDbContext())
        {
            context.TaskDependencies.Add(new TaskDependency { BlockingTaskId = blocker.Id, BlockedTaskId = blocked.Id });
            await context.SaveChangesAsync();
        }

        var exception = await Record.ExceptionAsync(() => _sut.RemoveAsync(blocked.Id));

        Assert.Null(exception);
        await using var verifyContext = _fixture.ContextFactory.CreateDbContext();
        Assert.False(await verifyContext.TaskDependencies.AnyAsync(d => d.BlockedTaskId == blocked.Id));
    }

    [Fact]
    public async Task RemoveAsync_OnATaskWithRelationships_RemovesTheRelationshipRowsTooInsteadOfThrowing()
    {
        var source = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Source task" };
        source.SoftDelete();
        await _sut.AddAsync(source);
        var target = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Target task" };
        await _sut.AddAsync(target);

        await using (var context = _fixture.ContextFactory.CreateDbContext())
        {
            context.TaskRelationships.Add(new TaskRelationship { SourceTaskId = source.Id, TargetTaskId = target.Id, RelationshipType = TaskRelationshipType.Related });
            await context.SaveChangesAsync();
        }

        var exception = await Record.ExceptionAsync(() => _sut.RemoveAsync(source.Id));

        Assert.Null(exception);
        await using var verifyContext = _fixture.ContextFactory.CreateDbContext();
        Assert.False(await verifyContext.TaskRelationships.AnyAsync(r => r.SourceTaskId == source.Id));
    }

    [Fact]
    public async Task RemoveAsync_OnATaskWithSubtasks_OrphansTheSubtasksInsteadOfDeletingThem()
    {
        var parent = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Parent" };
        parent.SoftDelete();
        await _sut.AddAsync(parent);
        var subtask = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Still-live subtask", ParentTaskId = parent.Id };
        await _sut.AddAsync(subtask);

        await _sut.RemoveAsync(parent.Id);

        var results = await _sut.GetByDateAsync(new DateOnly(2026, 7, 27));
        var survivingSubtask = Assert.Single(results);
        Assert.Equal("Still-live subtask", survivingSubtask.Title);
        Assert.Null(survivingSubtask.ParentTaskId);
    }
}
