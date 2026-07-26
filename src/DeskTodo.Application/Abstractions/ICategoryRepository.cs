using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>
/// Persistence abstraction for <see cref="Category"/>. Each method is a
/// self-contained unit of work — see the remarks on <see cref="ITaskRepository"/>.
/// </summary>
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>No-ops if the category is built-in (see <see cref="Category.IsBuiltIn"/>) or doesn't exist.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
