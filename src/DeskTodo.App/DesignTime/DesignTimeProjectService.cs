using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;

namespace DeskTodo.App.DesignTime;

/// <summary>
/// No-op <see cref="IProjectService"/> used only as a fallback when
/// <see cref="App.Services"/> is null — i.e. at XAML-designer time, which
/// never runs through <c>Program.Main</c>'s DI container.
/// </summary>
internal sealed class DesignTimeProjectService : IProjectService
{
    public Task<IReadOnlyList<Project>> GetProjectsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Project>>([]);

    public Task<Project> CreateProjectAsync(string name, string? description, string colorHex, CancellationToken cancellationToken = default) =>
        Task.FromResult(new Project { Name = name, ColorHex = colorHex });

    public Task UpdateProjectAsync(Guid projectId, string name, string? description, string colorHex, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SetArchivedAsync(Guid projectId, bool isArchived, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
