namespace DeskTodo.Application.DTOs;

/// <summary>
/// One category's row in the Dashboard's breakdown — Phase 24's "Time Per Project"/"Category
/// Analytics" deliverable, scoped to Category rather than "Project" (Phase 25's concept,
/// which doesn't exist yet — see docs/ARCHITECTURE.md's "Phase 24" section). <see cref="CategoryId"/>
/// is null for the "No Category" bucket.
/// </summary>
public sealed record CategoryAnalytics(Guid? CategoryId, string CategoryName, string ColorHex, int TotalCount, int CompletedCount, int FocusMinutes)
{
    public double CompletionRate => TotalCount == 0 ? 0 : CompletedCount * 100.0 / TotalCount;
}
