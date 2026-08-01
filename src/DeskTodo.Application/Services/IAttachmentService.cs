using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>Attachment use cases: list a task's attachments, attach a file (copying it into app storage), remove one.</summary>
public interface IAttachmentService
{
    Task<IReadOnlyList<Attachment>> GetAttachmentsAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies the file at <paramref name="sourceFilePath"/> into app storage and records an
    /// <see cref="Attachment"/> row. Returns null (rather than throwing) if the source file
    /// doesn't exist or exceeds the size cap — both are "the user picked something that
    /// can't be attached," not a genuine error.
    /// </summary>
    Task<Attachment?> AddAttachmentAsync(Guid taskId, string sourceFilePath, CancellationToken cancellationToken = default);

    /// <summary>Deletes the DB row first, then best-effort deletes the copied file — an orphaned file on disk is a smaller problem than a DB row pointing at nothing.</summary>
    Task RemoveAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default);

    /// <summary>Resolves <see cref="Attachment.StoredRelativePath"/> to a full path on disk, for opening the file.</summary>
    string GetAbsolutePath(Attachment attachment);
}
