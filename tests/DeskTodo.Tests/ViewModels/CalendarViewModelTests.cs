using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class CalendarViewModelTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero));
    private readonly CalendarViewModel _sut;

    public CalendarViewModelTests()
    {
        _sut = new CalendarViewModel(_taskService.Object, _timeProvider, NullLogger<CalendarViewModel>.Instance);
    }

    private static TaskItem CreateTask(DateOnly planDate, bool completed = false)
    {
        var task = new TaskItem { PlanDate = planDate, Title = "Task" };
        if (completed)
        {
            task.Complete();
        }

        return task;
    }

    [Fact]
    public async Task LoadAsync_WithNoInitialMonth_UsesTheCurrentMonth()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.LoadAsync();

        Assert.Equal(new DateOnly(2026, 8, 1), _sut.DisplayedMonth);
        Assert.Equal("August 2026", _sut.MonthTitle);
    }

    [Fact]
    public async Task LoadAsync_AlwaysProducesExactly42Days()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.LoadAsync(new DateOnly(2026, 2, 1));

        Assert.Equal(42, _sut.Days.Count);
    }

    [Fact]
    public async Task LoadAsync_MarksDaysOutsideTheDisplayedMonthAsNotCurrentMonth()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.LoadAsync(new DateOnly(2026, 8, 1));

        Assert.All(_sut.Days.Where(d => d.Date.Month == 8 && d.Date.Year == 2026), d => Assert.True(d.IsCurrentMonth));
        Assert.Contains(_sut.Days, d => !d.IsCurrentMonth);
    }

    [Fact]
    public async Task LoadAsync_ComputesTaskCountsPerDay()
    {
        var tasks = new[]
        {
            CreateTask(new DateOnly(2026, 8, 10), completed: true),
            CreateTask(new DateOnly(2026, 8, 10), completed: false),
            CreateTask(new DateOnly(2026, 8, 12), completed: false),
        };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tasks);

        await _sut.LoadAsync(new DateOnly(2026, 8, 1));

        var aug10 = _sut.Days.Single(d => d.Date == new DateOnly(2026, 8, 10));
        Assert.Equal(2, aug10.TotalCount);
        Assert.Equal(1, aug10.CompletedCount);
        Assert.Equal("1/2", aug10.CountDisplay);

        var aug11 = _sut.Days.Single(d => d.Date == new DateOnly(2026, 8, 11));
        Assert.Equal(0, aug11.TotalCount);
        Assert.Equal("—", aug11.CountDisplay);
        Assert.False(aug11.HasTasks);
    }

    [Fact]
    public async Task LoadAsync_ExcludesArchivedTasksFromCounts()
    {
        var archived = CreateTask(new DateOnly(2026, 8, 10));
        archived.Archive();
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([archived]);

        await _sut.LoadAsync(new DateOnly(2026, 8, 1));

        var aug10 = _sut.Days.Single(d => d.Date == new DateOnly(2026, 8, 10));
        Assert.Equal(0, aug10.TotalCount);
    }

    [Fact]
    public async Task LoadAsync_MarksTodaysCellAsToday()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.LoadAsync(new DateOnly(2026, 8, 1));

        var today = _sut.Days.Single(d => d.Date == new DateOnly(2026, 8, 15));
        Assert.True(today.IsToday);
        Assert.All(_sut.Days.Where(d => d.Date != new DateOnly(2026, 8, 15)), d => Assert.False(d.IsToday));
    }

    [Fact]
    public async Task PreviousMonthAsync_NextMonthAsync_NavigateAMonthAtATime()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        await _sut.LoadAsync(new DateOnly(2026, 8, 1));

        await _sut.PreviousMonthCommand.ExecuteAsync(null);
        Assert.Equal(new DateOnly(2026, 7, 1), _sut.DisplayedMonth);

        await _sut.NextMonthCommand.ExecuteAsync(null);
        await _sut.NextMonthCommand.ExecuteAsync(null);
        Assert.Equal(new DateOnly(2026, 9, 1), _sut.DisplayedMonth);
    }

    [Fact]
    public async Task GoToCurrentMonthAsync_ReturnsToTodaysMonth()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        await _sut.LoadAsync(new DateOnly(2026, 1, 1));

        await _sut.GoToCurrentMonthCommand.ExecuteAsync(null);

        Assert.Equal(new DateOnly(2026, 8, 1), _sut.DisplayedMonth);
    }

    [Fact]
    public async Task ClickingADayCell_RaisesDateSelected()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        await _sut.LoadAsync(new DateOnly(2026, 8, 1));
        DateOnly? selected = null;
        _sut.DateSelected += (_, date) => selected = date;

        _sut.Days.Single(d => d.Date == new DateOnly(2026, 8, 20)).SelectCommand.Execute(null);

        Assert.Equal(new DateOnly(2026, 8, 20), selected);
    }

    [Fact]
    public void CloseCommand_RaisesCloseRequested()
    {
        var raised = false;
        _sut.CloseRequested += (_, _) => raised = true;

        _sut.CloseCommand.Execute(null);

        Assert.True(raised);
    }
}
