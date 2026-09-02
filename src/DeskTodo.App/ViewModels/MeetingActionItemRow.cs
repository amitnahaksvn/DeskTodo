using CommunityToolkit.Mvvm.ComponentModel;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// One action-item candidate in <see cref="MeetingSessionViewModel"/>'s review list — either
/// extracted by <see cref="Application.Services.IMeetingActionExtractor"/> from the meeting
/// notes, or added manually. <see cref="IsIncluded"/> is the user's "create this as a task on
/// End Meeting" toggle, matching <c>SelectableTemplateOption</c>'s established checkbox-row shape.
/// </summary>
public sealed partial class MeetingActionItemRow(string title, string? owner, string? deadlineText, DateTime? dueDate) : ObservableObject
{
    public string Title { get; } = title;

    public string? Owner { get; } = owner;

    public string? DeadlineText { get; } = deadlineText;

    public DateTime? DueDate { get; } = dueDate;

    [ObservableProperty]
    public partial bool IsIncluded { get; set; } = true;
}
