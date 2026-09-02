using DeskTodo.Domain.Enums;

namespace DeskTodo.Domain.Entities;

/// <summary>
/// Feature 39 (Roadmap-39-100.md) — a quick, unsorted capture: "I need to write this down now
/// and organize it later," as opposed to a <see cref="TaskItem"/>, which already carries a
/// day/priority/category decision. Deliberately just free text plus lifecycle state — no
/// due date/priority/tags of its own, since those are what *converting* to a task is for.
/// </summary>
public sealed class InboxItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Content { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public InboxItemStatus Status { get; private set; } = InboxItemStatus.Unprocessed;

    /// <summary>Set once <see cref="Status"/> becomes <see cref="InboxItemStatus.Converted"/> or <see cref="InboxItemStatus.Archived"/> — null while still <see cref="InboxItemStatus.Unprocessed"/>.</summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>The task this item became, once converted — SetNull on that task's hard delete, same survives-permanent-deletion pattern as <see cref="TaskHistory.TaskId"/>.</summary>
    public Guid? ConvertedTaskId { get; private set; }

    public TaskItem? ConvertedTask { get; private set; }

    public void MarkConverted(Guid taskId)
    {
        Status = InboxItemStatus.Converted;
        ConvertedTaskId = taskId;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = InboxItemStatus.Archived;
        ProcessedAt = DateTime.UtcNow;
    }
}
