namespace DeskTodo.Domain.Enums;

/// <summary>The outcome of a <see cref="Entities.MigrationRun"/>.</summary>
public enum MigrationStatus
{
    Pending = 0,

    /// <summary>At least one row failed validation — no tasks were created (this feature's "if validation fails, no partial import should remain" requirement, honored by validating every row before importing any of them).</summary>
    Failed = 1,

    Completed = 2,
}
