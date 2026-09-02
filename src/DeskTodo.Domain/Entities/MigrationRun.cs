using DeskTodo.Domain.Enums;

namespace DeskTodo.Domain.Entities;

/// <summary>
/// Roadmap-39-100.md Feature 90 — a record of one run through the Source → Reader → Normalizer
/// → Mapper → Validator → Duplicate Resolver → Importer pipeline (Feature 89's Mass Import
/// Wizard is that pipeline's concrete CSV/JSON implementation; this is the "each migration
/// should have an ID and log" persistence Feature 90 asks for on top of it).
/// </summary>
public sealed class MigrationRun
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>What was imported — e.g. a file name — for the user's own reference in the run history, not used by any logic.</summary>
    public required string SourceDescription { get; set; }

    public MigrationStatus Status { get; set; } = MigrationStatus.Pending;

    public int TotalRecords { get; set; }

    public int ImportedCount { get; set; }

    public int SkippedCount { get; set; }

    /// <summary>One line per row's outcome ("Row 3 imported: 'Ship it'", "Row 5 skipped: duplicate of an existing task") — the migration's own report.</summary>
    public List<string> LogEntries { get; set; } = [];

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}
