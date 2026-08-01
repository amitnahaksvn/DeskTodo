using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class AttachmentRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly AttachmentRepository _sut;
    private readonly TaskRepository _taskRepository;

    public AttachmentRepositoryTests()
    {
        _sut = new AttachmentRepository(_fixture.ContextFactory);
        _taskRepository = new TaskRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    private async Task<Guid> CreateTaskAsync()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Design review" };
        await _taskRepository.AddAsync(task);
        return task.Id;
    }

    [Fact]
    public async Task AddAsync_ThenGetByTaskIdAsync_ReturnsTheAttachment()
    {
        var taskId = await CreateTaskAsync();
        var attachment = new Attachment { TaskId = taskId, FileName = "notes.pdf", StoredRelativePath = "attachments/a.pdf", FileSizeBytes = 1024 };

        await _sut.AddAsync(attachment);
        var results = await _sut.GetByTaskIdAsync(taskId);

        Assert.Single(results);
        Assert.Equal("notes.pdf", results[0].FileName);
    }

    [Fact]
    public async Task GetByIdAsync_OnMissingId_ReturnsNull()
    {
        Assert.Null(await _sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheRow()
    {
        var taskId = await CreateTaskAsync();
        var attachment = new Attachment { TaskId = taskId, FileName = "notes.pdf", StoredRelativePath = "attachments/a.pdf" };
        await _sut.AddAsync(attachment);

        await _sut.DeleteAsync(attachment.Id);

        Assert.Null(await _sut.GetByIdAsync(attachment.Id));
    }

    [Fact]
    public async Task DeleteAsync_OnMissingId_DoesNotThrow()
    {
        await _sut.DeleteAsync(Guid.NewGuid());
    }
}
