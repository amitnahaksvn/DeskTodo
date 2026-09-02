namespace DeskTodo.App.ViewModels;

/// <summary>One follow-up queued during a meeting, created as a real task when the meeting ends.</summary>
public sealed record MeetingFollowUpRow(string Title, DateTime? DueDate = null);
