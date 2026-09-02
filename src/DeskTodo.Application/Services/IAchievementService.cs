namespace DeskTodo.Application.Services;

/// <summary>One progress marker — Feature 62 (Roadmap-39-100.md). Deliberately no points/streaks-as-score — the spec explicitly says "avoid aggressive gamification," so this is a fixed, small set of milestone-style markers, not an open-ended points system.</summary>
public sealed record Achievement(string Title, string Description, bool IsUnlocked, string ProgressDisplay);

/// <summary>
/// Feature 62 — Achievement/Progress System. Computed entirely from data this app already
/// persists (tasks, focus sessions, projects, milestones); no new schema. A handful of the
/// spec's own examples are ambiguous outside a multi-user/points context ("Maintained task
/// organization," "Completed all weekly milestones") — see the implementation's own doc
/// comments for the concrete interpretation each one was given.
/// </summary>
public interface IAchievementService
{
    Task<IReadOnlyList<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken = default);
}
