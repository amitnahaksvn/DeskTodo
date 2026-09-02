namespace DeskTodo.Application.Services;

/// <summary>
/// One action item candidate pulled out of free-text meeting notes by
/// <see cref="IMeetingActionExtractor"/> — not yet a <see cref="Domain.Entities.TaskItem"/>, just
/// a proposal the user reviews (and can exclude) before Meeting Mode's "Create Tasks" step turns
/// the accepted ones into real tasks.
/// </summary>
public sealed record ActionCandidate(string Title, string? Owner, string? DeadlineText, DateTime? DueDate);

/// <summary>
/// Feature 59 (Roadmap-39-100.md) — turns meeting notes into structured action candidates.
/// Kept as an interface, same reasoning as <see cref="IQuickAddParser"/>: "Implement deterministic
/// parsing first. AI can be plugged in later" is this feature's own spec note, so a future
/// AI-backed implementation can replace <see cref="RuleBasedMeetingActionExtractor"/> without
/// Meeting Mode changing at all.
/// </summary>
public interface IMeetingActionExtractor
{
    IReadOnlyList<ActionCandidate> Extract(string notes, DateOnly today);
}
