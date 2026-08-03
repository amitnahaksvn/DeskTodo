using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class MilestonesViewModelTests
{
    private readonly Mock<IMilestoneService> _milestoneService = new();
    private readonly MilestonesViewModel _sut;

    public MilestonesViewModelTests()
    {
        _sut = new MilestonesViewModel(_milestoneService.Object, NullLogger<MilestonesViewModel>.Instance);
    }

    private static Milestone CreateMilestone(string title, DateOnly? targetDate = null, bool isCompleted = false, int totalTasks = 0, int completedTasks = 0)
    {
        var milestone = new Milestone { Title = title, TargetDate = targetDate, IsCompleted = isCompleted };
        for (var i = 0; i < totalTasks; i++)
        {
            var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = $"Task {i}" };
            if (i < completedTasks)
            {
                task.Complete();
            }

            milestone.Tasks.Add(task);
        }

        return milestone;
    }

    [Fact]
    public async Task LoadAsync_WithNoMilestones_SetsTheEmptyFlag()
    {
        _milestoneService.Setup(s => s.GetMilestonesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.LoadAsync();

        Assert.True(_sut.HasNoMilestones);
        Assert.Empty(_sut.Milestones);
    }

    [Fact]
    public async Task LoadAsync_PopulatesDisplayFieldsAndTaskProgress()
    {
        var milestone = CreateMilestone("Ship v1", new DateOnly(2026, 9, 1), totalTasks: 3, completedTasks: 1);
        _milestoneService.Setup(s => s.GetMilestonesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([milestone]);

        await _sut.LoadAsync();

        Assert.False(_sut.HasNoMilestones);
        var row = Assert.Single(_sut.Milestones);
        Assert.Equal("Ship v1", row.Title);
        Assert.Equal("Sep 1, 2026", row.TargetDateDisplay);
        Assert.Equal("1/3 tasks done", row.ProgressDisplay);
        Assert.Equal("Mark complete", row.ToggleButtonLabel);
    }

    [Fact]
    public async Task LoadAsync_WithNoTargetDateOrTasks_ShowsFallbackText()
    {
        var milestone = CreateMilestone("Someday");
        _milestoneService.Setup(s => s.GetMilestonesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([milestone]);

        await _sut.LoadAsync();

        var row = _sut.Milestones[0];
        Assert.Equal("No target date", row.TargetDateDisplay);
        Assert.Equal("No linked tasks", row.ProgressDisplay);
    }

    [Fact]
    public async Task AddMilestoneAsync_WithATitle_CreatesItAndReloads()
    {
        _milestoneService.SetupSequence(s => s.GetMilestonesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([])
            .ReturnsAsync([CreateMilestone("Ship v1")]);
        await _sut.LoadAsync();
        _sut.NewMilestoneTitle = "  Ship v1  ";
        var targetDate = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        _sut.NewMilestoneTargetDate = targetDate;

        await _sut.AddMilestoneCommand.ExecuteAsync(null);

        _milestoneService.Verify(s => s.CreateMilestoneAsync("Ship v1", null, new DateOnly(2026, 9, 1), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(string.Empty, _sut.NewMilestoneTitle);
        Assert.Null(_sut.NewMilestoneTargetDate);
        Assert.Single(_sut.Milestones);
    }

    [Fact]
    public async Task AddMilestoneAsync_WithABlankTitle_DoesNotCreateAnything()
    {
        await _sut.AddMilestoneCommand.ExecuteAsync(null);

        _milestoneService.Verify(s => s.CreateMilestoneAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToggleCompleteCommand_TogglesCompletion()
    {
        var milestone = CreateMilestone("Ship v1");
        _milestoneService.Setup(s => s.GetMilestonesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([milestone]);
        await _sut.LoadAsync();
        var row = _sut.Milestones[0];

        await row.ToggleCompleteCommand.ExecuteAsync(null);

        _milestoneService.Verify(s => s.SetCompletedAsync(milestone.Id, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCommand_DeletesTheMilestone()
    {
        var milestone = CreateMilestone("Ship v1");
        _milestoneService.Setup(s => s.GetMilestonesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([milestone]);
        await _sut.LoadAsync();
        var row = _sut.Milestones[0];

        await row.DeleteCommand.ExecuteAsync(null);

        _milestoneService.Verify(s => s.DeleteMilestoneAsync(milestone.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
