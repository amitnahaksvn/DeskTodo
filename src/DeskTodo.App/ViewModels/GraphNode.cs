namespace DeskTodo.App.ViewModels;

/// <summary>One task rendered in <see cref="TaskGraphViewModel"/>'s canvas — <see cref="X"/>/<see cref="Y"/> are already-computed absolute canvas coordinates (a fixed hub-and-spoke layout, not a force-directed one).</summary>
public sealed record GraphNode(Guid TaskId, string Title, bool IsCenter, double X, double Y);
