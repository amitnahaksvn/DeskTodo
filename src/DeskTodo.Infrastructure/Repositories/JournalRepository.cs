using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class JournalRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : IJournalRepository
{
    public async Task<IReadOnlyList<JournalEntry>> GetForDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.JournalEntries.AsNoTracking().Where(j => j.Date == date).OrderByDescending(j => j.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JournalEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.JournalEntries.AsNoTracking().OrderByDescending(j => j.Date).ToListAsync(cancellationToken);
    }

    public async Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.JournalEntries.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task AddAsync(JournalEntry entry, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.JournalEntries.Add(entry);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(JournalEntry entry, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.JournalEntries.Update(entry);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entry = await context.JournalEntries.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
        if (entry is not null)
        {
            context.JournalEntries.Remove(entry);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
