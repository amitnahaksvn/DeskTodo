namespace DeskTodo.App.ViewModels;

/// <summary>One stat card on the Dashboard — a plain (label, already-formatted value) pair, not a live-bound value, since <see cref="AnalyticsViewModel.Tiles"/> rebuilds the whole list whenever any contributing property changes.</summary>
public sealed record AnalyticsTile(string Label, string Value);
