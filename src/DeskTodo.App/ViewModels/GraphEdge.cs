using Avalonia;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// One relationship edge in <see cref="TaskGraphViewModel"/>'s canvas, connecting two
/// already-positioned <see cref="GraphNode"/>s. <see cref="Start"/>/<see cref="End"/> are
/// <see cref="Point"/>s (rather than four separate doubles) so the graph window's XAML can bind
/// them directly to a <c>Line</c>'s <c>StartPoint</c>/<c>EndPoint</c>. <see cref="Description"/>
/// backs the plain-text relationship list shown alongside the canvas (e.g. for screen-reader
/// users, or anyone who'd rather read than click nodes).
/// </summary>
public sealed record GraphEdge(Guid RelationshipId, string Label, Point Start, Point End, string Description);
