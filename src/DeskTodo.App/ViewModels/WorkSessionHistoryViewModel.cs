using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Work Session History window (Feature 65, Roadmap-39-100.md). The spec's own note
/// flags that <see cref="Domain.Entities.FocusSession"/> (Phase 23) already persists every
/// session — this is a reporting/UI layer over that existing data, not a parallel entity: no
/// new persistence, just a session list plus Today/This week totals computed from it.
/// </summary>
public sealed partial class WorkSessionHistoryViewModel(IFocusSessionService focusSessionService, TimeProvider timeProvider, ILogger<WorkSessionHistoryViewModel> logger) : ViewModelBase
{
    public ObservableCollection<WorkSessionOption> Sessions { get; } = [];

    [ObservableProperty]
    public partial string TodayTotalDisplay { get; set; } = "0m";

    [ObservableProperty]
    public partial string ThisWeekTotalDisplay { get; set; } = "0m";

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = await focusSessionService.GetAllSessionsAsync(cancellationToken);

            Sessions.Clear();
            foreach (var session in sessions)
            {
                Sessions.Add(new WorkSessionOption(
                    session.Task?.Title ?? "(no task)",
                    session.Type.ToString(),
                    session.DurationMinutes,
                    session.StartedAt.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt")));
            }

            var now = timeProvider.GetLocalNow().DateTime;
            var today = now.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

            var todayTotal = sessions.Where(s => s.StartedAt.ToLocalTime().Date == today).Sum(s => s.DurationMinutes);
            var weekTotal = sessions.Where(s => s.StartedAt.ToLocalTime().Date >= startOfWeek).Sum(s => s.DurationMinutes);

            TodayTotalDisplay = FormatMinutes(todayTotal);
            ThisWeekTotalDisplay = FormatMinutes(weekTotal);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load work session history");
        }
    }

    private static string FormatMinutes(int minutes) => minutes >= 60 ? $"{minutes / 60}h {minutes % 60}m" : $"{minutes}m";
}
