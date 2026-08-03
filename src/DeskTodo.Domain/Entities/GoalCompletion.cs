namespace DeskTodo.Domain.Entities;

/// <summary>One calendar day a <see cref="Goal"/> was marked done — a log entry, not a mutable flag, so <see cref="Goal.GetCurrentStreak"/> can walk it directly. A (GoalId, CompletedDate) pair is unique — see <c>GoalCompletionConfiguration</c> — since a goal can only be "done" once per day.</summary>
public sealed class GoalCompletion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required Guid GoalId { get; set; }

    public Goal? Goal { get; set; }

    public required DateOnly CompletedDate { get; set; }
}
