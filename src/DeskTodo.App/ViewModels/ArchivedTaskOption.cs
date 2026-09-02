namespace DeskTodo.App.ViewModels;

/// <summary>One archived <see cref="Domain.Entities.TaskItem"/> as shown in <see cref="ArchiveViewModel"/>'s Tasks list.</summary>
public sealed record ArchivedTaskOption(Guid Id, string Title);

/// <summary>One archived <see cref="Domain.Entities.Project"/> as shown in <see cref="ArchiveViewModel"/>'s Projects list.</summary>
public sealed record ArchivedProjectOption(Guid Id, string Name);
