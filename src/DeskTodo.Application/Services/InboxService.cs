using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IInboxService"/>
public sealed class InboxService(IInboxRepository inboxRepository, ITaskService taskService) : IInboxService
{
    public async Task<InboxItem> CaptureAsync(string content, CancellationToken cancellationToken = default)
    {
        var item = new InboxItem { Content = content };
        await inboxRepository.AddAsync(item, cancellationToken);
        return item;
    }

    public Task<IReadOnlyList<InboxItem>> GetUnprocessedAsync(CancellationToken cancellationToken = default) =>
        inboxRepository.GetUnprocessedAsync(cancellationToken);

    public async Task<TaskItem> ConvertToTaskAsync(Guid inboxItemId, DateOnly planDate, CancellationToken cancellationToken = default)
    {
        var item = await inboxRepository.GetByIdAsync(inboxItemId, cancellationToken)
            ?? throw new InvalidOperationException($"Inbox item '{inboxItemId}' was not found.");

        var task = await taskService.CreateTaskAsync(planDate, item.Content, cancellationToken: cancellationToken);
        item.MarkConverted(task.Id);
        await inboxRepository.UpdateAsync(item, cancellationToken);
        return task;
    }

    public async Task ArchiveAsync(Guid inboxItemId, CancellationToken cancellationToken = default)
    {
        var item = await inboxRepository.GetByIdAsync(inboxItemId, cancellationToken)
            ?? throw new InvalidOperationException($"Inbox item '{inboxItemId}' was not found.");

        item.Archive();
        await inboxRepository.UpdateAsync(item, cancellationToken);
    }

    public Task DeleteAsync(Guid inboxItemId, CancellationToken cancellationToken = default) =>
        inboxRepository.RemoveAsync(inboxItemId, cancellationToken);
}
