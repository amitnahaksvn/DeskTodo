using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>Persistence abstraction for <see cref="TaskVersion"/> — Feature 44's version snapshots.</summary>
public interface ITaskVersionRepository
{
    Task AddAsync(TaskVersion version, CancellationToken cancellationToken = default);

    /// <summary>A task's version snapshots, most recent first.</summary>
    Task<IReadOnlyList<TaskVersion>> GetForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<TaskVersion?> GetByIdAsync(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>Highest <see cref="TaskVersion.VersionNumber"/> already captured for this task, or 0 if none.</summary>
    Task<int> GetMaxVersionNumberAsync(Guid taskId, CancellationToken cancellationToken = default);
}
