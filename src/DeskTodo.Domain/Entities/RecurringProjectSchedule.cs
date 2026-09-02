using DeskTodo.Domain.Enums;

namespace DeskTodo.Domain.Entities;

/// <summary>
/// Roadmap-39-100.md Feature 87 — recurrence applied at the project level: on the configured
/// <see cref="Frequency"/>, generate a brand-new <see cref="Entities.Project"/> (with tasks and
/// milestones) from <see cref="ProjectTemplateId"/>, the same way Phase 19's per-task recurrence
/// generates a new <see cref="TaskItem"/> on its own cadence. Distinct from Phase 19: that
/// recurs one task; this recurs an entire <see cref="ProjectTemplate"/> (Feature 86)'s structure.
/// </summary>
public sealed class RecurringProjectSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public Guid ProjectTemplateId { get; set; }

    public ProjectTemplate? ProjectTemplate { get; set; }

    /// <summary>Display color as a "#RRGGBB" hex string, applied to every project this schedule generates — mirrors <see cref="Project.ColorHex"/> since a schedule stands in for the user picking a color at creation time each occurrence.</summary>
    public required string ColorHex { get; set; }

    public ProjectRecurrenceFrequency Frequency { get; set; } = ProjectRecurrenceFrequency.Monthly;

    public DateOnly StartDate { get; set; }

    /// <summary>
    /// A <see cref="string.Format(string, object?)"/> pattern for each generated project's
    /// name — <c>"{0}"</c> is replaced with the occurrence's start date (e.g.
    /// <c>"Monthly Reporting — {0:MMMM yyyy}"</c>). Defaults to the schedule's own
    /// <see cref="Name"/> followed by the date when left blank.
    /// </summary>
    public string GeneratedProjectNamePattern { get; set; } = string.Empty;

    /// <summary>The next occurrence's start date this schedule hasn't generated a project for yet — advances by <see cref="Frequency"/> each time <c>RecurringProjectScheduleService.GenerateDueProjectsAsync</c> materializes one.</summary>
    public DateOnly NextOccurrenceDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Ids of every <see cref="Entities.Project"/> this schedule has generated so far, oldest first — lets the UI show a schedule's history and link back to it (Feature 87's "retain a link to the originating template" requirement, applied per-generated-project rather than only per-template).</summary>
    public List<Guid> GeneratedProjectIds { get; set; } = [];
}
