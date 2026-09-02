namespace DeskTodo.App.ViewModels;

/// <summary>One logged <see cref="Domain.Entities.Distraction"/> as shown in <see cref="DistractionLogViewModel"/>'s list.</summary>
public sealed record DistractionOption(string CategoryDisplay, int DurationMinutes, string? Notes, string StartedAtDisplay);
