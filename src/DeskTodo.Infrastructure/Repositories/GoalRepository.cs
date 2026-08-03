using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class GoalRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : IGoalRepository
{
    public async Task<IReadOnlyList<Goal>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Goals
            .AsNoTracking()
            .Include(g => g.Completions)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Goal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Goals
            .AsNoTracking()
            .Include(g => g.Completions)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task AddAsync(Goal goal, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Goals.Add(goal);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Goal goal, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(goal).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var goal = await context.Goals.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (goal is null)
        {
            return;
        }

        context.Goals.Remove(goal);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddCompletionAsync(Guid goalId, DateOnly date, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var alreadyLogged = await context.GoalCompletions
            .AnyAsync(c => c.GoalId == goalId && c.CompletedDate == date, cancellationToken);
        if (alreadyLogged)
        {
            return;
        }

        context.GoalCompletions.Add(new GoalCompletion { GoalId = goalId, CompletedDate = date });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveCompletionAsync(Guid goalId, DateOnly date, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var completion = await context.GoalCompletions
            .FirstOrDefaultAsync(c => c.GoalId == goalId && c.CompletedDate == date, cancellationToken);
        if (completion is null)
        {
            return;
        }

        context.GoalCompletions.Remove(completion);
        await context.SaveChangesAsync(cancellationToken);
    }
}
