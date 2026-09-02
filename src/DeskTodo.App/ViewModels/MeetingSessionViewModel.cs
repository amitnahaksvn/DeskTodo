using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Meeting Mode window (Feature 58, Roadmap-39-100.md) — a temporary, in-memory
/// workspace for one meeting (title, participants, agenda, live notes, decisions and action
/// items captured as they happen), fanning out into real app data only on "End Meeting":
/// action items and follow-ups become tasks (<see cref="ITaskService"/>), decisions are recorded
/// to the existing Decision Log (Feature 57, <see cref="IDecisionService"/>), and the raw notes
/// are saved to the existing Daily Journal (Feature 60, <see cref="IJournalService"/>). No new
/// "Meeting" entity/table exists — everything a meeting produces already has a home in one of
/// those three existing features, so this ViewModel is a scratch pad that writes through to them
/// rather than a fourth persisted concept of its own.
/// </summary>
public sealed partial class MeetingSessionViewModel(
    ITaskService taskService,
    IJournalService journalService,
    IDecisionService decisionService,
    IMeetingActionExtractor actionExtractor,
    TimeProvider timeProvider,
    ILogger<MeetingSessionViewModel> logger) : ViewModelBase
{
    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Participants { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Agenda { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasEnded { get; set; }

    [ObservableProperty]
    public partial string NewActionItemTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewActionItemOwner { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewDecisionTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewDecisionText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewFollowUpTitle { get; set; } = string.Empty;

    public ObservableCollection<MeetingActionItemRow> ActionItems { get; } = [];

    public ObservableCollection<MeetingDecisionRow> Decisions { get; } = [];

    public ObservableCollection<MeetingFollowUpRow> FollowUps { get; } = [];

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    /// <summary>Runs Feature 59's extractor over the current <see cref="Notes"/>, replacing any previously extracted (but not manually added) rows.</summary>
    [RelayCommand]
    private void ExtractActionItems()
    {
        var candidates = actionExtractor.Extract(Notes, Today());
        ActionItems.Clear();
        foreach (var candidate in candidates)
        {
            ActionItems.Add(new MeetingActionItemRow(candidate.Title, candidate.Owner, candidate.DeadlineText, candidate.DueDate));
        }

        StatusMessage = ActionItems.Count == 0
            ? "No action items recognized in the notes yet."
            : $"Found {ActionItems.Count} action item(s) — review and uncheck any that shouldn't become tasks.";
    }

    [RelayCommand]
    private void AddActionItem()
    {
        var title = NewActionItemTitle.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        var owner = string.IsNullOrWhiteSpace(NewActionItemOwner) ? null : NewActionItemOwner.Trim();
        ActionItems.Add(new MeetingActionItemRow(title, owner, deadlineText: null, dueDate: null));
        NewActionItemTitle = string.Empty;
        NewActionItemOwner = string.Empty;
    }

    [RelayCommand]
    private void RemoveActionItem(MeetingActionItemRow row) => ActionItems.Remove(row);

    [RelayCommand]
    private void AddDecision()
    {
        var title = NewDecisionTitle.Trim();
        var text = NewDecisionText.Trim();
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(text))
        {
            return;
        }

        Decisions.Add(new MeetingDecisionRow(title, text));
        NewDecisionTitle = string.Empty;
        NewDecisionText = string.Empty;
    }

    [RelayCommand]
    private void RemoveDecision(MeetingDecisionRow row) => Decisions.Remove(row);

    [RelayCommand]
    private void AddFollowUp()
    {
        var title = NewFollowUpTitle.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        FollowUps.Add(new MeetingFollowUpRow(title));
        NewFollowUpTitle = string.Empty;
    }

    [RelayCommand]
    private void RemoveFollowUp(MeetingFollowUpRow row) => FollowUps.Remove(row);

    /// <summary>
    /// "End Meeting": creates a task per included action item and per follow-up, records each
    /// decision to the Decision Log, and — if any notes were typed — saves them to the Journal.
    /// Guarded against running twice (e.g. a double-click) so nothing is created a second time.
    /// </summary>
    [RelayCommand]
    private async Task EndMeetingAsync()
    {
        if (HasEnded)
        {
            return;
        }

        try
        {
            var today = Today();
            var meetingTitle = string.IsNullOrWhiteSpace(Title) ? "Untitled Meeting" : Title.Trim();
            var tasksCreated = 0;

            foreach (var item in ActionItems.Where(i => i.IsIncluded))
            {
                var description = item.Owner is null ? null : $"Owner: {item.Owner}";
                await taskService.CreateTaskAsync(today, item.Title, description, dueDate: item.DueDate);
                tasksCreated++;
            }

            foreach (var followUp in FollowUps)
            {
                await taskService.CreateTaskAsync(today, followUp.Title, $"Follow-up from meeting: {meetingTitle}", dueDate: followUp.DueDate);
                tasksCreated++;
            }

            foreach (var decision in Decisions)
            {
                await decisionService.RecordDecisionAsync(decision.Title, $"From meeting: {meetingTitle}", decision.DecisionText, alternatives: null, reason: null, projectId: null);
            }

            var savedNotes = false;
            if (!string.IsNullOrWhiteSpace(Notes))
            {
                await journalService.AddEntryAsync(today, $"Meeting: {meetingTitle}", BuildJournalContent(), mood: null);
                savedNotes = true;
            }

            HasEnded = true;
            StatusMessage = $"Meeting ended — {tasksCreated} task(s) created, {Decisions.Count} decision(s) recorded" +
                             (savedNotes ? ", notes saved to the Journal." : ".");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to end meeting '{Title}'", Title);
            StatusMessage = "Something went wrong ending the meeting — nothing further was saved.";
        }
    }

    private string BuildJournalContent()
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(Participants))
        {
            builder.AppendLine($"Participants: {Participants}").AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(Agenda))
        {
            builder.AppendLine("Agenda:").AppendLine(Agenda).AppendLine();
        }

        builder.AppendLine("Notes:").AppendLine(Notes);
        return builder.ToString().TrimEnd();
    }
}
