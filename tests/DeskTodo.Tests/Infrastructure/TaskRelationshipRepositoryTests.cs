using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class TaskRelationshipRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly TaskRelationshipRepository _sut;
    private readonly TaskRepository _taskRepository;

    public TaskRelationshipRepositoryTests()
    {
        _sut = new TaskRelationshipRepository(_fixture.ContextFactory);
        _taskRepository = new TaskRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    private async Task<TaskItem> CreateTaskAsync(string title)
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = title };
        await _taskRepository.AddAsync(task);
        return task;
    }

    [Fact]
    public async Task AddAsync_ThenGetForTaskAsync_IncludesTheRelationshipFromEitherSide()
    {
        var source = await CreateTaskAsync("Design the API");
        var target = await CreateTaskAsync("Implement the API");

        await _sut.AddAsync(new TaskRelationship { SourceTaskId = source.Id, TargetTaskId = target.Id, RelationshipType = TaskRelationshipType.DependsOn });

        var fromSource = await _sut.GetForTaskAsync(source.Id);
        var fromTarget = await _sut.GetForTaskAsync(target.Id);

        Assert.Single(fromSource);
        Assert.Single(fromTarget);
        Assert.Equal("Implement the API", fromSource[0].TargetTask?.Title);
        Assert.Equal("Design the API", fromTarget[0].SourceTask?.Title);
    }

    [Fact]
    public async Task GetForTaskAsync_DoesNotIncludeRelationshipsBetweenOtherTasks()
    {
        var a = await CreateTaskAsync("A");
        var b = await CreateTaskAsync("B");
        var c = await CreateTaskAsync("C");
        await _sut.AddAsync(new TaskRelationship { SourceTaskId = b.Id, TargetTaskId = c.Id, RelationshipType = TaskRelationshipType.Related });

        var relationships = await _sut.GetForTaskAsync(a.Id);

        Assert.Empty(relationships);
    }

    [Fact]
    public async Task ExistsAsync_TrueOnlyForTheExactSourceTargetType()
    {
        var source = await CreateTaskAsync("Source");
        var target = await CreateTaskAsync("Target");
        await _sut.AddAsync(new TaskRelationship { SourceTaskId = source.Id, TargetTaskId = target.Id, RelationshipType = TaskRelationshipType.DuplicateOf });

        Assert.True(await _sut.ExistsAsync(source.Id, target.Id, TaskRelationshipType.DuplicateOf));
        Assert.False(await _sut.ExistsAsync(source.Id, target.Id, TaskRelationshipType.Related));
        Assert.False(await _sut.ExistsAsync(target.Id, source.Id, TaskRelationshipType.DuplicateOf));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheRelationship()
    {
        var source = await CreateTaskAsync("Source");
        var target = await CreateTaskAsync("Target");
        var relationship = new TaskRelationship { SourceTaskId = source.Id, TargetTaskId = target.Id, RelationshipType = TaskRelationshipType.FollowUpOf };
        await _sut.AddAsync(relationship);

        await _sut.DeleteAsync(relationship.Id);

        Assert.Empty(await _sut.GetForTaskAsync(source.Id));
    }

    [Fact]
    public async Task DeleteAsync_WithAnUnknownId_DoesNotThrow()
    {
        await _sut.DeleteAsync(Guid.NewGuid());
    }
}
