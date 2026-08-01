using DeskTodo.Domain.Entities;
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

        Assert.Equal(["Alpha", "Zebra"], results.Select(t => t.Name));
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
