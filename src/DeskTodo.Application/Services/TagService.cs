using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="ITagService"/>
public sealed class TagService(ITagRepository tagRepository) : ITagService
{
    public Task<IReadOnlyList<Tag>> GetAllTagsAsync(CancellationToken cancellationToken = default) =>
        tagRepository.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<Tag>> GetTagsForTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        tagRepository.GetForTaskAsync(taskId, cancellationToken);

    public async Task AssignTagAsync(Guid taskId, string tagName, CancellationToken cancellationToken = default)
    {
        var trimmed = tagName.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return;
        }

        var tag = await tagRepository.GetOrCreateByNameAsync(trimmed, cancellationToken);
        await tagRepository.AssignToTaskAsync(taskId, tag.Id, cancellationToken);
    }

    public Task RemoveTagAsync(Guid taskId, Guid tagId, CancellationToken cancellationToken = default) =>
        tagRepository.RemoveFromTaskAsync(taskId, tagId, cancellationToken);

    public Task DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default) =>
        tagRepository.DeleteAsync(tagId, cancellationToken);
}
