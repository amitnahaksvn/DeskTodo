using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>
/// Template use cases: saving an existing task's shape as a reusable
/// template, and seeding a new task from one.
/// </summary>
public interface ITaskTemplateService
{
    Task<IReadOnlyList<TaskTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>Copies <paramref name="taskId"/>'s title/description/priority/category/estimate/notes/checklist into a new named <see cref="TaskTemplate"/>. Throws if the task doesn't exist.</summary>
    Task<TaskTemplate> SaveAsTemplateAsync(Guid taskId, string templateName, CancellationToken cancellationToken = default);

    /// <summary>Creates a new task on <paramref name="planDate"/> seeded from the template (including its checklist lines). Returns null if the template no longer exists.</summary>
    Task<TaskItem?> CreateTaskFromTemplateAsync(Guid templateId, DateOnly planDate, CancellationToken cancellationToken = default);

    Task DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);
}
