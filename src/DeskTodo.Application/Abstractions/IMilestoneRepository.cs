using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>Persistence abstraction for <see cref="Milestone"/>. Each method is a self-contained unit of work — see the remarks on <see cref="ITaskRepository"/>.</summary>
public interface IMilestoneRepository
{
    /// <summary>Includes each milestone's linked <see cref="Milestone.Tasks"/> — needed for the "X/Y tasks done" progress the Milestones tab shows.</summary>
    Task<IReadOnlyList<Milestone>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Milestone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Milestone milestone, CancellationToken cancellationToken = default);

    Task UpdateAsync(Milestone milestone, CancellationToken cancellationToken = default);

    /// <summary>No-ops if the milestone doesn't exist. Linked tasks are unlinked (MilestoneId set null), not deleted — see MilestoneConfiguration's SetNull.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
