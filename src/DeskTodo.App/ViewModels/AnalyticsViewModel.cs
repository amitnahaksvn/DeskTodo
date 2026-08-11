using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.DTOs;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Dashboard (Phase 24) — summary tiles, a GitHub-style Heat Map of the last 12
/// weeks' completion density, a per-category breakdown, and a generated Weekly/Monthly
/// Report. Purely a reader: every value here comes from <see cref="IAnalyticsService"/>'s
/// read-only aggregation, nothing on this screen writes anything back.
/// </summary>
public sealed partial class AnalyticsViewModel(IAnalyticsService analyticsService, TimeProvider timeProvider, ILogger<AnalyticsViewModel> logger) : ViewModelBase
{
    /// <summary>How far back the Heat Map reaches — 12 weeks reads as "about a season," the same span GitHub's own contribution graph defaults to on a narrow layout.</summary>
    private const int HeatMapWeeks = 12;

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Tiles))]
    public partial double WeeklyCompletionRate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Tiles))]
    public partial double MonthlyCompletionRate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Tiles))]
    public partial double OverallCompletionRate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Tiles))]
    public partial int CurrentStreakDays { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Tiles))]
    public partial int FocusMinutesThisWeek { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Tiles))]
    public partial int FocusMinutesAllTime { get; set; }

    /// <summary>The summary values above, packaged for the Dashboard's tile <c>ItemsControl</c> — a plain list of (label, value) pairs is simpler to lay out in a <c>WrapPanel</c> than six individually-positioned <c>Border</c>s in XAML.</summary>
    public IReadOnlyList<AnalyticsTile> Tiles =>
    [
        new AnalyticsTile("Weekly progress", $"{WeeklyCompletionRate:F0}%"),
        new AnalyticsTile("Monthly progress", $"{MonthlyCompletionRate:F0}%"),
        new AnalyticsTile("Overall completion", $"{OverallCompletionRate:F0}%"),
        new AnalyticsTile("Current streak", CurrentStreakDays == 1 ? "1 day" : $"{CurrentStreakDays} days"),
        new AnalyticsTile("Focus time this week", $"{FocusMinutesThisWeek}m"),
        new AnalyticsTile("Focus time all-time", $"{FocusMinutesAllTime}m"),
    ];

    public ObservableCollection<DailyCompletionCount> HeatMapDays { get; } = [];

    public ObservableCollection<CategoryAnalytics> CategoryBreakdown { get; } = [];

    [ObservableProperty]
    public partial string ReportText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReportPeriodLabel { get; set; } = string.Empty;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await analyticsService.GetSummaryAsync(cancellationToken);
            WeeklyCompletionRate = summary.WeeklyCompletionRate;
            MonthlyCompletionRate = summary.MonthlyCompletionRate;
            OverallCompletionRate = summary.OverallCompletionRate;
            CurrentStreakDays = summary.CurrentStreakDays;
            FocusMinutesThisWeek = summary.FocusMinutesThisWeek;
            FocusMinutesAllTime = summary.FocusMinutesAllTime;

            var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
            var heatMapStart = today.AddDays(-(HeatMapWeeks * 7 - 1));
            var heatMapDays = await analyticsService.GetHeatMapDataAsync(heatMapStart, today, cancellationToken);
            HeatMapDays.Clear();
            foreach (var day in heatMapDays)
            {
                HeatMapDays.Add(day);
            }

            var categories = await analyticsService.GetCategoryAnalyticsAsync(cancellationToken);
            CategoryBreakdown.Clear();
            foreach (var category in categories)
            {
                CategoryBreakdown.Add(category);
            }

            IsLoaded = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load analytics");
        }
    }

    [RelayCommand]
    private async Task GenerateWeeklyReportAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        await GenerateReportForPeriodAsync(weekStart, weekStart.AddDays(6), cancellationToken);
    }

    [RelayCommand]
    private async Task GenerateMonthlyReportAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        await GenerateReportForPeriodAsync(monthStart, monthStart.AddMonths(1).AddDays(-1), cancellationToken);
    }

    private async Task GenerateReportForPeriodAsync(DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken)
    {
        try
        {
            ReportText = await analyticsService.GenerateReportAsync(periodStart, periodEnd, cancellationToken);
            ReportPeriodLabel = $"{periodStart:MMM d} – {periodEnd:MMM d, yyyy}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate report for {Start} to {End}", periodStart, periodEnd);
        }
    }
}
