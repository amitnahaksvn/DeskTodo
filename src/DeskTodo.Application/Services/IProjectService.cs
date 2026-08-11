using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>Project use cases: create/rename/recolor/archive/delete a project. Linking a task to a project is a plain <see cref="TaskItem.ProjectId"/> property set through <see cref="ITaskService"/>'s normal update path, the same way linking a task to a Category or Milestone works — no dedicated link/unlink method here.</summary>
public interface IProjectService
{
    Task<IReadOnlyList<Project>> GetProjectsAsync(CancellationToken cancellationToken = default);

    Task<Project> CreateProjectAsync(string name, string? description, string colorHex, CancellationToken cancellationToken = default);

    Task UpdateProjectAsync(Guid projectId, string name, string? description, string colorHex, CancellationToken cancellationToken = default);

    Task SetArchivedAsync(Guid projectId, bool isArchived, CancellationToken cancellationToken = default);

    /// <summary>Linked tasks are unlinked, not deleted — see <see cref="Abstractions.IProjectRepository.DeleteAsync"/>.</summary>
    Task DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}
