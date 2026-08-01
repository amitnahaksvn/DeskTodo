using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IChecklistService"/>
public sealed class ChecklistService(IChecklistRepository checklistRepository) : IChecklistService
{
    public Task<IReadOnlyList<ChecklistItem>> GetItemsAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        checklistRepository.GetByTaskIdAsync(taskId, cancellationToken);

    public async Task<ChecklistItem?> AddItemAsync(Guid taskId, string text, CancellationToken cancellationToken = default)
    {
        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        var maxOrder = await checklistRepository.GetMaxOrderAsync(taskId, cancellationToken);
        var item = new ChecklistItem { TaskId = taskId, Text = trimmed, Order = maxOrder + 1 };
        await checklistRepository.AddAsync(item, cancellationToken);
        return item;
    }

    public async Task ToggleItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await checklistRepository.GetByIdAsync(itemId, cancellationToken);
        if (item is null)
        {
            return;
        }

        item.IsChecked = !item.IsChecked;
        await checklistRepository.UpdateAsync(item, cancellationToken);
    }

    public Task RemoveItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        checklistRepository.DeleteAsync(itemId, cancellationToken);
}
