using DeskTodo.Domain.Enums;

namespace DeskTodo.Domain.Entities;

/// <summary>
/// Roadmap-39-100.md Feature 91 — a saved, reusable export configuration ("Weekly Project
/// Report: CSV, Project = Current, Date Range = This Week"), applied against the existing
/// Phase 14 export pipeline rather than a new export format. A profile with no
/// <see cref="ProjectId"/> exports every project (the spec's "Project: Current" is interpreted
/// as "a specific project, chosen once when the profile is saved" — see this feature's own
/// roadmap entry for why, and for what "Fields" subsetting was deliberately not built).
/// </summary>
public sealed class ExportProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public ExportFormat Format { get; set; } = ExportFormat.Csv;

    /// <summary>Null means every project (and tasks with no project at all); set means only that project's tasks.</summary>
    public Guid? ProjectId { get; set; }

    public ExportDateRange DateRange { get; set; } = ExportDateRange.All;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
