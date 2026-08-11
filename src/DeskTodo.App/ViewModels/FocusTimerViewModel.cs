using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Phase 23's session timer engine — backs Pomodoro, Stopwatch, and Countdown Timer (which
/// "Focus Timer"/"Focus Mode"/"Deep Work Session" from the original wishlist are all just
/// different default lengths of, see <see cref="FocusSessionType"/>'s own doc comment).
///
/// Registered as a DI singleton, not transient like other window ViewModels — a running
/// timer is app-wide state, not something scoped to one window's lifetime: it needs to keep
/// ticking (and be reflected in the widget header's indicator) whether or not
/// <c>FocusTimerWindow</c> is currently open. <c>WidgetWindow</c> binds its indicator
/// directly to this same instance rather than through <see cref="WidgetViewModel"/>, so
/// opening the timer window doesn't require threading a new constructor dependency through
/// every existing <c>WidgetViewModel</c> test.
///
/// A Pomodoro's break phase is never logged as a <see cref="Domain.Entities.FocusSession"/> —
/// only completed (or manually stopped, past one full minute) work time counts as time to
/// credit toward a linked task's <see cref="Domain.Entities.TaskItem.ActualMinutes"/>.
///
/// <c>OnTick</c> is internal (via <c>InternalsVisibleTo</c>, same as
/// <c>WidgetViewModel.OnDayRolloverTick</c>) so tests can call it directly to simulate ticks
/// deterministically instead of waiting on a real <see cref="DispatcherTimer"/>.
/// </summary>
public sealed partial class FocusTimerViewModel : ViewModelBase, IDisposable
{
    private readonly IFocusSessionService _focusSessionService;
    private readonly ITaskService _taskService;
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FocusTimerViewModel> _logger;
    private readonly DispatcherTimer _tickTimer;

    private int _pomodoroWorkSeconds = 25 * 60;
    private int _pomodoroBreakSeconds = 5 * 60;
    private int _plannedPhaseSeconds;
    private Guid? _activeTaskId;
    private DateTime _phaseStartedAtUtc;

    public FocusTimerViewModel(
        IFocusSessionService focusSessionService,
        ITaskService taskService,
        ISettingsService settingsService,
        INotificationService notificationService,
        TimeProvider timeProvider,
        ILogger<FocusTimerViewModel> logger)
    {
        _focusSessionService = focusSessionService;
        _taskService = taskService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
        _logger = logger;

        _tickTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _tickTimer.Tick += OnTick;
    }

