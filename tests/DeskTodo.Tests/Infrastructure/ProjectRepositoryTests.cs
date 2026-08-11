using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class ProjectRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly ProjectRepository _sut;
    private readonly TaskRepository _taskRepository;

    public ProjectRepositoryTests()
    {
        _sut = new ProjectRepository(_fixture.ContextFactory);
        _taskRepository = new TaskRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsTheProject()
    {
        var project = new Project { Name = "Website Redesign", ColorHex = "#6366F1" };

        await _sut.AddAsync(project);
        var fetched = await _sut.GetByIdAsync(project.Id);

        Assert.NotNull(fetched);
        Assert.Equal("Website Redesign", fetched.Name);
        Assert.Equal("#6366F1", fetched.ColorHex);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByName()
    {
        var zeta = new Project { Name = "Zeta", ColorHex = "#000000" };
        var alpha = new Project { Name = "Alpha", ColorHex = "#111111" };
        await _sut.AddAsync(zeta);
        await _sut.AddAsync(alpha);

        var results = await _sut.GetAllAsync();

        Assert.Equal(["Alpha", "Zeta"], results.Select(p => p.Name));
    }

    [Fact]
    public async Task GetAllAsync_IncludesLinkedTasks()
    {
        var project = new Project { Name = "Website Redesign", ColorHex = "#6366F1" };
        await _sut.AddAsync(project);
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Write copy", ProjectId = project.Id };
        await _taskRepository.AddAsync(task);

        var results = await _sut.GetAllAsync();

        Assert.Single(results.Single(p => p.Id == project.Id).Tasks);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var project = new Project { Name = "Website Redesign", ColorHex = "#6366F1" };
        await _sut.AddAsync(project);
        project.IsArchived = true;
        project.Name = "Website Redesign v2";

        await _sut.UpdateAsync(project);

        var fetched = await _sut.GetByIdAsync(project.Id);
        Assert.True(fetched!.IsArchived);
        Assert.Equal("Website Redesign v2", fetched.Name);
    }

    [Fact]
    public async Task DeleteAsync_UnlinksTasksRatherThanDeletingThem()
    {
        var project = new Project { Name = "Website Redesign", ColorHex = "#6366F1" };
        await _sut.AddAsync(project);
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Write copy", ProjectId = project.Id };
        await _taskRepository.AddAsync(task);

        await _sut.DeleteAsync(project.Id);

        Assert.Null(await _sut.GetByIdAsync(project.Id));
        var fetchedTask = await _taskRepository.GetByIdAsync(task.Id);
        Assert.NotNull(fetchedTask);
        Assert.Null(fetchedTask.ProjectId);
    }

    [Fact]
    public async Task DeleteAsync_OnMissingId_DoesNotThrow()
    {
        await _sut.DeleteAsync(Guid.NewGuid());
    }
}
