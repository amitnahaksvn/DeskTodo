using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>Persistence abstraction for <see cref="Decision"/> — Feature 57's Decision Log.</summary>
public interface IDecisionRepository
{
    /// <summary>Most recent first.</summary>
    Task<IReadOnlyList<Decision>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Decision?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Decision decision, CancellationToken cancellationToken = default);

    Task UpdateAsync(Decision decision, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
