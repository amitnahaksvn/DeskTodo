namespace DeskTodo.App.ViewModels;

/// <summary>One <see cref="Domain.Entities.Decision"/> as shown in <see cref="DecisionLogViewModel"/>'s list.</summary>
public sealed record DecisionOption(Guid Id, string Title, string? Context, string DecisionText, string? Alternatives, string? Reason, string CreatedAtDisplay);
