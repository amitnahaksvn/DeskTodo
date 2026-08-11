using DeskTodo.App.ViewModels;
using DeskTodo.Application.DTOs;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class AnalyticsViewModelTests
{
    private readonly Mock<IAnalyticsService> _analyticsService = new();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero));
    private readonly AnalyticsViewModel _sut;

    public AnalyticsViewModelTests()
    {
        _sut = new AnalyticsViewModel(_analyticsService.Object, _timeProvider, NullLogger<AnalyticsViewModel>.Instance);
        _analyticsService.Setup(s => s.GetSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AnalyticsSummary
        {
            WeeklyCompletionRate = 50,
            MonthlyCompletionRate = 60,
            OverallCompletionRate = 70,
            CurrentStreakDays = 3,
            FocusMinutesThisWeek = 45,
            FocusMinutesAllTime = 300,
        });
        _analyticsService.Setup(s => s.GetHeatMapDataAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DailyCompletionCount>());
        _analyticsService.Setup(s => s.GetCategoryAnalyticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CategoryAnalytics>());
    }

    [Fact]
    public async Task LoadAsync_PopulatesSummaryTiles()
    {
        await _sut.LoadAsync();

        Assert.Equal(50, _sut.WeeklyCompletionRate);
        Assert.Equal(60, _sut.MonthlyCompletionRate);
        Assert.Equal(70, _sut.OverallCompletionRate);
        Assert.Equal(3, _sut.CurrentStreakDays);
        Assert.Equal(45, _sut.FocusMinutesThisWeek);
        Assert.Equal(300, _sut.FocusMinutesAllTime);
        Assert.True(_sut.IsLoaded);
    }

    [Fact]
    public async Task LoadAsync_BuildsSixTilesFromTheSummary()
    {
        await _sut.LoadAsync();

        Assert.Equal(6, _sut.Tiles.Count);
        Assert.Contains(_sut.Tiles, t => t.Label == "Weekly progress" && t.Value == "50%");
        Assert.Contains(_sut.Tiles, t => t.Label == "Current streak" && t.Value == "3 days");
    }

    [Fact]
    public async Task LoadAsync_WithASingleDayStreak_UsesSingularLabel()
    {
        _analyticsService.Setup(s => s.GetSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AnalyticsSummary
        {
            WeeklyCompletionRate = 0,
            MonthlyCompletionRate = 0,
            OverallCompletionRate = 0,
            CurrentStreakDays = 1,
            FocusMinutesThisWeek = 0,
            FocusMinutesAllTime = 0,
        });

        await _sut.LoadAsync();

        Assert.Contains(_sut.Tiles, t => t.Label == "Current streak" && t.Value == "1 day");
    }

    [Fact]
    public async Task LoadAsync_RequestsTheHeatMapForTheLast12Weeks()
    {
        await _sut.LoadAsync();

        _analyticsService.Verify(s => s.GetHeatMapDataAsync(
            new DateOnly(2026, 5, 21), // 12*7-1 = 83 days before 2026-08-12
            new DateOnly(2026, 8, 12),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_PopulatesHeatMapAndCategoryBreakdown()
    {
        var days = new[] { new DailyCompletionCount(new DateOnly(2026, 8, 12), 2, 3) };
        var categories = new[] { new CategoryAnalytics(Guid.NewGuid(), "Work", "#3B82F6", 5, 3, 20) };
        _analyticsService.Setup(s => s.GetHeatMapDataAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(days);
        _analyticsService.Setup(s => s.GetCategoryAnalyticsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categories);

        await _sut.LoadAsync();

        Assert.Single(_sut.HeatMapDays);
        Assert.Single(_sut.CategoryBreakdown);
        Assert.Equal("Work", _sut.CategoryBreakdown[0].CategoryName);
    }

    [Fact]
    public async Task GenerateWeeklyReportCommand_UsesThisWeeksSundayToSaturdayRange()
    {
        _analyticsService.Setup(s => s.GenerateReportAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Report");

        await _sut.GenerateWeeklyReportCommand.ExecuteAsync(null);

        _analyticsService.Verify(s => s.GenerateReportAsync(new DateOnly(2026, 8, 9), new DateOnly(2026, 8, 15), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("# Report", _sut.ReportText);
        Assert.False(string.IsNullOrEmpty(_sut.ReportPeriodLabel));
    }

    [Fact]
    public async Task GenerateMonthlyReportCommand_UsesTheFullCalendarMonth()
    {
        _analyticsService.Setup(s => s.GenerateReportAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Monthly");

        await _sut.GenerateMonthlyReportCommand.ExecuteAsync(null);

        _analyticsService.Verify(s => s.GenerateReportAsync(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("# Monthly", _sut.ReportText);
    }
}
