using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Options;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Options;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IAttachmentService"/>
public sealed class AttachmentService(
    IAttachmentRepository attachmentRepository,
    IOptions<AppStorageOptions> storageOptions) : IAttachmentService
{
    /// <summary>20 MB — generous for a task attachment (a doc, a screenshot, a small PDF) without letting the SQLite-adjacent data directory balloon from someone attaching a video.</summary>
    private const long MaxFileSizeBytes = 20 * 1024 * 1024;

    private const string AttachmentsSubfolder = "attachments";

    public Task<IReadOnlyList<Attachment>> GetAttachmentsAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        attachmentRepository.GetByTaskIdAsync(taskId, cancellationToken);

    public async Task<Attachment?> AddAttachmentAsync(Guid taskId, string sourceFilePath, CancellationToken cancellationToken = default)
    {
        var sourceInfo = new FileInfo(sourceFilePath);
        if (!sourceInfo.Exists || sourceInfo.Length > MaxFileSizeBytes)
        {
            return null;
        }

        var attachment = new Attachment
        {
            TaskId = taskId,
            FileName = sourceInfo.Name,
            StoredRelativePath = Path.Combine(AttachmentsSubfolder, $"{Guid.NewGuid()}{sourceInfo.Extension}"),
            FileSizeBytes = sourceInfo.Length,
        };

        var absoluteDestination = GetAbsolutePath(attachment);
        Directory.CreateDirectory(Path.GetDirectoryName(absoluteDestination)!);
        File.Copy(sourceFilePath, absoluteDestination, overwrite: false);

        await attachmentRepository.AddAsync(attachment, cancellationToken);
        return attachment;
    }

    public async Task RemoveAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await attachmentRepository.GetByIdAsync(attachmentId, cancellationToken);
        if (attachment is null)
        {
            return;
        }

        await attachmentRepository.DeleteAsync(attachmentId, cancellationToken);

        var absolutePath = GetAbsolutePath(attachment);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
    }

    public string GetAbsolutePath(Attachment attachment) =>
        Path.Combine(storageOptions.Value.RootDirectory, attachment.StoredRelativePath);
}
