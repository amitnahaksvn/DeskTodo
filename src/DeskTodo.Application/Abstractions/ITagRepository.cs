using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>
/// Persistence abstraction for <see cref="Tag"/> and its many-to-many
/// assignment onto tasks. Each method is a self-contained unit of work — see
/// the remarks on <see cref="ITaskRepository"/>.
/// </summary>
public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Tag>> GetForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Case-insensitive lookup; creates a new <see cref="Tag"/> if none matches.</summary>
    Task<Tag> GetOrCreateByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>No-ops if the tag doesn't exist (the join rows are removed via cascade delete).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>No-ops if either id doesn't exist, or the tag is already assigned.</summary>
    Task AssignToTaskAsync(Guid taskId, Guid tagId, CancellationToken cancellationToken = default);

    /// <summary>No-ops if either id doesn't exist, or the tag isn't currently assigned.</summary>
    Task RemoveFromTaskAsync(Guid taskId, Guid tagId, CancellationToken cancellationToken = default);
}
