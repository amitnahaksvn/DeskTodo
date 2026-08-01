using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>Checklist use cases for a single task's sub-items — add/toggle/remove, each persisted immediately.</summary>
public interface IChecklistService
{
    Task<IReadOnlyList<ChecklistItem>> GetItemsAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Appends a new item to the end of the task's checklist. A blank <paramref name="text"/> is a no-op.</summary>
    Task<ChecklistItem?> AddItemAsync(Guid taskId, string text, CancellationToken cancellationToken = default);

    /// <summary>Flips <see cref="ChecklistItem.IsChecked"/>. No-ops if the item no longer exists.</summary>
    Task ToggleItemAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task RemoveItemAsync(Guid itemId, CancellationToken cancellationToken = default);
}
