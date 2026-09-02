using DeskTodo.Application.Services;

namespace DeskTodo.App.ViewModels;

/// <summary>A <see cref="SensitiveDataMatch"/> plus which of the task editor's fields it was found in — Feature 76, Roadmap-39-100.md.</summary>
public sealed record TaskFieldSensitiveMatch(string FieldName, SensitiveDataMatch Match)
{
    public string DisplayText => $"{FieldName} — {Match.PatternName}: \"{Match.MatchedText}\"";
}
