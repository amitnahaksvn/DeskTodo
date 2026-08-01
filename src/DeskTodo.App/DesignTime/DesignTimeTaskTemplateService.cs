using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;

namespace DeskTodo.App.DesignTime;

/// <summary>
/// No-op <see cref="ITaskTemplateService"/> used only as a fallback when
/// <see cref="App.Services"/> is null — i.e. at XAML-designer time, which
/// never runs through <c>Program.Main</c>'s DI container.
/// </summary>
internal sealed class DesignTimeTaskTemplateService : ITaskTemplateService
{
    public Task<IReadOnlyList<TaskTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaskTemplate>>([]);

    public Task<TaskTemplate> SaveAsTemplateAsync(Guid taskId, string templateName, CancellationToken cancellationToken = default) =>
        Task.FromResult(new TaskTemplate { Name = templateName, TaskTitle = string.Empty });

    public Task<TaskItem?> CreateTaskFromTemplateAsync(Guid templateId, DateOnly planDate, CancellationToken cancellationToken = default) =>
        Task.FromResult<TaskItem?>(null);

    public Task DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
