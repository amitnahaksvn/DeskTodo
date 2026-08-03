using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class YearViewModelTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero));
    private readonly YearViewModel _sut;

    public YearViewModelTests()
    {
        _sut = new YearViewModel(_taskService.Object, _timeProvider, NullLogger<YearViewModel>.Instance);
    }

    [Fact]
    public async Task LoadAsync_WithNoInitialDate_UsesTheCurrentYear_AndProducesTwelveMonths()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.LoadAsync();

        Assert.Equal(2026, _sut.Year);
        Assert.Equal(12, _sut.Months.Count);
        Assert.Equal("January", _sut.Months[0].MonthName);
        Assert.Equal("December", _sut.Months[11].MonthName);
    }

    [Fact]
    public async Task LoadAsync_ComputesTaskCountsPerMonth_IgnoringOtherYears()
    {
        var tasks = new[]
        {
            new TaskItem { PlanDate = new DateOnly(2026, 3, 5), Title = "March task" },
            new TaskItem { PlanDate = new DateOnly(2025, 3, 5), Title = "Last year's March task" },
        };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tasks);

        await _sut.LoadAsync();

        Assert.Equal(1, _sut.Months[2].TotalCount); // March
        Assert.Equal(0, _sut.Months[0].TotalCount); // January
    }

    [Fact]
    public async Task LoadAsync_MarksOnlyTheCurrentMonthAsCurrent()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.LoadAsync();

        Assert.True(_sut.Months[7].IsCurrentMonth); // August
        Assert.All(_sut.Months.Where(m => m.MonthName != "August"), m => Assert.False(m.IsCurrentMonth));
    }

    [Fact]
    public async Task PreviousYearAsync_NextYearAsync_NavigateAYearAtATime()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        await _sut.LoadAsync();

        await _sut.PreviousYearCommand.ExecuteAsync(null);
        Assert.Equal(2025, _sut.Year);

        await _sut.NextYearCommand.ExecuteAsync(null);
        await _sut.NextYearCommand.ExecuteAsync(null);
        Assert.Equal(2027, _sut.Year);
    }

    [Fact]
    public async Task GoToCurrentYearAsync_ReturnsToTodaysYear()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        await _sut.LoadAsync(new DateOnly(2020, 1, 1));

        await _sut.GoToCurrentYearCommand.ExecuteAsync(null);

        Assert.Equal(2026, _sut.Year);
    }

    [Fact]
    public async Task ClickingAMonthTile_RaisesDateSelectedWithTheFirstOfThatMonth()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        await _sut.LoadAsync();
        DateOnly? selected = null;
        _sut.DateSelected += (_, date) => selected = date;

        _sut.Months[4].SelectCommand.Execute(null); // May

        Assert.Equal(new DateOnly(2026, 5, 1), selected);
    }
}
