using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class FocusContextRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : IFocusContextRepository
{
    public async Task<IReadOnlyList<FocusContext>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.FocusContexts.AsNoTracking().OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FocusContext context, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.FocusContexts.Add(context);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.FocusContexts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is not null)
        {
            dbContext.FocusContexts.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
