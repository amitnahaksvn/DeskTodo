using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class GoalsViewModelTests
{
    private readonly Mock<IGoalService> _goalService = new();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero));
    private readonly GoalsViewModel _sut;

    public GoalsViewModelTests()
    {
        _sut = new GoalsViewModel(_goalService.Object, _timeProvider, NullLogger<GoalsViewModel>.Instance);
    }

    private static Goal CreateGoal(string name, params DateOnly[] completedDates)
    {
        var goal = new Goal { Name = name };
        foreach (var date in completedDates)
        {
            goal.Completions.Add(new GoalCompletion { GoalId = goal.Id, CompletedDate = date });
        }

        return goal;
    }

    [Fact]
    public async Task LoadAsync_WithNoGoals_SetsTheEmptyFlag()
    {
        _goalService.Setup(s => s.GetGoalsAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.LoadAsync();

        Assert.True(_sut.HasNoGoals);
        Assert.Empty(_sut.Goals);
    }

    [Fact]
    public async Task LoadAsync_PopulatesStreakAndTotalsPerRow()
    {
        var goal = CreateGoal("Meditate", new DateOnly(2026, 8, 15), new DateOnly(2026, 8, 14));
        _goalService.Setup(s => s.GetGoalsAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync([goal]);

        await _sut.LoadAsync();

        Assert.False(_sut.HasNoGoals);
        var row = Assert.Single(_sut.Goals);
        Assert.Equal("Meditate", row.Name);
        Assert.Equal(2, row.CurrentStreak);
        Assert.Equal(2, row.TotalCompletions);
        Assert.True(row.IsCompletedToday);
        Assert.Equal("Done today ✓", row.ToggleButtonLabel);
    }

    [Fact]
    public async Task AddGoalAsync_WithAName_CreatesItAndReloads()
    {
        _goalService.SetupSequence(s => s.GetGoalsAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([])
            .ReturnsAsync([CreateGoal("Read more")]);
        await _sut.LoadAsync();
        _sut.NewGoalName = "  Read more  ";

        await _sut.AddGoalCommand.ExecuteAsync(null);

        _goalService.Verify(s => s.CreateGoalAsync("Read more", null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(string.Empty, _sut.NewGoalName);
        Assert.Single(_sut.Goals);
    }

    [Fact]
    public async Task AddGoalAsync_WithABlankName_DoesNotCreateAnything()
    {
        await _sut.AddGoalCommand.ExecuteAsync(null);

        _goalService.Verify(s => s.CreateGoalAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToggleTodayCommand_WhenNotCompletedToday_MarksItCompleted()
    {
        var goal = CreateGoal("Meditate");
        _goalService.Setup(s => s.GetGoalsAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync([goal]);
        await _sut.LoadAsync();
        var row = _sut.Goals[0];
        Assert.False(row.IsCompletedToday);

        await row.ToggleTodayCommand.ExecuteAsync(null);

        _goalService.Verify(s => s.MarkCompletedAsync(goal.Id, new DateOnly(2026, 8, 15), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleTodayCommand_WhenAlreadyCompletedToday_UnmarksIt()
    {
        var goal = CreateGoal("Meditate", new DateOnly(2026, 8, 15));
        _goalService.Setup(s => s.GetGoalsAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync([goal]);
        await _sut.LoadAsync();
        var row = _sut.Goals[0];

        await row.ToggleTodayCommand.ExecuteAsync(null);

        _goalService.Verify(s => s.UnmarkCompletedAsync(goal.Id, new DateOnly(2026, 8, 15), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCommand_DeletesTheGoal()
    {
        var goal = CreateGoal("Meditate");
        _goalService.Setup(s => s.GetGoalsAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync([goal]);
        await _sut.LoadAsync();
        var row = _sut.Goals[0];

        await row.DeleteCommand.ExecuteAsync(null);

        _goalService.Verify(s => s.DeleteGoalAsync(goal.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
