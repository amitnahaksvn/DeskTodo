using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IBulkEditRuleService"/>
public sealed class BulkEditRuleService(
    IBulkEditRuleRepository ruleRepository,
    ITaskService taskService,
    ITagService tagService,
    ILogger<BulkEditRuleService> logger) : IBulkEditRuleService
{
    public Task<IReadOnlyList<BulkEditRule>> GetRulesAsync(CancellationToken cancellationToken = default) =>
        ruleRepository.GetAllAsync(cancellationToken);

    public async Task<BulkEditRule> CreateRuleAsync(
        string name,
        IReadOnlyList<BulkEditCondition> conditions,
        IReadOnlyList<BulkEditAction> actions,
        CancellationToken cancellationToken = default)
    {
        var rule = new BulkEditRule
        {
            Name = name.Trim(),
            Conditions = conditions.ToList(),
            Actions = actions.ToList(),
        };

        await ruleRepository.AddAsync(rule, cancellationToken);
        return rule;
    }

    public Task DeleteRuleAsync(Guid ruleId, CancellationToken cancellationToken = default) =>
        ruleRepository.DeleteAsync(ruleId, cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> PreviewAsync(IReadOnlyList<BulkEditCondition> conditions, CancellationToken cancellationToken = default)
    {
        var allTasks = await taskService.GetAllTasksAsync(cancellationToken);
        return allTasks.Where(task => BulkEditRuleMatcher.Matches(task, conditions)).ToList();
    }

    public async Task<int> ApplyAsync(IReadOnlyList<BulkEditCondition> conditions, IReadOnlyList<BulkEditAction> actions, CancellationToken cancellationToken = default)
    {
        var matches = await PreviewAsync(conditions, cancellationToken);

        foreach (var task in matches)
        {
            foreach (var action in actions)
            {
                try
                {
                    await ApplyActionAsync(task, action, cancellationToken);
                }
                catch (Exception ex)
                {
                    // One task failing an action (e.g. MarkCompleted on a blocked task) shouldn't
                    // abort the whole batch — best-effort, same reasoning TaskGroupService uses
                    // for a group member whose template was deleted.
                    logger.LogWarning(ex, "Bulk edit action {ActionType} failed for task {TaskId}", action.Type, task.Id);
                }
            }
        }

        return matches.Count;
    }

    public async Task<int> ApplyRuleAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await ruleRepository.GetByIdAsync(ruleId, cancellationToken) ?? throw new BulkEditRuleNotFoundException(ruleId);
        return await ApplyAsync(rule.Conditions, rule.Actions, cancellationToken);
    }

    private async Task ApplyActionAsync(TaskItem task, BulkEditAction action, CancellationToken cancellationToken)
    {
        switch (action.Type)
        {
            case BulkEditActionType.SetPriority:
                if (Enum.TryParse<TaskPriority>(action.Value, ignoreCase: true, out var priority))
                {
                    task.Priority = priority;
                    await taskService.UpdateTaskAsync(task, cancellationToken);
                }

                break;

            case BulkEditActionType.AddTag:
                await tagService.AssignTagAsync(task.Id, action.Value, cancellationToken);
                break;

            case BulkEditActionType.MoveToProject:
                if (Guid.TryParse(action.Value, out var projectId))
                {
                    task.ProjectId = projectId;
                    await taskService.UpdateTaskAsync(task, cancellationToken);
                }

                break;

            case BulkEditActionType.SetCategory:
                if (Guid.TryParse(action.Value, out var categoryId))
                {
                    task.CategoryId = categoryId;
                    await taskService.UpdateTaskAsync(task, cancellationToken);
                }

                break;

            case BulkEditActionType.MarkCompleted:
                if (!task.IsCompleted)
                {
                    await taskService.CompleteTaskAsync(task.Id, cancellationToken);
                }

                break;

            case BulkEditActionType.Delete:
                await taskService.DeleteTaskAsync(task.Id, cancellationToken);
                break;
        }
    }
}
