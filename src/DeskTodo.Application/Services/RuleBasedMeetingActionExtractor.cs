using System.Text.RegularExpressions;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IMeetingActionExtractor"/>
/// <remarks>
/// One candidate per line: a line must start with "&lt;Name&gt; will/needs to/should/..." to be
/// recognized at all — this is deliberately conservative (misses action items phrased as
/// questions or imperatives with no named owner) rather than guessing and producing noisy
/// candidates. A recognized owner clause matches this feature's own spec examples exactly
/// ("John will review the API by Friday.").
/// </remarks>
public sealed partial class RuleBasedMeetingActionExtractor(IQuickAddParser quickAddParser) : IMeetingActionExtractor
{
    public IReadOnlyList<ActionCandidate> Extract(string notes, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return [];
        }

        var candidates = new List<ActionCandidate>();
        var lines = notes.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            var lineMatch = ActionLineRegex().Match(line);
            if (!lineMatch.Success)
            {
                continue;
            }

            var owner = lineMatch.Groups["owner"].Value;
            var action = lineMatch.Groups["action"].Value.Trim();

            string? deadlineText = null;
            DateTime? dueDate = null;
            var deadlineMatch = DeadlineRegex().Match(action);
            if (deadlineMatch.Success)
            {
                deadlineText = deadlineMatch.Groups["deadline"].Value;
                action = DeadlineRegex().Replace(action, string.Empty, 1);
                // Reuse Feature 41's deterministic date parser rather than re-implementing
                // "friday"/"tomorrow"/explicit-date resolution a second time; a phrase like
                // "next week" it doesn't recognize just leaves DueDate null — DeadlineText still
                // carries the raw phrase for display.
                dueDate = quickAddParser.Parse(deadlineText, today).DueDate;
            }

            action = action.TrimEnd(' ', '.');
            if (action.Length == 0)
            {
                continue;
            }

            var title = char.ToUpperInvariant(action[0]) + action[1..];
            candidates.Add(new ActionCandidate(title, owner, deadlineText, dueDate));
        }

        return candidates;
    }

    [GeneratedRegex(@"^(?<owner>[A-Z][A-Za-z'-]*)\s+(?:will|needs to|need to|should|is going to|are going to|has to|have to)\s+(?<action>.+)$")]
    private static partial Regex ActionLineRegex();

    [GeneratedRegex(@"\s+(?:by\s+|on\s+)?(?<deadline>next\s+week|next\s+month|tomorrow|today|monday|tuesday|wednesday|thursday|friday|saturday|sunday|\d{4}-\d{2}-\d{2})\.?$", RegexOptions.IgnoreCase)]
    private static partial Regex DeadlineRegex();
}
