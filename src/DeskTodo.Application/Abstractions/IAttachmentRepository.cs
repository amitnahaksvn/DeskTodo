using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>
/// Persistence abstraction for <see cref="Attachment"/> rows. Only the row itself — the
/// actual file's copy/delete on disk is an Application-layer (<c>IAttachmentService</c>)
/// concern, since a repository shouldn't know about <c>AppStorageOptions.RootDirectory</c>.
/// </summary>
public interface IAttachmentRepository
{
    Task<IReadOnlyList<Attachment>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<Attachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Attachment attachment, CancellationToken cancellationToken = default);

    /// <summary>No-ops if the attachment doesn't exist.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
