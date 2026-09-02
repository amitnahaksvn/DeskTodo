using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>Milestone use cases: create/update/complete/delete a milestone. Linking a task to a milestone is a plain <see cref="TaskItem.MilestoneId"/> property set through <see cref="ITaskService"/>'s normal update path, the same way linking a task to a Category works — no dedicated link/unlink method here.</summary>
public interface IMilestoneService
{
    Task<IReadOnlyList<Milestone>> GetMilestonesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <paramref name="projectId"/> is Feature 50's project checkpoint linkage — when given, the
    /// new milestone is appended to the end of that project's existing <see cref="Milestone.Order"/>
    /// sequence; null (the default) creates a standalone milestone, same as before Feature 50.
    /// </summary>
    Task<Milestone> CreateMilestoneAsync(string title, string? description, DateOnly? targetDate, Guid? projectId = null, CancellationToken cancellationToken = default);

    Task UpdateMilestoneAsync(Guid milestoneId, string title, string? description, DateOnly? targetDate, CancellationToken cancellationToken = default);

    Task SetCompletedAsync(Guid milestoneId, bool isCompleted, CancellationToken cancellationToken = default);

    /// <summary>Linked tasks are unlinked, not deleted — see <see cref="Abstractions.IMilestoneRepository.DeleteAsync"/>.</summary>
    Task DeleteMilestoneAsync(Guid milestoneId, CancellationToken cancellationToken = default);
}
