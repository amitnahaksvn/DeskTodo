using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>Tag use cases: the global tag list, a task's own tags, and assigning/removing tags on a task.</summary>
public interface ITagService
{
    Task<IReadOnlyList<Tag>> GetAllTagsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Tag>> GetTagsForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Gets-or-creates a tag by name (case-insensitive) and assigns it to the task. A blank <paramref name="tagName"/> is a no-op.</summary>
    Task AssignTagAsync(Guid taskId, string tagName, CancellationToken cancellationToken = default);

    Task RemoveTagAsync(Guid taskId, Guid tagId, CancellationToken cancellationToken = default);

    /// <summary>Deletes a tag outright (removing it from every task it's assigned to), not just from one task.</summary>
    Task DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default);
}
