namespace DeskTodo.Application.DTOs;

/// <summary>
/// A flat, format-agnostic shape for one exported/imported task — CSV, JSON and Markdown
/// writers all consume this rather than <c>TaskItem</c> directly, so the file formats don't
/// change shape if the Domain entity grows fields later. Category is carried by name (not
/// <c>CategoryId</c>) since a Guid is meaningless once it leaves this specific database —
/// re-importing into a different DeskTodo install matches categories by name instead.
/// </summary>
public sealed class TaskExportRecord
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public required DateOnly PlanDate { get; init; }

    public DateTime? DueDate { get; init; }

    public string Priority { get; init; } = "Medium";

    public string? Category { get; init; }

    public string? Notes { get; init; }

    public bool IsCompleted { get; init; }

    public bool IsPinned { get; init; }

    public int? EstimatedMinutes { get; init; }
}
