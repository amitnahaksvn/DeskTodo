using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>
/// Task Group use cases: creating/editing a named bundle of existing <see cref="TaskTemplate"/>s,
/// and instantiating every member of a group onto a chosen day in one click.
/// </summary>
public interface ITaskGroupService
{
    Task<IReadOnlyList<TaskGroup>> GetGroupsAsync(CancellationToken cancellationToken = default);

    Task<TaskGroup> CreateGroupAsync(string name, IReadOnlyList<Guid> templateIds, CancellationToken cancellationToken = default);

    /// <summary>Replaces the group's name and member template ids wholesale. Throws if the group doesn't exist.</summary>
    Task UpdateGroupAsync(Guid groupId, string name, IReadOnlyList<Guid> templateIds, CancellationToken cancellationToken = default);

    Task DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates one task per member template (via <see cref="ITaskTemplateService.CreateTaskFromTemplateAsync"/>,
    /// so checklist copying and every other "new task from template" behavior stays in one
    /// place) on <paramref name="planDate"/>, in the group's own member order. A member
    /// template id that no longer exists is silently skipped rather than failing the whole
    /// batch — the group still creates whatever tasks it can. Returns the created tasks, in
    /// the same order they were created.
    /// </summary>
    Task<IReadOnlyList<TaskItem>> CreateTasksFromGroupAsync(Guid groupId, DateOnly planDate, CancellationToken cancellationToken = default);
}
