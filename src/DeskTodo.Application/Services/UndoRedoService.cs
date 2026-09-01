namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IUndoRedoService"/>
public sealed class UndoRedoService : IUndoRedoService
{
    /// <summary>Bounded so a long session's undo history doesn't grow without limit.</summary>
    private const int MaxStackSize = 50;

    private readonly LinkedList<UndoableAction> _undoStack = new();
    private readonly LinkedList<UndoableAction> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public string? NextUndoDescription => _undoStack.Last?.Value.Description;

    public string? NextRedoDescription => _redoStack.Last?.Value.Description;

    public event EventHandler? StateChanged;

    public void Record(string description, Func<Task> undo, Func<Task> redo)
    {
        _undoStack.AddLast(new UndoableAction(description, undo, redo));
        while (_undoStack.Count > MaxStackSize)
        {
            _undoStack.RemoveFirst();
        }

        _redoStack.Clear();
        RaiseStateChanged();
    }

    public async Task<bool> UndoAsync()
    {
        if (_undoStack.Last is not { } node)
        {
            return false;
        }

        _undoStack.RemoveLast();
        await node.Value.Undo();
        _redoStack.AddLast(node.Value);
        RaiseStateChanged();
        return true;
    }

    public async Task<bool> RedoAsync()
    {
        if (_redoStack.Last is not { } node)
        {
            return false;
        }

        _redoStack.RemoveLast();
        await node.Value.Redo();
        _undoStack.AddLast(node.Value);
        RaiseStateChanged();
        return true;
    }

    public void Clear()
    {
        if (_undoStack.Count == 0 && _redoStack.Count == 0)
        {
            return;
        }

        _undoStack.Clear();
        _redoStack.Clear();
        RaiseStateChanged();
    }

    private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    private sealed record UndoableAction(string Description, Func<Task> Undo, Func<Task> Redo);
}
