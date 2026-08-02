using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data.Configurations;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class TaskTemplateRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly TaskTemplateRepository _sut;

    public TaskTemplateRepositoryTests()
    {
        _sut = new TaskTemplateRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsTheTemplate()
    {
        var template = new TaskTemplate
        {
            Name = "Weekly grocery run",
            TaskTitle = "Groceries",
            ChecklistItems = ["Milk", "Eggs"],
        };

        await _sut.AddAsync(template);
        var fetched = await _sut.GetByIdAsync(template.Id);

        Assert.NotNull(fetched);
        Assert.Equal("Weekly grocery run", fetched.Name);
        Assert.Equal(["Milk", "Eggs"], fetched.ChecklistItems);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByName()
    {
        await _sut.AddAsync(new TaskTemplate { Name = "Zebra", TaskTitle = "Z" });
        await _sut.AddAsync(new TaskTemplate { Name = "Alpha", TaskTitle = "A" });

        var results = await _sut.GetAllAsync();
        var names = results.Select(t => t.Name).ToList();

        Assert.Equal(names.OrderBy(n => n, StringComparer.Ordinal), names);
        Assert.Contains("Alpha", names);
        Assert.Contains("Zebra", names);
        Assert.True(names.IndexOf("Alpha") < names.IndexOf("Zebra"));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTheSevenSeededDefaultTemplates()
    {
        var results = await _sut.GetAllAsync();

        Assert.Equal(7, results.Count);
        Assert.Contains(results, t => t.Id == TaskTemplateConfiguration.WeeklyGroceryRunId && t.Name == "Weekly grocery run");
        Assert.Contains(results, t => t.Id == TaskTemplateConfiguration.SprintPlanningPrepId && t.ChecklistItems.Contains("Review backlog"));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheTemplate()
    {
        var template = new TaskTemplate { Name = "Sprint prep", TaskTitle = "Sprint planning prep" };
        await _sut.AddAsync(template);

        await _sut.DeleteAsync(template.Id);

        Assert.Null(await _sut.GetByIdAsync(template.Id));
    }

    [Fact]
    public async Task DeleteAsync_OnMissingId_DoesNotThrow()
    {
        await _sut.DeleteAsync(Guid.NewGuid());
    }
}
