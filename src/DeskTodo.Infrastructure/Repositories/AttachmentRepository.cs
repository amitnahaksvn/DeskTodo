using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Repositories;

public sealed class AttachmentRepository(IDbContextFactory<DeskTodoDbContext> contextFactory) : IAttachmentRepository
{
    public async Task<IReadOnlyList<Attachment>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Attachments
            .AsNoTracking()
            .Where(a => a.TaskId == taskId)
            .OrderBy(a => a.AddedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Attachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Attachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task AddAsync(Attachment attachment, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Attachments.Add(attachment);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var attachment = await context.Attachments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (attachment is null)
        {
            return;
        }

        context.Attachments.Remove(attachment);
        await context.SaveChangesAsync(cancellationToken);
    }
}
