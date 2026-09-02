using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class ProjectTemplateRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : IProjectTemplateRepository
{
    public async Task<IReadOnlyList<ProjectTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.ProjectTemplates
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.ProjectTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task AddAsync(ProjectTemplate template, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.ProjectTemplates.Add(template);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProjectTemplate template, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(template).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var template = await context.ProjectTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (template is null)
        {
            return;
        }

        context.ProjectTemplates.Remove(template);
        await context.SaveChangesAsync(cancellationToken);
    }
}
