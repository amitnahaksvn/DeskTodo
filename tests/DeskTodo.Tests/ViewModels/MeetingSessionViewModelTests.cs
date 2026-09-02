using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class MeetingSessionViewModelTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly Mock<IJournalService> _journalService = new();
    private readonly Mock<IDecisionService> _decisionService = new();
    private readonly Mock<IMeetingActionExtractor> _actionExtractor = new();
    private readonly MeetingSessionViewModel _sut;

    public MeetingSessionViewModelTests()
    {
        _taskService.Setup(s => s.CreateTaskAsync(
                It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TaskPriority>(),
                It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateOnly planDate, string title, string? description, TaskPriority _, Guid? _, DateTime? dueDate, Guid? _, CancellationToken _) =>
                new TaskItem { PlanDate = planDate, Title = title, Description = description, DueDate = dueDate });

        _sut = new MeetingSessionViewModel(
            _taskService.Object,
            _journalService.Object,
            _decisionService.Object,
            _actionExtractor.Object,
            TimeProvider.System,
            NullLogger<MeetingSessionViewModel>.Instance);
    }

    [Fact]
    public void ExtractActionItemsCommand_PopulatesActionItemsFromTheExtractor_AllIncludedByDefault()
    {
        _sut.Notes = "John will review the API by Friday.";
        _actionExtractor.Setup(e => e.Extract(_sut.Notes, It.IsAny<DateOnly>()))
            .Returns([new ActionCandidate("Review the API", "John", "Friday", DateTime.Today.AddDays(2))]);

        _sut.ExtractActionItemsCommand.Execute(null);

        var item = Assert.Single(_sut.ActionItems);
        Assert.Equal("Review the API", item.Title);
        Assert.Equal("John", item.Owner);
        Assert.True(item.IsIncluded);
    }

    [Fact]
    public void ExtractActionItemsCommand_WithNoCandidates_SetsAStatusMessage()
    {
        _actionExtractor.Setup(e => e.Extract(It.IsAny<string>(), It.IsAny<DateOnly>())).Returns([]);

        _sut.ExtractActionItemsCommand.Execute(null);

        Assert.Contains("No action items", _sut.StatusMessage);
    }

    [Fact]
    public void AddActionItemCommand_WithATitle_AddsAnIncludedRow_AndClearsTheInputs()
    {
        _sut.NewActionItemTitle = "Follow up with legal";
        _sut.NewActionItemOwner = "Priya";

        _sut.AddActionItemCommand.Execute(null);

        var item = Assert.Single(_sut.ActionItems);
        Assert.Equal("Follow up with legal", item.Title);
        Assert.Equal("Priya", item.Owner);
        Assert.True(item.IsIncluded);
        Assert.Equal(string.Empty, _sut.NewActionItemTitle);
        Assert.Equal(string.Empty, _sut.NewActionItemOwner);
    }

    [Fact]
    public void AddActionItemCommand_WithABlankTitle_DoesNothing()
    {
        _sut.NewActionItemTitle = "   ";

        _sut.AddActionItemCommand.Execute(null);

        Assert.Empty(_sut.ActionItems);
    }

    [Fact]
    public void RemoveActionItemCommand_RemovesTheGivenRow()
    {
        _sut.NewActionItemTitle = "Row A";
        _sut.AddActionItemCommand.Execute(null);
        var row = _sut.ActionItems[0];

        _sut.RemoveActionItemCommand.Execute(row);

        Assert.Empty(_sut.ActionItems);
    }

    [Fact]
    public void AddDecisionCommand_WithTitleAndText_AddsARow_AndClearsTheInputs()
    {
        _sut.NewDecisionTitle = "Use PostgreSQL";
        _sut.NewDecisionText = "Chosen over MongoDB for relational integrity";

        _sut.AddDecisionCommand.Execute(null);

        var decision = Assert.Single(_sut.Decisions);
        Assert.Equal("Use PostgreSQL", decision.Title);
        Assert.Equal(string.Empty, _sut.NewDecisionTitle);
        Assert.Equal(string.Empty, _sut.NewDecisionText);
    }

    [Fact]
    public void AddDecisionCommand_WithoutDecisionText_DoesNothing()
    {
        _sut.NewDecisionTitle = "Use PostgreSQL";
        _sut.NewDecisionText = "   ";

        _sut.AddDecisionCommand.Execute(null);

        Assert.Empty(_sut.Decisions);
    }

    [Fact]
    public void AddFollowUpCommand_WithATitle_AddsARow()
    {
        _sut.NewFollowUpTitle = "Send meeting summary";

        _sut.AddFollowUpCommand.Execute(null);

        var followUp = Assert.Single(_sut.FollowUps);
        Assert.Equal("Send meeting summary", followUp.Title);
        Assert.Equal(string.Empty, _sut.NewFollowUpTitle);
    }

    [Fact]
    public async Task EndMeetingAsync_CreatesATaskForEachIncludedActionItem_AndSkipsExcludedOnes()
    {
        _sut.Title = "Weekly Sync";
        _sut.NewActionItemTitle = "Included item";
        _sut.AddActionItemCommand.Execute(null);
        _sut.NewActionItemTitle = "Excluded item";
        _sut.AddActionItemCommand.Execute(null);
        _sut.ActionItems[1].IsIncluded = false;

        await _sut.EndMeetingCommand.ExecuteAsync(null);

        _taskService.Verify(s => s.CreateTaskAsync(
            It.IsAny<DateOnly>(), "Included item", It.IsAny<string?>(), It.IsAny<TaskPriority>(),
            It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
        _taskService.Verify(s => s.CreateTaskAsync(
            It.IsAny<DateOnly>(), "Excluded item", It.IsAny<string?>(), It.IsAny<TaskPriority>(),
            It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EndMeetingAsync_EncodesTheOwnerIntoTheTaskDescription()
    {
        _sut.NewActionItemTitle = "Review API";
        _sut.NewActionItemOwner = "John";
        _sut.AddActionItemCommand.Execute(null);

        await _sut.EndMeetingCommand.ExecuteAsync(null);

        _taskService.Verify(s => s.CreateTaskAsync(
            It.IsAny<DateOnly>(), "Review API", "Owner: John", It.IsAny<TaskPriority>(),
            It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EndMeetingAsync_CreatesATaskForEachFollowUp()
    {
        _sut.NewFollowUpTitle = "Send summary";
        _sut.AddFollowUpCommand.Execute(null);

        await _sut.EndMeetingCommand.ExecuteAsync(null);

        _taskService.Verify(s => s.CreateTaskAsync(
            It.IsAny<DateOnly>(), "Send summary", It.IsAny<string?>(), It.IsAny<TaskPriority>(),
            It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EndMeetingAsync_RecordsEachDecisionToTheDecisionLog()
    {
        _sut.Title = "Weekly Sync";
        _sut.NewDecisionTitle = "Use PostgreSQL";
        _sut.NewDecisionText = "Better relational support";
        _sut.AddDecisionCommand.Execute(null);

        await _sut.EndMeetingCommand.ExecuteAsync(null);

        _decisionService.Verify(s => s.RecordDecisionAsync(
            "Use PostgreSQL", "From meeting: Weekly Sync", "Better relational support", null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EndMeetingAsync_WithNotes_SavesThemToTheJournal()
    {
        _sut.Title = "Weekly Sync";
        _sut.Notes = "Discussed the roadmap.";

        await _sut.EndMeetingCommand.ExecuteAsync(null);

        _journalService.Verify(s => s.AddEntryAsync(
            It.IsAny<DateOnly>(), "Meeting: Weekly Sync", It.Is<string>(c => c.Contains("Discussed the roadmap.")), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EndMeetingAsync_WithNoNotes_DoesNotTouchTheJournal()
    {
        await _sut.EndMeetingCommand.ExecuteAsync(null);

        _journalService.Verify(s => s.AddEntryAsync(
            It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EndMeetingAsync_SetsHasEnded_AndIsGuardedAgainstRunningTwice()
    {
        _sut.NewFollowUpTitle = "One follow-up";
        _sut.AddFollowUpCommand.Execute(null);

        await _sut.EndMeetingCommand.ExecuteAsync(null);
        Assert.True(_sut.HasEnded);

        await _sut.EndMeetingCommand.ExecuteAsync(null);

        _taskService.Verify(s => s.CreateTaskAsync(
            It.IsAny<DateOnly>(), "One follow-up", It.IsAny<string?>(), It.IsAny<TaskPriority>(),
            It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
