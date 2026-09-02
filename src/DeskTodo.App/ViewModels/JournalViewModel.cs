using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Daily Journal window (Feature 60, Roadmap-39-100.md) — a date-based personal/work
/// journal, deliberately not another task list (no priority/due date/completion state; see
/// <see cref="Domain.Entities.JournalEntry"/>'s own doc comment).
/// </summary>
public sealed partial class JournalViewModel(IJournalService journalService, TimeProvider timeProvider, ILogger<JournalViewModel> logger) : ViewModelBase
{
    public ObservableCollection<JournalEntryOption> Entries { get; } = [];

    [ObservableProperty]
    public partial DateOnly SelectedDate { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewContent { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewMood { get; set; } = string.Empty;

    public JournalViewModel InitializeToday()
    {
        SelectedDate = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        return this;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = string.IsNullOrWhiteSpace(SearchText)
                ? await journalService.GetEntriesForDateAsync(SelectedDate, cancellationToken)
                : await journalService.SearchAsync(SearchText, cancellationToken);

            Entries.Clear();
            foreach (var entry in entries)
            {
                Entries.Add(new JournalEntryOption(entry.Id, entry.Title, entry.Content, entry.Mood, entry.Date.ToString("ddd, MMM d, yyyy")));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load journal entries");
        }
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task GoToPreviousDayAsync()
    {
        SelectedDate = SelectedDate.AddDays(-1);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task GoToNextDayAsync()
    {
        SelectedDate = SelectedDate.AddDays(1);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddEntryAsync()
    {
        var title = NewTitle.Trim();
        var content = NewContent.Trim();
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
        {
            return;
        }

        try
        {
            await journalService.AddEntryAsync(SelectedDate, title, content, NewMood);
            NewTitle = string.Empty;
            NewContent = string.Empty;
            NewMood = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add a journal entry");
        }
    }

    [RelayCommand]
    private async Task DeleteEntryAsync(Guid entryId)
    {
        try
        {
            await journalService.DeleteEntryAsync(entryId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete journal entry {EntryId}", entryId);
        }
    }
}
