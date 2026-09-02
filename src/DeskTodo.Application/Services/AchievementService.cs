namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IAchievementService"/>
public sealed class AchievementService(ITaskService taskService, IFocusSessionService focusSessionService, IProjectService projectService, IMilestoneService milestoneService) : IAchievementService
{
    public async Task<IReadOnlyList<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await taskService.GetAllTasksAsync(cancellationToken);
        var completedCount = tasks.Count(t => t.IsCompleted);

        var sessions = await focusSessionService.GetAllSessionsAsync(cancellationToken);
        var focusHours = sessions.Sum(s => s.DurationMinutes) / 60.0;

        var projects = await projectService.GetProjectsAsync(cancellationToken);
        // "Completed first project" — interpreted as a project with at least one task, all of
        // them done (there's no explicit Project.IsCompleted flag to check against instead).
        var completedProjectCount = projects.Count(p => p.Tasks.Count > 0 && p.Tasks.All(t => t.IsCompleted));

        var milestones = await milestoneService.GetMilestonesAsync(cancellationToken);
        var completedMilestoneCount = milestones.Count(m => m.IsCompleted);

        // "Maintained task organization" — interpreted as: no task is currently overdue, i.e.
        // nothing has been allowed to slip past its due date unattended.
        var noOverdueTasks = tasks.All(t => !t.IsOverdue);

        return
        [
            new Achievement("First Steps", "Complete your first task", completedCount >= 1, $"{Math.Min(completedCount, 1)}/1"),
            new Achievement("Century", "Complete 100 tasks", completedCount >= 100, $"{completedCount}/100"),
            new Achievement("50 Focus Hours", "Log 50 hours of focus time", focusHours >= 50, $"{focusHours:0.#}/50h"),
            new Achievement("Project Finisher", "Complete every task in a project", completedProjectCount >= 1, $"{completedProjectCount} project(s) fully complete"),
            new Achievement("Milestone Maker", "Complete 5 milestones", completedMilestoneCount >= 5, $"{completedMilestoneCount}/5"),
            new Achievement("Well Organized", "No overdue tasks right now", noOverdueTasks, noOverdueTasks ? "All caught up" : $"{tasks.Count(t => t.IsOverdue)} overdue"),
        ];
    }
}
