namespace DeskTodo.Application.DTOs;

/// <summary>One day's row in the Dashboard's Heat Map — how many tasks were completed (and, for context, how many were planned) that day.</summary>
public sealed record DailyCompletionCount(DateOnly Date, int CompletedCount, int TotalCount);
