using DeskTodo.Application.Options;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using DeskTodo.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DeskTodo.Tests.Infrastructure;

/// <summary>Feature 70 (Roadmap-39-100.md) — real SQLite, no mocks, same bar as this project's other repository tests.</summary>
public sealed class DataIntegrityServiceTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), "DeskTodoTests", Guid.NewGuid().ToString("N"));
    private readonly TaskRepository _taskRepository;

    public DataIntegrityServiceTests()
    {
        Directory.CreateDirectory(_rootDirectory);
        _taskRepository = new TaskRepository(_fixture.ContextFactory);
    }

    public void Dispose()
    {
        _fixture.Dispose();
        Directory.Delete(_rootDirectory, recursive: true);
    }

    private DataIntegrityService CreateSut() =>
        new(_fixture.ContextFactory, Options.Create(new AppStorageOptions { RootDirectory = _rootDirectory }), NullLogger<DataIntegrityService>.Instance);

    [Fact]
    public async Task CheckAsync_OnAHealthyDatabase_ReportsNoIssues()
    {
        await _taskRepository.AddAsync(new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Healthy task" });
        var sut = CreateSut();

        var issues = await sut.CheckAsync();

        Assert.Empty(issues);
    }

    [Fact]
    public async Task CheckAsync_FindsATaskThatIsItsOwnParent_AsAutoRepairable()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Self-parented" };
        await _taskRepository.AddAsync(task);
        task.ParentTaskId = task.Id;
        await _taskRepository.UpdateAsync(task);
        var sut = CreateSut();

        var issues = await sut.CheckAsync();

        var issue = Assert.Single(issues);
        Assert.True(issue.IsAutoRepairable);
        Assert.Contains("own parent", issue.Description);
    }

    [Fact]
    public async Task CheckAsync_FindsANegativeEstimatedMinutesValue()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Bad estimate", EstimatedMinutes = -5 };
        await _taskRepository.AddAsync(task);
        var sut = CreateSut();

        var issues = await sut.CheckAsync();

        Assert.Single(issues);
    }

    [Fact]
    public async Task CheckAsync_FindsAnAttachmentWhoseFileIsMissingFromDisk()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Has attachment" };
        await _taskRepository.AddAsync(task);
        await using (var context = await _fixture.ContextFactory.CreateDbContextAsync())
        {
            context.Attachments.Add(new Attachment { TaskId = task.Id, FileName = "missing.pdf", StoredRelativePath = "attachments/missing.pdf" });
            await context.SaveChangesAsync();
        }

        var sut = CreateSut();
        var issues = await sut.CheckAsync();

        var issue = Assert.Single(issues);
        Assert.Contains("Missing attachment file", issue.Category);
    }

    [Fact]
    public async Task RepairAsync_ClearsASelfParentReference()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Self-parented" };
        await _taskRepository.AddAsync(task);
        task.ParentTaskId = task.Id;
        await _taskRepository.UpdateAsync(task);
        var sut = CreateSut();
        var issues = await sut.CheckAsync();

        var fixedCount = await sut.RepairAsync(issues);

        Assert.Equal(1, fixedCount);
        var reloaded = await _taskRepository.GetByIdAsync(task.Id);
        Assert.Null(reloaded!.ParentTaskId);
    }

    [Fact]
    public async Task RepairAsync_RemovesAnAttachmentRow_WhoseFileIsMissing()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Has attachment" };
        await _taskRepository.AddAsync(task);
        await using (var context = await _fixture.ContextFactory.CreateDbContextAsync())
        {
            context.Attachments.Add(new Attachment { TaskId = task.Id, FileName = "missing.pdf", StoredRelativePath = "attachments/missing.pdf" });
            await context.SaveChangesAsync();
        }

        var sut = CreateSut();
        var issues = await sut.CheckAsync();

        await sut.RepairAsync(issues);

        await using var contextAfter = await _fixture.ContextFactory.CreateDbContextAsync();
        Assert.Empty(contextAfter.Attachments);
    }

    [Fact]
    public async Task RepairAsync_WithNoAutoRepairableIssues_FixesNothing()
    {
        var sut = CreateSut();

        var fixedCount = await sut.RepairAsync([]);

        Assert.Equal(0, fixedCount);
    }
}
