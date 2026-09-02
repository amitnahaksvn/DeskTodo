namespace DeskTodo.App.ViewModels;

/// <summary>One logged <see cref="Domain.Entities.FocusSession"/> as shown in <see cref="WorkSessionHistoryViewModel"/>'s list.</summary>
public sealed record WorkSessionOption(string TaskTitle, string TypeDisplay, int DurationMinutes, string StartedAtDisplay);
