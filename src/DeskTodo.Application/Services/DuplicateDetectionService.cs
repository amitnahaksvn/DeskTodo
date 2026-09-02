using System.Text.RegularExpressions;
using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IDuplicateDetectionService"/>
public sealed partial class DuplicateDetectionService : IDuplicateDetectionService
{
    private const double MinimumScoreToReport = 0.6;
    private const double ContextBoost = 0.1;

    public IReadOnlyList<DuplicateCandidate> FindPossibleDuplicates(string title, DateOnly planDate, Guid? categoryId, IEnumerable<TaskItem> candidates)
    {
        var normalizedTitle = Normalize(title);
        if (normalizedTitle.Length == 0)
        {
            return [];
        }

        var titleTokens = Tokenize(normalizedTitle);
        var results = new List<DuplicateCandidate>();

        foreach (var candidate in candidates)
        {
            var normalizedCandidateTitle = Normalize(candidate.Title);

            // Level 1 — exact match.
            var score = normalizedCandidateTitle == normalizedTitle
                ? 1.0
                // Level 2 — token similarity (Jaccard index over normalized word sets).
                : JaccardSimilarity(titleTokens, Tokenize(normalizedCandidateTitle));

            // Level 3 (partial) — same day and category nudges an already-similar title
            // higher, standing in for the fuller context weighting the spec describes.
            // Harmless no-op when score is already 0 or 1.
            if (score > 0 && candidate.PlanDate == planDate && candidate.CategoryId == categoryId)
            {
                score = Math.Min(1.0, score + ContextBoost);
            }

            if (score >= MinimumScoreToReport)
            {
                results.Add(new DuplicateCandidate(candidate, score));
            }
        }

        return results.OrderByDescending(r => r.SimilarityScore).ToList();
    }

    private static double JaccardSimilarity(IReadOnlySet<string> a, IReadOnlySet<string> b)
    {
        if (a.Count == 0 || b.Count == 0)
        {
            return 0;
        }

        var intersection = a.Intersect(b).Count();
        var union = a.Union(b).Count();
        return union == 0 ? 0 : (double)intersection / union;
    }

    private static HashSet<string> Tokenize(string normalized) =>
        normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

    private static string Normalize(string title) =>
        CollapseWhitespaceRegex().Replace(NonWordCharacterRegex().Replace(title.ToLowerInvariant(), " "), " ").Trim();

    [GeneratedRegex(@"[^\w\s]")]
    private static partial Regex NonWordCharacterRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex CollapseWhitespaceRegex();
}
