using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Exceptions;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IProjectService"/>
public sealed class ProjectService(IProjectRepository projectRepository) : IProjectService
{
    public Task<IReadOnlyList<Project>> GetProjectsAsync(CancellationToken cancellationToken = default) =>
        projectRepository.GetAllAsync(cancellationToken);

    public async Task<Project> CreateProjectAsync(string name, string? description, string colorHex, CancellationToken cancellationToken = default)
    {
        var project = new Project
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ColorHex = colorHex,
        };
        await projectRepository.AddAsync(project, cancellationToken);
        return project;
    }

    public async Task UpdateProjectAsync(Guid projectId, string name, string? description, string colorHex, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken) ?? throw new ProjectNotFoundException(projectId);
        project.Name = name.Trim();
        project.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        project.ColorHex = colorHex;
        await projectRepository.UpdateAsync(project, cancellationToken);
    }

    public async Task SetArchivedAsync(Guid projectId, bool isArchived, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken) ?? throw new ProjectNotFoundException(projectId);
        project.IsArchived = isArchived;
        await projectRepository.UpdateAsync(project, cancellationToken);
    }

    public Task DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        projectRepository.DeleteAsync(projectId, cancellationToken);
}
