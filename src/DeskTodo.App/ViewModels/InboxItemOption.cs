namespace DeskTodo.App.ViewModels;

/// <summary>One unprocessed <see cref="Domain.Entities.InboxItem"/> as shown in <see cref="InboxViewModel"/>'s queue.</summary>
public sealed record InboxItemOption(Guid Id, string Content, string CreatedAtDisplay);
