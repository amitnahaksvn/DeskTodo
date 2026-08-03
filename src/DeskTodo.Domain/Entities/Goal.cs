namespace DeskTodo.Domain.Entities;

/// <summary>
/// A personal, ongoing habit-style target — "read more this year," "meditate daily" — with
/// no fixed end date, tracked by marking it done per calendar day rather than by completing
/// a single task. Distinct from <see cref="Milestone"/>, which is the project-management
/// flavor of "goal" (a fixed deliverable with a target date that tasks link to).
/// </summary>
public sealed class Goal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Hidden from the active Goals list without losing completion history — mirrors <see cref="TaskItem.IsArchived"/>'s reasoning.</summary>
    public bool IsArchived { get; set; }

    /// <summary>One row per calendar day this goal was marked done — see <see cref="GetCurrentStreak"/> for why this is a log, not a cached streak counter.</summary>
    public ICollection<GoalCompletion> Completions { get; set; } = [];

    /// <summary>
    /// Consecutive days marked done, walking backward from <paramref name="today"/>. A
    /// streak that doesn't include today or yesterday is 0 — missing yesterday breaks it,
    /// the same way a real habit streak would. Computed from the completion log on every
    /// call rather than stored, so it can never drift out of sync with what was actually
    /// logged (e.g. after deleting a stray completion) the way a cached counter could.
    /// </summary>
    public int GetCurrentStreak(DateOnly today)
    {
        var completedDates = Completions.Select(c => c.CompletedDate).ToHashSet();
        var cursor = completedDates.Contains(today) ? today : today.AddDays(-1);

        if (!completedDates.Contains(cursor))
        {
            return 0;
        }

        var streak = 0;
        while (completedDates.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }
}
