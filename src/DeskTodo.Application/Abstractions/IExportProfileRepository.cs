using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

public interface IExportProfileRepository
{
    Task<IReadOnlyList<ExportProfile>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ExportProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(ExportProfile profile, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
