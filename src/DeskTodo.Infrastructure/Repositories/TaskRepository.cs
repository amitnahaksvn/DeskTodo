using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class TaskRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : ITaskRepository
{
    public async Task<IReadOnlyList<TaskItem>> GetByDateAsync(DateOnly planDate, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Tasks
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.PlanDate == planDate && !t.IsDeleted && !t.IsArchived)
            .OrderBy(t => t.DayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetArchivedAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Tasks
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.IsArchived && !t.IsDeleted)
            .OrderByDescending(t => t.ModifiedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetPinnedAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Tasks
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.IsPinned && !t.IsDeleted && !t.IsArchived)
            .OrderByDescending(t => t.ModifiedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetMaxDayOrderAsync(DateOnly planDate, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var maxOrder = await context.Tasks
            .Where(t => t.PlanDate == planDate)
            .Select(t => (int?)t.DayOrder)
            .MaxAsync(cancellationToken);

        return maxOrder ?? -1;
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Tasks.Add(task);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        // Entry(task).State = Modified (rather than DbSet.Update(task)) marks only
        // the task itself as Modified. Callers may pass a task whose Category
        // navigation was populated by an earlier Include() (e.g. from
        // GetByDateAsync) — CategoryId (the FK scalar) is what should be
        // persisted here, so the reachable Category is attached as Unchanged
        // (no UPDATE emitted for it) rather than being pulled into the write.
        context.Entry(task).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReorderAsync(DateOnly planDate, IReadOnlyList<Guid> orderedTaskIds, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var tasksForDay = await context.Tasks
            .Where(t => t.PlanDate == planDate)
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        for (var index = 0; index < orderedTaskIds.Count; index++)
        {
            if (tasksForDay.TryGetValue(orderedTaskIds[index], out var task) && task.DayOrder != index)
            {
                task.DayOrder = index;
                task.Touch();
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
