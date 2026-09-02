namespace DeskTodo.Domain.Enums;

/// <summary>An action a <see cref="Entities.BulkEditRule"/> applies to every matching task.</summary>
public enum BulkEditActionType
{
    SetPriority = 0,
    AddTag = 1,
    MoveToProject = 2,
    SetCategory = 3,
    MarkCompleted = 4,

    /// <summary>Destructive — the roadmap's own "Safety" section requires additional confirmation before this runs, enforced by the ViewModel, not this layer.</summary>
    Delete = 5,
}
