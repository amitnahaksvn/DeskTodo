using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>Persistence abstraction for <see cref="FocusContext"/> — Feature 63's Focus Contexts.</summary>
public interface IFocusContextRepository
{
    Task<IReadOnlyList<FocusContext>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(FocusContext context, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
