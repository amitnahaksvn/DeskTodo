namespace DeskTodo.Application.Services;

/// <summary>
/// Feature 43 (Roadmap-39-100.md) — an app-wide undo/redo stack of already-performed actions.
/// Deliberately scoped to <c>WidgetViewModel</c>'s own most common mutating task actions
/// (complete/reopen, delete/restore, pin/unpin, rename) rather than every possible mutation in
/// the app (bulk operations, grid edits, settings changes) — the same kind of scope cut Phase
/// 11 and Phase 28 both already called out when they explicitly deferred general undo/redo.
/// </summary>
public interface IUndoRedoService
{
    bool CanUndo { get; }

    bool CanRedo { get; }

    /// <summary>A short label for the action that would be undone next (e.g. "Complete \"Buy milk\""), or null if <see cref="CanUndo"/> is false.</summary>
    string? NextUndoDescription { get; }

    /// <summary>A short label for the action that would be redone next, or null if <see cref="CanRedo"/> is false.</summary>
    string? NextRedoDescription { get; }

    /// <summary>Raised whenever <see cref="CanUndo"/>/<see cref="CanRedo"/> may have changed, so a bound Undo/Redo button can refresh its enabled state.</summary>
    event EventHandler? StateChanged;

    /// <summary>
    /// Records an action that has *already been performed* — <paramref name="undo"/> reverses
    /// it, <paramref name="redo"/> re-applies it. Pushing a new action clears the redo stack,
    /// the same semantics every undo/redo system uses.
    /// </summary>
    void Record(string description, Func<Task> undo, Func<Task> redo);

    /// <summary>Runs the most recent action's undo delegate and moves it onto the redo stack. No-ops (returns false) if <see cref="CanUndo"/> is false.</summary>
    Task<bool> UndoAsync();

    /// <summary>Runs the most recently undone action's redo delegate and moves it back onto the undo stack. No-ops (returns false) if <see cref="CanRedo"/> is false.</summary>
    Task<bool> RedoAsync();

    /// <summary>Clears both stacks — used when the widget navigates to a different day, since an undo/redo entry from one day's list doesn't make sense once you're viewing another.</summary>
    void Clear();
}
