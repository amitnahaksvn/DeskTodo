using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>Persistence abstraction for <see cref="Distraction"/> — Feature 64's Distraction Log.</summary>
public interface IDistractionRepository
{
    /// <summary>Most recent first.</summary>
    Task<IReadOnlyList<Distraction>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Distraction distraction, CancellationToken cancellationToken = default);
}
