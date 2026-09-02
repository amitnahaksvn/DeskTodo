using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class InboxRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly InboxRepository _sut;

    public InboxRepositoryTests()
    {
        _sut = new InboxRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsTheItem()
    {
        var item = new InboxItem { Content = "Buy milk" };

        await _sut.AddAsync(item);
        var result = await _sut.GetByIdAsync(item.Id);

        Assert.NotNull(result);
        Assert.Equal("Buy milk", result!.Content);
        Assert.Equal(InboxItemStatus.Unprocessed, result.Status);
    }

    [Fact]
    public async Task GetUnprocessedAsync_ExcludesConvertedAndArchivedItems_OrderedOldestFirst()
    {
        var first = new InboxItem { Content = "First", CreatedAt = new DateTime(2026, 8, 1) };
        var second = new InboxItem { Content = "Second", CreatedAt = new DateTime(2026, 8, 2) };
        var archived = new InboxItem { Content = "Archived", CreatedAt = new DateTime(2026, 8, 3) };
        await _sut.AddAsync(first);
        await _sut.AddAsync(second);
        await _sut.AddAsync(archived);

        archived.Archive();
        await _sut.UpdateAsync(archived);

        var results = await _sut.GetUnprocessedAsync();

        Assert.Equal([first.Id, second.Id], results.Select(i => i.Id));
    }

    [Fact]
    public async Task UpdateAsync_PersistsStatusChanges()
    {
        var item = new InboxItem { Content = "Task" };
        await _sut.AddAsync(item);

        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Converted task" };
        await new TaskRepository(_fixture.ContextFactory).AddAsync(task);

        item.MarkConverted(task.Id);
        await _sut.UpdateAsync(item);

        var reloaded = await _sut.GetByIdAsync(item.Id);
        Assert.Equal(InboxItemStatus.Converted, reloaded!.Status);
        Assert.Equal(task.Id, reloaded.ConvertedTaskId);
        Assert.NotNull(reloaded.ProcessedAt);
    }

    [Fact]
    public async Task RemoveAsync_DeletesTheItem()
    {
        var item = new InboxItem { Content = "Gone" };
        await _sut.AddAsync(item);

        await _sut.RemoveAsync(item.Id);

        Assert.Null(await _sut.GetByIdAsync(item.Id));
    }

    [Fact]
    public async Task RemoveAsync_WithUnknownId_DoesNotThrow()
    {
        await _sut.RemoveAsync(Guid.NewGuid());
    }
}
