using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Options;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Options;
using Moq;

namespace DeskTodo.Tests.Application;

public class AttachmentServiceTests : IDisposable
{
    private readonly string _scratchRoot = Path.Combine(Path.GetTempPath(), "DeskTodoAttachmentServiceTests_" + Guid.NewGuid());
    private readonly Mock<IAttachmentRepository> _attachmentRepository = new();
    private readonly AttachmentService _sut;

    public AttachmentServiceTests()
    {
        Directory.CreateDirectory(_scratchRoot);
        var options = Options.Create(new AppStorageOptions { RootDirectory = _scratchRoot });
        _sut = new AttachmentService(_attachmentRepository.Object, options);
    }

    public void Dispose()
    {
        if (Directory.Exists(_scratchRoot))
        {
            Directory.Delete(_scratchRoot, recursive: true);
        }
    }

    private string CreateSourceFile(string name, int sizeBytes = 128)
    {
        var path = Path.Combine(_scratchRoot, name);
        File.WriteAllBytes(path, new byte[sizeBytes]);
        return path;
    }

    [Fact]
    public async Task AddAttachmentAsync_CopiesTheFileAndRecordsTheRow()
    {
        var taskId = Guid.NewGuid();
        var sourcePath = CreateSourceFile("report.pdf");

        var attachment = await _sut.AddAttachmentAsync(taskId, sourcePath);

        Assert.NotNull(attachment);
        Assert.Equal("report.pdf", attachment.FileName);
        Assert.Equal(taskId, attachment.TaskId);
        Assert.True(File.Exists(_sut.GetAbsolutePath(attachment)));
        _attachmentRepository.Verify(r => r.AddAsync(attachment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAttachmentAsync_OnMissingSourceFile_ReturnsNull()
    {
        var attachment = await _sut.AddAttachmentAsync(Guid.NewGuid(), Path.Combine(_scratchRoot, "does-not-exist.txt"));

        Assert.Null(attachment);
        _attachmentRepository.Verify(r => r.AddAsync(It.IsAny<Attachment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAttachmentAsync_OverTheSizeCap_ReturnsNull()
    {
        var sourcePath = CreateSourceFile("huge.bin", sizeBytes: 21 * 1024 * 1024);

        var attachment = await _sut.AddAttachmentAsync(Guid.NewGuid(), sourcePath);

        Assert.Null(attachment);
        _attachmentRepository.Verify(r => r.AddAsync(It.IsAny<Attachment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveAttachmentAsync_DeletesTheRowAndTheFile()
    {
        var taskId = Guid.NewGuid();
        var sourcePath = CreateSourceFile("report.pdf");
        var attachment = await _sut.AddAttachmentAsync(taskId, sourcePath);
        Assert.NotNull(attachment);
        var absolutePath = _sut.GetAbsolutePath(attachment);
        _attachmentRepository.Setup(r => r.GetByIdAsync(attachment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(attachment);

        await _sut.RemoveAttachmentAsync(attachment.Id);

        _attachmentRepository.Verify(r => r.DeleteAsync(attachment.Id, It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(File.Exists(absolutePath));
    }

    [Fact]
    public async Task RemoveAttachmentAsync_WhenAttachmentMissing_DoesNotThrow()
    {
        _attachmentRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Attachment?)null);

        await _sut.RemoveAttachmentAsync(Guid.NewGuid());

        _attachmentRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
