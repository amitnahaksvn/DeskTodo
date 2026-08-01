using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class TagRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : ITagRepository
{
    public async Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Tags
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Tag>> GetForTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Tasks
            .AsNoTracking()
            .Where(t => t.Id == taskId)
            .SelectMany(t => t.Tags)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tag> GetOrCreateByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await context.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower(), cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var tag = new Tag { Name = name };
        context.Tags.Add(tag);
        await context.SaveChangesAsync(cancellationToken);
        return tag;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var tag = await context.Tags.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tag is null)
        {
            return;
        }

        context.Tags.Remove(tag);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignToTaskAsync(Guid taskId, Guid tagId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var task = await context.Tasks.Include(t => t.Tags).FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        var tag = await context.Tags.FirstOrDefaultAsync(t => t.Id == tagId, cancellationToken);
        if (task is null || tag is null || task.Tags.Any(t => t.Id == tagId))
        {
            return;
        }

        task.Tags.Add(tag);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFromTaskAsync(Guid taskId, Guid tagId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var task = await context.Tasks.Include(t => t.Tags).FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        var tag = task?.Tags.FirstOrDefault(t => t.Id == tagId);
        if (tag is null)
        {
            return;
        }

        task!.Tags.Remove(tag);
        await context.SaveChangesAsync(cancellationToken);
    }
}
