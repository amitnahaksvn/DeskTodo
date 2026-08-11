using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Application.Settings;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

/// <summary>
/// Drives the timer by calling the internal <see cref="FocusTimerViewModel.OnTick"/> directly
/// (same pattern as <c>WidgetViewModelTests</c>' <c>OnDayRolloverTick</c> calls) rather than
/// waiting on the real 1-second <c>DispatcherTimer</c> — deterministic and instant.
/// </summary>
public class FocusTimerViewModelTests
{
    private readonly Mock<IFocusSessionService> _focusSessionService = new();
    private readonly Mock<ITaskService> _taskService = new();
    private readonly Mock<ISettingsService> _settingsService = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly FocusTimerViewModel _sut;

    public FocusTimerViewModelTests()
    {
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => new AppSettings());
        _taskService.Setup(s => s.GetTasksForDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TaskItem>());
        _sut = new FocusTimerViewModel(_focusSessionService.Object, _taskService.Object, _settingsService.Object, _notificationService.Object, TimeProvider.System, NullLogger<FocusTimerViewModel>.Instance);
    }

    [Fact]
    public void StartCommand_WithCountdownTimer_SetsRemainingSecondsFromSelectedDuration()
    {
        _sut.SelectedType = FocusSessionType.CountdownTimer;
        _sut.SelectedDurationMinutes = 10m;

        _sut.StartCommand.Execute(null);

        Assert.True(_sut.IsRunning);
        Assert.Equal(600, _sut.RemainingSeconds);
        Assert.Equal("10:00", _sut.DisplayText);
    }

    [Fact]
    public void OnTick_WithCountdownTimer_DecrementsRemainingSeconds()
    {
        _sut.SelectedType = FocusSessionType.CountdownTimer;
        _sut.SelectedDurationMinutes = 1m;
        _sut.StartCommand.Execute(null);

        _sut.OnTick(null, EventArgs.Empty);
        _sut.OnTick(null, EventArgs.Empty);

        Assert.Equal(58, _sut.RemainingSeconds);
    }

    [Fact]
    public async Task OnTick_WhenCountdownReachesZero_LogsTheSession_NotifiesAndStops()
    {
        _sut.SelectedType = FocusSessionType.CountdownTimer;
        _sut.SelectedDurationMinutes = 1m;
        _sut.StartCommand.Execute(null);

        for (var i = 0; i < 60; i++)
        {
            _sut.OnTick(null, EventArgs.Empty);
        }

        // The completion path is fire-and-forget (_ = HandleCountdownCompleteAsync()) —
        // give it a beat to actually run before asserting.
        await Task.Delay(50);

        Assert.False(_sut.IsRunning);
        _focusSessionService.Verify(s => s.CompleteSessionAsync(FocusSessionType.CountdownTimer, It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, null, It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(n => n.NotifyAsync("Focus Timer", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_BeforeAFullMinuteElapses_DoesNotLogASession()
    {
        _sut.SelectedType = FocusSessionType.CountdownTimer;
        _sut.SelectedDurationMinutes = 5m;
        _sut.StartCommand.Execute(null);
        _sut.OnTick(null, EventArgs.Empty); // 1 second elapsed — under a minute

        await _sut.StopCommand.ExecuteAsync(null);

        Assert.False(_sut.IsRunning);
        _focusSessionService.Verify(s => s.CompleteSessionAsync(It.IsAny<FocusSessionType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StopAsync_AfterAFewMinutesElapse_LogsThePartialSession()
    {
        _sut.SelectedType = FocusSessionType.CountdownTimer;
        _sut.SelectedDurationMinutes = 10m;
        _sut.StartCommand.Execute(null);
        for (var i = 0; i < 180; i++) // 3 minutes
        {
            _sut.OnTick(null, EventArgs.Empty);
        }

        await _sut.StopCommand.ExecuteAsync(null);

        _focusSessionService.Verify(s => s.CompleteSessionAsync(FocusSessionType.CountdownTimer, It.IsAny<DateTime>(), It.IsAny<DateTime>(), 3, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_WithATaskSelected_LogsTheSessionAgainstThatTask()
    {
        var task = new TaskOption(Guid.NewGuid(), "Write report");
        _sut.Tasks.Add(task);
        _sut.SelectedTask = task;
        _sut.SelectedType = FocusSessionType.Stopwatch;
        _sut.StartCommand.Execute(null);
        for (var i = 0; i < 90; i++)
        {
            _sut.OnTick(null, EventArgs.Empty);
        }

        await _sut.StopCommand.ExecuteAsync(null);

        _focusSessionService.Verify(s => s.CompleteSessionAsync(FocusSessionType.Stopwatch, It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, task.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void OnTick_WithStopwatch_IncrementsElapsedSecondsIndefinitely()
    {
        _sut.SelectedType = FocusSessionType.Stopwatch;
        _sut.StartCommand.Execute(null);

        for (var i = 0; i < 125; i++)
        {
            _sut.OnTick(null, EventArgs.Empty);
        }

        Assert.Equal(125, _sut.ElapsedSeconds);
        Assert.Equal("02:05", _sut.DisplayText);
        Assert.True(_sut.IsRunning); // Stopwatch never auto-stops.
    }

    [Fact]
    public async Task OnTick_PomodoroWorkPhaseComplete_LogsTheWorkSession_AndSwitchesToBreakWithoutStopping()
    {
        var settings = new AppSettings { PomodoroWorkMinutes = 1, PomodoroBreakMinutes = 1 };
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        await _sut.LoadAsync();
        _sut.SelectedType = FocusSessionType.Pomodoro;
        _sut.StartCommand.Execute(null);

        for (var i = 0; i < 60; i++)
        {
            _sut.OnTick(null, EventArgs.Empty);
        }
        await Task.Delay(50);

        Assert.True(_sut.IsRunning); // Pomodoro auto-continues into the break, doesn't stop.
        Assert.True(_sut.IsBreak);
        Assert.Equal(60, _sut.RemainingSeconds); // Break phase just started.
        _focusSessionService.Verify(s => s.CompleteSessionAsync(FocusSessionType.Pomodoro, It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(n => n.NotifyAsync("Pomodoro", It.Is<string>(m => m.Contains("break")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnTick_PomodoroBreakPhaseComplete_DoesNotLogASession_AndIncrementsRoundNumber()
    {
        var settings = new AppSettings { PomodoroWorkMinutes = 1, PomodoroBreakMinutes = 1 };
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        await _sut.LoadAsync();
        _sut.SelectedType = FocusSessionType.Pomodoro;
        _sut.StartCommand.Execute(null);
        for (var i = 0; i < 60; i++) // finish work phase
        {
            _sut.OnTick(null, EventArgs.Empty);
        }
        await Task.Delay(50);

        for (var i = 0; i < 60; i++) // finish break phase
        {
            _sut.OnTick(null, EventArgs.Empty);
        }
        await Task.Delay(50);

        Assert.False(_sut.IsBreak);
        Assert.Equal(2, _sut.RoundNumber);
        // Exactly one CompleteSessionAsync call total — from the work phase only, not the break.
        _focusSessionService.Verify(s => s.CompleteSessionAsync(It.IsAny<FocusSessionType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_DuringABreak_DoesNotLogASession()
    {
        var settings = new AppSettings { PomodoroWorkMinutes = 1, PomodoroBreakMinutes = 5 };
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        await _sut.LoadAsync();
        _sut.SelectedType = FocusSessionType.Pomodoro;
        _sut.StartCommand.Execute(null);
        for (var i = 0; i < 60; i++) // finish work phase, enter break
        {
            _sut.OnTick(null, EventArgs.Empty);
        }
        await Task.Delay(50);
        _focusSessionService.Invocations.Clear();

        await _sut.StopCommand.ExecuteAsync(null);

        _focusSessionService.Verify(s => s.CompleteSessionAsync(It.IsAny<FocusSessionType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void PauseThenResume_TogglesIsPausedAndCanPauseCanResume()
    {
        _sut.SelectedType = FocusSessionType.Stopwatch;
        _sut.StartCommand.Execute(null);
        Assert.True(_sut.CanPause);
        Assert.False(_sut.CanResume);

        _sut.PauseCommand.Execute(null);

        Assert.True(_sut.IsPaused);
        Assert.False(_sut.CanPause);
        Assert.True(_sut.CanResume);

        _sut.ResumeCommand.Execute(null);

        Assert.False(_sut.IsPaused);
        Assert.True(_sut.CanPause);
    }

    [Fact]
    public void StartCommand_WhileAlreadyRunning_IsANoOp()
    {
        _sut.SelectedType = FocusSessionType.Stopwatch;
        _sut.StartCommand.Execute(null);
        _sut.OnTick(null, EventArgs.Empty);

        _sut.StartCommand.Execute(null); // shouldn't reset ElapsedSeconds back to 0

        Assert.Equal(1, _sut.ElapsedSeconds);
    }

    [Fact]
    public async Task LoadAsync_PopulatesTasksFromToday_ExcludingCompletedOnes()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var incomplete = new TaskItem { PlanDate = today, Title = "Write report", DayOrder = 0 };
        var completed = new TaskItem { PlanDate = today, Title = "Done already", DayOrder = 1 };
        completed.Complete();
        _taskService.Setup(s => s.GetTasksForDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([incomplete, completed]);

        await _sut.LoadAsync();

        Assert.Equal(["None", "Write report"], _sut.Tasks.Select(t => t.Title));
    }

    [Fact]
    public void PreselectTask_SetsSelectedTask()
    {
        var taskId = Guid.NewGuid();

        _sut.PreselectTask(taskId, "Write report");

        Assert.Equal(taskId, _sut.SelectedTask.Id);
        Assert.Equal("Write report", _sut.SelectedTask.Title);
    }

    [Fact]
    public void PreselectTask_WhileRunning_IsANoOp()
    {
        _sut.SelectedType = FocusSessionType.Stopwatch;
        _sut.StartCommand.Execute(null);
        var originalSelection = _sut.SelectedTask;

        _sut.PreselectTask(Guid.NewGuid(), "Should not apply");

        Assert.Equal(originalSelection, _sut.SelectedTask);
    }

    [Fact]
    public void SelectDurationPresetCommand_SetsSelectedDurationMinutes()
    {
        _sut.SelectDurationPresetCommand.Execute("50");

        Assert.Equal(50m, _sut.SelectedDurationMinutes);
    }
}
