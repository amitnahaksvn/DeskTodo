using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

public interface IMigrationRunRepository
{
    Task<IReadOnlyList<MigrationRun>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(MigrationRun run, CancellationToken cancellationToken = default);
}
