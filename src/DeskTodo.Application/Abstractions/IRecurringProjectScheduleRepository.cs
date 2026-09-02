using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

public interface IRecurringProjectScheduleRepository
{
    Task<IReadOnlyList<RecurringProjectSchedule>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecurringProjectSchedule>> GetDueAsync(DateOnly asOf, CancellationToken cancellationToken = default);

    Task<RecurringProjectSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(RecurringProjectSchedule schedule, CancellationToken cancellationToken = default);

    Task UpdateAsync(RecurringProjectSchedule schedule, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
