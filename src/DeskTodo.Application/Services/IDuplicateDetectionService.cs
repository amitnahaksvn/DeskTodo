using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>One candidate match from <see cref="IDuplicateDetectionService.FindPossibleDuplicates"/> — Feature 47.</summary>
public sealed record DuplicateCandidate(TaskItem Task, double SimilarityScore);

/// <summary>
/// Feature 47 (Roadmap-39-100.md) — Levels 1 and 2 of the spec's three-level detection (exact
/// normalized-title match, then token-similarity), plus a same-day/same-category score boost
/// standing in for Level 3's fuller context weighting. Level 3's full "existing status" and
/// Level 4's semantic-embeddings tier are both out of scope for this pass — see the scope note
/// where this is wired into <c>WidgetViewModel.AddTaskAsync</c>.
/// </summary>
public interface IDuplicateDetectionService
{
    /// <summary>
    /// Candidates scored 0 (no similarity) to 1 (exact normalized match) against
    /// <paramref name="candidates"/> — callers pass in the pool to check against (e.g. today's
    /// incomplete tasks) rather than this service loading tasks itself, keeping it a pure,
    /// easily-testable text/context comparison. Only scores at or above 0.6 are returned.
    /// </summary>
    IReadOnlyList<DuplicateCandidate> FindPossibleDuplicates(string title, DateOnly planDate, Guid? categoryId, IEnumerable<TaskItem> candidates);
}
