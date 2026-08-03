using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class MilestoneRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly MilestoneRepository _sut;
    private readonly TaskRepository _taskRepository;

    public MilestoneRepositoryTests()
    {
        _sut = new MilestoneRepository(_fixture.ContextFactory);
        _taskRepository = new TaskRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsTheMilestone()
    {
        var milestone = new Milestone { Title = "Ship v1", TargetDate = new DateOnly(2026, 9, 1) };

        await _sut.AddAsync(milestone);
        var fetched = await _sut.GetByIdAsync(milestone.Id);

        Assert.NotNull(fetched);
        Assert.Equal("Ship v1", fetched.Title);
        Assert.Equal(new DateOnly(2026, 9, 1), fetched.TargetDate);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByTargetDate_NullsLast()
    {
        var noDate = new Milestone { Title = "Someday" };
        var later = new Milestone { Title = "Later", TargetDate = new DateOnly(2026, 12, 1) };
        var sooner = new Milestone { Title = "Sooner", TargetDate = new DateOnly(2026, 9, 1) };
        await _sut.AddAsync(noDate);
        await _sut.AddAsync(later);
        await _sut.AddAsync(sooner);

        var results = await _sut.GetAllAsync();

        Assert.Equal(["Sooner", "Later", "Someday"], results.Select(m => m.Title));
    }

    [Fact]
    public async Task GetAllAsync_IncludesLinkedTasks()
    {
        var milestone = new Milestone { Title = "Ship v1" };
        await _sut.AddAsync(milestone);
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Write docs", MilestoneId = milestone.Id };
        await _taskRepository.AddAsync(task);

        var results = await _sut.GetAllAsync();

        Assert.Single(results.Single(m => m.Id == milestone.Id).Tasks);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var milestone = new Milestone { Title = "Ship v1" };
        await _sut.AddAsync(milestone);
        milestone.IsCompleted = true;
        milestone.Title = "Ship v1.0";

        await _sut.UpdateAsync(milestone);

        var fetched = await _sut.GetByIdAsync(milestone.Id);
        Assert.True(fetched!.IsCompleted);
        Assert.Equal("Ship v1.0", fetched.Title);
    }

    [Fact]
    public async Task DeleteAsync_UnlinksTasksRatherThanDeletingThem()
    {
        var milestone = new Milestone { Title = "Ship v1" };
        await _sut.AddAsync(milestone);
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Write docs", MilestoneId = milestone.Id };
        await _taskRepository.AddAsync(task);

        await _sut.DeleteAsync(milestone.Id);

        Assert.Null(await _sut.GetByIdAsync(milestone.Id));
        var fetchedTask = await _taskRepository.GetByIdAsync(task.Id);
        Assert.NotNull(fetchedTask);
        Assert.Null(fetchedTask.MilestoneId);
    }

    [Fact]
    public async Task DeleteAsync_OnMissingId_DoesNotThrow()
    {
        await _sut.DeleteAsync(Guid.NewGuid());
    }
}
