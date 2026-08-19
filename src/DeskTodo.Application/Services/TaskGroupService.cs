using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Exceptions;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="ITaskGroupService"/>
public sealed class TaskGroupService(
    ITaskGroupRepository groupRepository,
    ITaskTemplateService templateService) : ITaskGroupService
{
    public Task<IReadOnlyList<TaskGroup>> GetGroupsAsync(CancellationToken cancellationToken = default) =>
        groupRepository.GetAllAsync(cancellationToken);

    public async Task<TaskGroup> CreateGroupAsync(string name, IReadOnlyList<Guid> templateIds, CancellationToken cancellationToken = default)
    {
        var group = new TaskGroup
        {
            Name = name.Trim(),
            TemplateIds = templateIds.ToList(),
        };

        await groupRepository.AddAsync(group, cancellationToken);
        return group;
    }

    public async Task UpdateGroupAsync(Guid groupId, string name, IReadOnlyList<Guid> templateIds, CancellationToken cancellationToken = default)
    {
        var group = await groupRepository.GetByIdAsync(groupId, cancellationToken) ?? throw new TaskGroupNotFoundException(groupId);

        group.Name = name.Trim();
        group.TemplateIds = templateIds.ToList();

        await groupRepository.UpdateAsync(group, cancellationToken);
    }

    public Task DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        groupRepository.DeleteAsync(groupId, cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> CreateTasksFromGroupAsync(Guid groupId, DateOnly planDate, CancellationToken cancellationToken = default)
    {
        var group = await groupRepository.GetByIdAsync(groupId, cancellationToken) ?? throw new TaskGroupNotFoundException(groupId);

        var createdTasks = new List<TaskItem>();
        foreach (var templateId in group.TemplateIds)
        {
            var task = await templateService.CreateTaskFromTemplateAsync(templateId, planDate, cancellationToken);
            if (task is not null)
            {
                createdTasks.Add(task);
            }
        }

        return createdTasks;
    }
}
