using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

public interface IBulkEditRuleRepository
{
    Task<IReadOnlyList<BulkEditRule>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<BulkEditRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(BulkEditRule rule, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
