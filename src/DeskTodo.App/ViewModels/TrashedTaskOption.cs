namespace DeskTodo.App.ViewModels;

/// <summary>One deleted <see cref="Domain.Entities.TaskItem"/> as shown in <see cref="TrashViewModel"/>'s list — <see cref="DeletedAtDisplay"/> is a display-only, already-formatted string rather than a raw <see cref="DateTime"/>, so the view doesn't need its own date-format converter.</summary>
public sealed record TrashedTaskOption(Guid Id, string Title, string DeletedAtDisplay);
