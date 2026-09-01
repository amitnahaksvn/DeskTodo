using DeskTodo.Application.Services;

namespace DeskTodo.Tests.Application;

public class UndoRedoServiceTests
{
    [Fact]
    public void Record_MakesCanUndoTrue_AndCanRedoFalse()
    {
        var sut = new UndoRedoService();

        sut.Record("Complete task", () => Task.CompletedTask, () => Task.CompletedTask);

        Assert.True(sut.CanUndo);
        Assert.False(sut.CanRedo);
    }

    [Fact]
    public async Task UndoAsync_RunsTheUndoDelegate_AndMovesTheActionToTheRedoStack()
    {
        var sut = new UndoRedoService();
        var undoRan = false;
        sut.Record("Complete task", () => { undoRan = true; return Task.CompletedTask; }, () => Task.CompletedTask);

        var result = await sut.UndoAsync();

        Assert.True(result);
        Assert.True(undoRan);
        Assert.False(sut.CanUndo);
        Assert.True(sut.CanRedo);
    }

    [Fact]
    public async Task RedoAsync_RunsTheRedoDelegate_AndMovesTheActionBackToTheUndoStack()
    {
        var sut = new UndoRedoService();
        var redoRan = false;
        sut.Record("Complete task", () => Task.CompletedTask, () => { redoRan = true; return Task.CompletedTask; });
        await sut.UndoAsync();

        var result = await sut.RedoAsync();

        Assert.True(result);
        Assert.True(redoRan);
        Assert.True(sut.CanUndo);
        Assert.False(sut.CanRedo);
    }

    [Fact]
    public async Task UndoAsync_WithNothingToUndo_ReturnsFalse_AndDoesNotThrow()
    {
        var sut = new UndoRedoService();

        var result = await sut.UndoAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task RedoAsync_WithNothingToRedo_ReturnsFalse_AndDoesNotThrow()
    {
        var sut = new UndoRedoService();

        var result = await sut.RedoAsync();

        Assert.False(result);
    }

    [Fact]
    public void Record_AfterAnUndo_ClearsTheRedoStack()
    {
        var sut = new UndoRedoService();
        sut.Record("First", () => Task.CompletedTask, () => Task.CompletedTask);

        sut.Record("Second", () => Task.CompletedTask, () => Task.CompletedTask);

        Assert.False(sut.CanRedo);
    }

    [Fact]
    public void Clear_ResetsBothStacks()
    {
        var sut = new UndoRedoService();
        sut.Record("First", () => Task.CompletedTask, () => Task.CompletedTask);

        sut.Clear();

        Assert.False(sut.CanUndo);
        Assert.False(sut.CanRedo);
    }

    [Fact]
    public void Record_RaisesStateChanged()
    {
        var sut = new UndoRedoService();
        var raised = false;
        sut.StateChanged += (_, _) => raised = true;

        sut.Record("Action", () => Task.CompletedTask, () => Task.CompletedTask);

        Assert.True(raised);
    }

    [Fact]
    public async Task UndoRedo_MultipleActions_UndoRunsInLastInFirstOutOrder()
    {
        var sut = new UndoRedoService();
        var order = new List<string>();
        sut.Record("A", () => { order.Add("undo-A"); return Task.CompletedTask; }, () => Task.CompletedTask);
        sut.Record("B", () => { order.Add("undo-B"); return Task.CompletedTask; }, () => Task.CompletedTask);

        await sut.UndoAsync();
        await sut.UndoAsync();

        Assert.Equal(["undo-B", "undo-A"], order);
    }

    [Fact]
    public void NextUndoDescription_ReflectsTheMostRecentlyRecordedAction()
    {
        var sut = new UndoRedoService();
        sut.Record("First", () => Task.CompletedTask, () => Task.CompletedTask);
        sut.Record("Second", () => Task.CompletedTask, () => Task.CompletedTask);

        Assert.Equal("Second", sut.NextUndoDescription);
    }
}
