namespace DeskTodo.App.ViewModels;

/// <summary>One <see cref="Domain.Entities.JournalEntry"/> as shown in <see cref="JournalViewModel"/>'s list.</summary>
public sealed record JournalEntryOption(Guid Id, string Title, string Content, string? Mood, string DateDisplay);
