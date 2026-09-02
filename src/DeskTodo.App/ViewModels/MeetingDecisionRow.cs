namespace DeskTodo.App.ViewModels;

/// <summary>One decision recorded during a meeting, pending commit to the Decision Log (Feature 57) when the meeting ends.</summary>
public sealed record MeetingDecisionRow(string Title, string DecisionText);
