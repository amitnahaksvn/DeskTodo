using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class RecurringProjectScheduleRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : IRecurringProjectScheduleRepository
{
    public async Task<IReadOnlyList<RecurringProjectSchedule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.RecurringProjectSchedules
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecurringProjectSchedule>> GetDueAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.RecurringProjectSchedules
            .AsNoTracking()
            .Where(s => s.IsActive && s.NextOccurrenceDate <= asOf)
            .OrderBy(s => s.NextOccurrenceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<RecurringProjectSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.RecurringProjectSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddAsync(RecurringProjectSchedule schedule, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.RecurringProjectSchedules.Add(schedule);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RecurringProjectSchedule schedule, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(schedule).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var schedule = await context.RecurringProjectSchedules.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (schedule is null)
        {
            return;
        }

        context.RecurringProjectSchedules.Remove(schedule);
        await context.SaveChangesAsync(cancellationToken);
    }
}
