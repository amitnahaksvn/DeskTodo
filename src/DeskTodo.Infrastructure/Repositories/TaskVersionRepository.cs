using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class TaskVersionRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : ITaskVersionRepository
{
    public async Task AddAsync(TaskVersion version, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.TaskVersions.Add(version);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskVersion>> GetForTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.TaskVersions
            .AsNoTracking()
            .Where(v => v.TaskId == taskId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskVersion?> GetByIdAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.TaskVersions.AsNoTracking().FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken);
    }

    public async Task<int> GetMaxVersionNumberAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var versions = await context.TaskVersions.AsNoTracking().Where(v => v.TaskId == taskId).ToListAsync(cancellationToken);
        return versions.Count == 0 ? 0 : versions.Max(v => v.VersionNumber);
    }
}
