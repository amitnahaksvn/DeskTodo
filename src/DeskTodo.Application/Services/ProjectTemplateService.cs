using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Exceptions;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IProjectTemplateService"/>
public sealed class ProjectTemplateService(
    IProjectTemplateRepository templateRepository,
    IProjectRepository projectRepository,
    IMilestoneRepository milestoneRepository,
    ITaskRepository taskRepository) : IProjectTemplateService
{
    public Task<IReadOnlyList<ProjectTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default) =>
        templateRepository.GetAllAsync(cancellationToken);

    public Task<ProjectTemplate?> GetTemplateAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        templateRepository.GetByIdAsync(templateId, cancellationToken);

    public async Task<ProjectTemplate> CreateTemplateAsync(
        string name,
        string? description,
        IReadOnlyList<ProjectTemplateTaskItem> taskItems,
        IReadOnlyList<ProjectTemplateMilestoneItem> milestoneItems,
        CancellationToken cancellationToken = default)
    {
        var template = new ProjectTemplate
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            TaskItems = taskItems.ToList(),
            MilestoneItems = milestoneItems.ToList(),
        };

        await templateRepository.AddAsync(template, cancellationToken);
        return template;
    }

    public async Task UpdateTemplateAsync(
        Guid templateId,
        string name,
        string? description,
        IReadOnlyList<ProjectTemplateTaskItem> taskItems,
        IReadOnlyList<ProjectTemplateMilestoneItem> milestoneItems,
        CancellationToken cancellationToken = default)
    {
        var template = await templateRepository.GetByIdAsync(templateId, cancellationToken)
            ?? throw new ProjectTemplateNotFoundException(templateId);

        template.Name = name.Trim();
        template.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        template.TaskItems = taskItems.ToList();
        template.MilestoneItems = milestoneItems.ToList();

        await templateRepository.UpdateAsync(template, cancellationToken);
    }

    public Task DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        templateRepository.DeleteAsync(templateId, cancellationToken);

    public async Task<Project> CreateProjectFromTemplateAsync(
        Guid templateId,
        string projectName,
        string colorHex,
        DateOnly startDate,
        CancellationToken cancellationToken = default)
    {
        var template = await templateRepository.GetByIdAsync(templateId, cancellationToken)
            ?? throw new ProjectTemplateNotFoundException(templateId);

        var project = new Project
        {
            Name = projectName.Trim(),
            ColorHex = colorHex,
        };
        await projectRepository.AddAsync(project, cancellationToken);

        var order = 0;
        foreach (var item in template.MilestoneItems)
        {
            var milestone = new Milestone
            {
                Title = item.Title,
                TargetDate = startDate.AddDays(item.DayOffset - 1),
                ProjectId = project.Id,
                Order = order++,
            };
            await milestoneRepository.AddAsync(milestone, cancellationToken);
        }

        foreach (var item in template.TaskItems)
        {
            var planDate = startDate.AddDays(item.DayOffsetStart - 1);
            var dueDate = startDate.AddDays(item.DayOffsetStart - 1 + Math.Max(0, item.DurationDays - 1));
            var dayOrder = await taskRepository.GetMaxDayOrderAsync(planDate, cancellationToken) + 1;

            var task = new TaskItem
            {
                PlanDate = planDate,
                DayOrder = dayOrder,
                Title = item.Title,
                Priority = item.Priority,
                ProjectId = project.Id,
                DueDate = dueDate.ToDateTime(TimeOnly.MinValue),
            };
            await taskRepository.AddAsync(task, cancellationToken);
        }

        return project;
    }
}
