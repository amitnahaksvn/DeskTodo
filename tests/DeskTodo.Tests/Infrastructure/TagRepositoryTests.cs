using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class TagRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly TagRepository _sut;
    private readonly TaskRepository _taskRepository;

    public TagRepositoryTests()
    {
        _sut = new TagRepository(_fixture.ContextFactory);
        _taskRepository = new TaskRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    private async Task<Guid> CreateTaskAsync()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Ship release" };
        await _taskRepository.AddAsync(task);
        return task.Id;
    }

    [Fact]
    public async Task GetOrCreateByNameAsync_WhenNoneExists_CreatesANewTag()
    {
        var tag = await _sut.GetOrCreateByNameAsync("Work");

        var all = await _sut.GetAllAsync();
        Assert.Equal(["Work"], all.Select(t => t.Name));
        Assert.Equal(tag.Id, all[0].Id);
    }

    [Fact]
    public async Task GetOrCreateByNameAsync_IsCaseInsensitive_AndReturnsTheExistingTag()
    {
        var first = await _sut.GetOrCreateByNameAsync("Work");
        var second = await _sut.GetOrCreateByNameAsync("WORK");

        Assert.Equal(first.Id, second.Id);
        Assert.Single(await _sut.GetAllAsync());
    }

    [Fact]
    public async Task AssignToTaskAsync_ThenGetForTaskAsync_ReturnsTheTag()
    {
        var taskId = await CreateTaskAsync();
        var tag = await _sut.GetOrCreateByNameAsync("Urgent");

        await _sut.AssignToTaskAsync(taskId, tag.Id);

        var forTask = await _sut.GetForTaskAsync(taskId);
        Assert.Equal(["Urgent"], forTask.Select(t => t.Name));
    }

    [Fact]
    public async Task AssignToTaskAsync_CalledTwice_DoesNotDuplicate()
    {
        var taskId = await CreateTaskAsync();
        var tag = await _sut.GetOrCreateByNameAsync("Urgent");

        await _sut.AssignToTaskAsync(taskId, tag.Id);
        await _sut.AssignToTaskAsync(taskId, tag.Id);

        var forTask = await _sut.GetForTaskAsync(taskId);
        Assert.Single(forTask);
    }

    [Fact]
    public async Task RemoveFromTaskAsync_RemovesTheAssignmentButNotTheTag()
    {
        var taskId = await CreateTaskAsync();
        var tag = await _sut.GetOrCreateByNameAsync("Urgent");
        await _sut.AssignToTaskAsync(taskId, tag.Id);

        await _sut.RemoveFromTaskAsync(taskId, tag.Id);

        Assert.Empty(await _sut.GetForTaskAsync(taskId));
        Assert.Single(await _sut.GetAllAsync());
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheTagEntirely()
    {
        var tag = await _sut.GetOrCreateByNameAsync("Temp");

        await _sut.DeleteAsync(tag.Id);

        Assert.Empty(await _sut.GetAllAsync());
    }
}