    public IReadOnlyList<FocusSessionType> Types { get; } = Enum.GetValues<FocusSessionType>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCountdownType))]
    [NotifyPropertyChangedFor(nameof(IsPomodoroType))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    public partial FocusSessionType SelectedType { get; set; } = FocusSessionType.CountdownTimer;

    /// <summary>Drives the duration picker's visibility — only <see cref="FocusSessionType.CountdownTimer"/> has a user-chosen length; Pomodoro's comes from Settings, Stopwatch has none.</summary>
    public bool IsCountdownType => SelectedType == FocusSessionType.CountdownTimer;

    /// <summary>Drives the "Round N" label's visibility.</summary>
    public bool IsPomodoroType => SelectedType == FocusSessionType.Pomodoro;

    // decimal?, not int, to bind directly to NumericUpDown.Value — same reasoning as
    // TaskEditViewModel.EstimatedMinutes.
    [ObservableProperty]
    public partial decimal? SelectedDurationMinutes { get; set; } = 25m;

    public ObservableCollection<TaskOption> Tasks { get; } = [TaskOption.None];

    [ObservableProperty]
    public partial TaskOption SelectedTask { get; set; } = TaskOption.None;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPause))]
    [NotifyPropertyChangedFor(nameof(CanResume))]
    public partial bool IsRunning { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPause))]
    [NotifyPropertyChangedFor(nameof(CanResume))]
    public partial bool IsPaused { get; set; }

    public bool CanPause => IsRunning && !IsPaused;

    public bool CanResume => IsRunning && IsPaused;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    public partial bool IsBreak { get; set; }

    [ObservableProperty]
    public partial int RoundNumber { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    public partial int RemainingSeconds { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    public partial int ElapsedSeconds { get; set; }

    public string DisplayText => FormatSeconds(SelectedType == FocusSessionType.Stopwatch ? ElapsedSeconds : RemainingSeconds);

    public string StatusText => SelectedType switch
    {
        FocusSessionType.Pomodoro => IsBreak ? "Break" : "Work",
        FocusSessionType.Stopwatch => "Stopwatch",
        _ => "Focus Timer",
    };

    private static string FormatSeconds(int totalSeconds)
    {
        var clamped = Math.Max(0, totalSeconds);
        return $"{clamped / 60:D2}:{clamped % 60:D2}";
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            _pomodoroWorkSeconds = settings.PomodoroWorkMinutes * 60;
            _pomodoroBreakSeconds = settings.PomodoroBreakMinutes * 60;

            var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
            var tasks = await _taskService.GetTasksForDateAsync(today, cancellationToken);

            var previouslySelectedId = SelectedTask.Id;
            Tasks.Clear();
            Tasks.Add(TaskOption.None);
            foreach (var task in tasks.Where(t => !t.IsCompleted).OrderBy(t => t.DayOrder))
            {
                Tasks.Add(new TaskOption(task.Id, task.Title));
            }

            SelectedTask = Tasks.FirstOrDefault(t => t.Id == previouslySelectedId) ?? TaskOption.None;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Focus Timer's task list/settings");
        }
    }

    /// <summary>Preselects a task before showing the window — e.g. from the full-field editor's "Start Timer" button. A no-op while a session is already running, since <see cref="Start"/> captures the linked task at the moment it's pressed, not read live off <see cref="SelectedTask"/>.</summary>
    public void PreselectTask(Guid taskId, string title)
    {
        if (IsRunning)
        {
            return;
        }

        var option = new TaskOption(taskId, title);
        if (!Tasks.Contains(option))
        {
            Tasks.Add(option);
        }

        SelectedTask = option;
    }

    [RelayCommand]
    private void SelectDurationPreset(string minutesText)
    {
        if (decimal.TryParse(minutesText, out var minutes))
        {
            SelectedDurationMinutes = minutes;
        }
    }

    [RelayCommand]
    private void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _activeTaskId = SelectedTask.Id;
        RoundNumber = 1;
        IsBreak = false;

        _plannedPhaseSeconds = SelectedType switch
        {
            FocusSessionType.Pomodoro => _pomodoroWorkSeconds,
            FocusSessionType.CountdownTimer => Math.Max(1, (int)(SelectedDurationMinutes ?? 25m)) * 60,
            _ => 0,
        };
        RemainingSeconds = _plannedPhaseSeconds;
        ElapsedSeconds = 0;
        _phaseStartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        IsRunning = true;
        IsPaused = false;
        _tickTimer.Start();
    }

    [RelayCommand]
    private void Pause()
    {
        if (!IsRunning || IsPaused)
        {
            return;
        }

        _tickTimer.Stop();
        IsPaused = true;
    }

    [RelayCommand]
    private void Resume()
    {
        if (!IsRunning || !IsPaused)
        {
            return;
        }

        _tickTimer.Start();
        IsPaused = false;
    }

    [RelayCommand]
    private async Task StopAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        _tickTimer.Stop();

        if (!IsBreak)
        {
            var elapsedSeconds = SelectedType == FocusSessionType.Stopwatch ? ElapsedSeconds : _plannedPhaseSeconds - RemainingSeconds;
            await LogIfSubstantialAsync(elapsedSeconds);
        }

        IsRunning = false;
        IsPaused = false;
        IsBreak = false;
        RemainingSeconds = 0;
        ElapsedSeconds = 0;
    }

    internal void OnTick(object? sender, EventArgs e)
    {
        if (SelectedType == FocusSessionType.Stopwatch)
        {
            ElapsedSeconds++;
            return;
        }

        RemainingSeconds--;
        if (RemainingSeconds > 0)
        {
            return;
        }

        if (SelectedType == FocusSessionType.Pomodoro)
        {
            _ = HandlePomodoroPhaseCompleteAsync();
        }
        else
        {
            _ = HandleCountdownCompleteAsync();
        }
    }

    private async Task HandlePomodoroPhaseCompleteAsync()
    {
        if (!IsBreak)
        {
            await LogIfSubstantialAsync(_pomodoroWorkSeconds);
            await NotifyAsync("Pomodoro", "Work session complete — take a break.");
            IsBreak = true;
            _plannedPhaseSeconds = _pomodoroBreakSeconds;
            RemainingSeconds = _pomodoroBreakSeconds;
        }
        else
        {
            await NotifyAsync("Pomodoro", "Break's over — back to work.");
            IsBreak = false;
            RoundNumber++;
            _plannedPhaseSeconds = _pomodoroWorkSeconds;
            RemainingSeconds = _pomodoroWorkSeconds;
            _phaseStartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        }
    }

    private async Task HandleCountdownCompleteAsync()
    {
        await LogIfSubstantialAsync(_plannedPhaseSeconds);
        await NotifyAsync("Focus Timer", "Time's up.");

        _tickTimer.Stop();
        IsRunning = false;
        IsPaused = false;
        RemainingSeconds = 0;
    }

    /// <summary>Sessions under a minute aren't logged — an accidental Start-then-immediately-Stop shouldn't leave a stray zero-minute row in a task's time-tracking history.</summary>
    private async Task LogIfSubstantialAsync(int elapsedSeconds)
    {
        var minutes = elapsedSeconds / 60;
        if (minutes < 1)
        {
            return;
        }

        try
        {
            var endedAt = _timeProvider.GetUtcNow().UtcDateTime;
            await _focusSessionService.CompleteSessionAsync(SelectedType, _phaseStartedAtUtc, endedAt, minutes, _activeTaskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log completed focus session");
        }
    }

    private async Task NotifyAsync(string title, string message)
    {
        try
        {
            await _notificationService.NotifyAsync(title, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Focus Timer notification");
        }
    }

    public void Dispose()
    {
        _tickTimer.Stop();
        _tickTimer.Tick -= OnTick;
    }
}
