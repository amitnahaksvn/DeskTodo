using DeskTodo.Domain.Enums;

namespace DeskTodo.Application.Services;

/// <summary>
/// The parsed result of a natural-language quick-add string — Feature 41 (Roadmap-39-100.md).
/// <see cref="Title"/> has every recognized token already stripped out, so it's ready to use
/// as-is.
/// </summary>
public sealed record TaskDraft(
    string Title,
    DateTime? DueDate,
    TaskPriority? Priority,
    string? ProjectName,
    IReadOnlyList<string> Tags,
    int? EstimatedMinutes);

/// <summary>
/// Feature 41 — parses free text into a <see cref="TaskDraft"/>. Kept as an interface (not a
/// concrete class QuickAddViewModel depends on directly) per the spec's own "don't couple the
/// task service directly to an AI provider" note — a future <c>AIQuickAddParser</c> could
/// implement this same contract without QuickAddViewModel changing at all.
/// </summary>
public interface IQuickAddParser
{
    TaskDraft Parse(string input, DateOnly today);
}
