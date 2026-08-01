using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Exceptions;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="ITaskTemplateService"/>
public sealed class TaskTemplateService(
    ITaskTemplateRepository templateRepository,
    ITaskRepository taskRepository,
    IChecklistRepository checklistRepository) : ITaskTemplateService
{
    public Task<IReadOnlyList<TaskTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default) =>
        templateRepository.GetAllAsync(cancellationToken);

    public async Task<TaskTemplate> SaveAsTemplateAsync(Guid taskId, string templateName, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(taskId, cancellationToken) ?? throw new TaskNotFoundException(taskId);

        var template = new TaskTemplate
        {
            Name = templateName.Trim(),
            TaskTitle = task.Title,
            Description = task.Description,
            Priority = task.Priority,
            CategoryId = task.CategoryId,
            EstimatedMinutes = task.EstimatedMinutes,
            Notes = task.Notes,
            ChecklistItems = task.ChecklistItems.OrderBy(c => c.Order).Select(c => c.Text).ToList(),
        };

        await templateRepository.AddAsync(template, cancellationToken);
        return template;
    }

    public async Task<TaskItem?> CreateTaskFromTemplateAsync(Guid templateId, DateOnly planDate, CancellationToken cancellationToken = default)
    {
        var template = await templateRepository.GetByIdAsync(templateId, cancellationToken);
        if (template is null)
        {
            return null;
        }

        var maxOrder = await taskRepository.GetMaxDayOrderAsync(planDate, cancellationToken);
        var task = new TaskItem
        {
            PlanDate = planDate,
            DayOrder = maxOrder + 1,
            Title = template.TaskTitle,
            Description = template.Description,
            Priority = template.Priority,
            CategoryId = template.CategoryId,
            EstimatedMinutes = template.EstimatedMinutes,
            Notes = template.Notes,
        };
        await taskRepository.AddAsync(task, cancellationToken);

        if (template.ChecklistItems.Count > 0)
        {
            var checklistItems = template.ChecklistItems.Select((text, index) => new ChecklistItem
            {
                TaskId = task.Id,
                Text = text,
                Order = index,
            });
            await checklistRepository.AddRangeAsync(checklistItems, cancellationToken);
        }

        return task;
    }

    public Task DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        templateRepository.DeleteAsync(templateId, cancellationToken);
}
