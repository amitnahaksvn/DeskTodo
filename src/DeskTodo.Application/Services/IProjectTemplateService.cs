using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>Roadmap-39-100.md Feature 86 — reusable "standard project" shapes that instantiate into a real <see cref="Project"/> plus its tasks and milestones on a chosen start date.</summary>
public interface IProjectTemplateService
{
    Task<IReadOnlyList<ProjectTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    Task<ProjectTemplate?> GetTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task<ProjectTemplate> CreateTemplateAsync(
        string name,
        string? description,
        IReadOnlyList<ProjectTemplateTaskItem> taskItems,
        IReadOnlyList<ProjectTemplateMilestoneItem> milestoneItems,
        CancellationToken cancellationToken = default);

    Task UpdateTemplateAsync(
        Guid templateId,
        string name,
        string? description,
        IReadOnlyList<ProjectTemplateTaskItem> taskItems,
        IReadOnlyList<ProjectTemplateMilestoneItem> milestoneItems,
        CancellationToken cancellationToken = default);

    Task DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>Materializes <paramref name="templateId"/> into a real <see cref="Project"/>: a project row, one <see cref="Milestone"/> per <see cref="ProjectTemplateMilestoneItem"/>, and one <see cref="TaskItem"/> per <see cref="ProjectTemplateTaskItem"/>, all with dates computed relative to <paramref name="startDate"/> and linked to the new project.</summary>
    Task<Project> CreateProjectFromTemplateAsync(
        Guid templateId,
        string projectName,
        string colorHex,
        DateOnly startDate,
        CancellationToken cancellationToken = default);
}
