using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;

namespace DeskTodo.App.DesignTime;

/// <summary>
/// No-op <see cref="ITagService"/> used only as a fallback when
/// <see cref="App.Services"/> is null — i.e. at XAML-designer time, which
/// never runs through <c>Program.Main</c>'s DI container.
/// </summary>
internal sealed class DesignTimeTagService : ITagService
{
    public Task<IReadOnlyList<Tag>> GetAllTagsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Tag>>([]);

    public Task<IReadOnlyList<Tag>> GetTagsForTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Tag>>([]);

    public Task AssignTagAsync(Guid taskId, string tagName, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RemoveTagAsync(Guid taskId, Guid tagId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
