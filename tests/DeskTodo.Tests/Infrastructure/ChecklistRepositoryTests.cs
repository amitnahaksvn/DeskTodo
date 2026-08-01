using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class ChecklistRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly ChecklistRepository _sut;
    private readonly TaskRepository _taskRepository;

    public ChecklistRepositoryTests()
    {
        _sut = new ChecklistRepository(_fixture.ContextFactory);
        _taskRepository = new TaskRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    private async Task<Guid> CreateTaskAsync()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Plan trip" };
        await _taskRepository.AddAsync(task);
        return task.Id;
    }

    [Fact]
    public async Task AddAsync_ThenGetByTaskIdAsync_ReturnsItemsInOrder()
    {
        var taskId = await CreateTaskAsync();
        await _sut.AddAsync(new ChecklistItem { TaskId = taskId, Text = "Second", Order = 1 });
        await _sut.AddAsync(new ChecklistItem { TaskId = taskId, Text = "First", Order = 0 });

        var results = await _sut.GetByTaskIdAsync(taskId);

        Assert.Equal(["First", "Second"], results.Select(c => c.Text));
    }

    [Fact]
    public async Task GetMaxOrderAsync_OnEmptyChecklist_ReturnsMinusOne()
    {
        var taskId = await CreateTaskAsync();

        var maxOrder = await _sut.GetMaxOrderAsync(taskId);

        Assert.Equal(-1, maxOrder);
    }

    [Fact]
    public async Task UpdateAsync_PersistsIsCheckedChange()
    {
        var taskId = await CreateTaskAsync();
        var item = new ChecklistItem { TaskId = taskId, Text = "Buy tickets" };
        await _sut.AddAsync(item);

        var fetched = await _sut.GetByIdAsync(item.Id);
        Assert.NotNull(fetched);
        fetched.IsChecked = true;
        await _sut.UpdateAsync(fetched);

        var reFetched = await _sut.GetByIdAsync(item.Id);
        Assert.NotNull(reFetched);
        Assert.True(reFetched.IsChecked);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheItem()
    {
        var taskId = await CreateTaskAsync();
        var item = new ChecklistItem { TaskId = taskId, Text = "Pack bags" };
        await _sut.AddAsync(item);

        await _sut.DeleteAsync(item.Id);

        Assert.Null(await _sut.GetByIdAsync(item.Id));
    }

    [Fact]
    public async Task DeleteAsync_OnMissingId_DoesNotThrow()
    {
        await _sut.DeleteAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task AddRangeAsync_InsertsEveryItem()
    {
        var taskId = await CreateTaskAsync();

        await _sut.AddRangeAsync(
        [
            new ChecklistItem { TaskId = taskId, Text = "A", Order = 0 },
            new ChecklistItem { TaskId = taskId, Text = "B", Order = 1 },
        ]);

        var results = await _sut.GetByTaskIdAsync(taskId);
        Assert.Equal(["A", "B"], results.Select(c => c.Text));
    }
}
