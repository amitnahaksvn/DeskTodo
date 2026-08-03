using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class WeekViewModelTests
{
    private readonly Mock<ITaskService> _taskService = new();
    // Saturday, so WeekStart should back up to the preceding Sunday.
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero));
    private readonly WeekViewModel _sut;

    public WeekViewModelTests()
    {
        _sut = new WeekViewModel(_taskService.Object, _timeProvider, NullLogger<WeekViewModel>.Instance);
    }

    [Fact]
    public async Task LoadAsync_WithNoInitialDate_StartsTheWeekOnTheMostRecentSunday()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.LoadAsync();

        Assert.Equal(DayOfWeek.Sunday, _sut.WeekStart.DayOfWeek);
        Assert.Equal(new DateOnly(2026, 8, 9), _sut.WeekStart);
        Assert.Equal(7, _sut.Days.Count);
    }

    [Fact]
    public async Task LoadAsync_ComputesTaskCountsPerDay()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 11), Title = "Task" };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        await _sut.LoadAsync();

        var tuesday = _sut.Days.Single(d => d.Date == new DateOnly(2026, 8, 11));
        Assert.Equal(1, tuesday.TotalCount);
    }

    [Fact]
    public async Task PreviousWeekAsync_NextWeekAsync_NavigateSevenDaysAtATime()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        await _sut.LoadAsync();
        var originalStart = _sut.WeekStart;

        await _sut.PreviousWeekCommand.ExecuteAsync(null);
        Assert.Equal(originalStart.AddDays(-7), _sut.WeekStart);

        await _sut.NextWeekCommand.ExecuteAsync(null);
        Assert.Equal(originalStart, _sut.WeekStart);
    }

    [Fact]
    public async Task GoToCurrentWeekAsync_ReturnsToTodaysWeek()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        await _sut.LoadAsync(new DateOnly(2026, 1, 1));

        await _sut.GoToCurrentWeekCommand.ExecuteAsync(null);

        Assert.Equal(new DateOnly(2026, 8, 9), _sut.WeekStart);
    }

    [Fact]
    public async Task ClickingADayCell_RaisesDateSelected()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        await _sut.LoadAsync();
        DateOnly? selected = null;
        _sut.DateSelected += (_, date) => selected = date;

        _sut.Days.Single(d => d.Date == new DateOnly(2026, 8, 11)).SelectCommand.Execute(null);

        Assert.Equal(new DateOnly(2026, 8, 11), selected);
    }
}
