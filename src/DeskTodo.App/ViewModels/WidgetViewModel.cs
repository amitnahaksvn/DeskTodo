using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backing view model for the always-visible desktop widget: today's date
/// and today's task list, with live completion progress, per-task CRUD
/// (create/rename/pin/archive/delete/duplicate — see <see cref="TaskItemViewModel"/>
/// for the per-row operations), the full-field editor hand-off, and
/// drag-to-reorder. Day navigation (previous/next/calendar) lands in a
/// later phase.
/// </summary>
public sealed partial class WidgetViewModel : ViewModelBase, IDisposable
{
    private readonly ITaskService _taskService;
    private readonly ILogger<WidgetViewModel> _logger;
    private readonly ILogger<TaskItemViewModel> _taskItemLogger;
    private readonly DispatcherTimer _dayRolloverTimer;

    public WidgetViewModel(ITaskService taskService, ILogger<WidgetViewModel> logger, ILogger<TaskItemViewModel> taskItemLogger)
    {
        _taskService = taskService;
        _logger = logger;
        _taskItemLogger = taskItemLogger;

        PlanDate = DateOnly.FromDateTime(DateTime.Now);

        // Polls rather than scheduling a single timer for exactly midnight so a
        // sleeping/suspended machine still catches the rollover soon after waking.
        _dayRolloverTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _dayRolloverTimer.Tick += OnDayRolloverTick;
        _dayRolloverTimer.Start();

        // Deliberately not "_ = LoadTasksAsync();" here: constructors kicking off
        // fire-and-forget async work make callers (including tests) race against it.
        // WidgetWindow triggers the initial load explicitly on Opened instead.
    }

    public DateOnly PlanDate { get; private set; }

    public string DayOfWeekText => PlanDate.ToDateTime(TimeOnly.MinValue).ToString("dddd", CultureInfo.CurrentCulture);

    public string DateText => PlanDate.ToDateTime(TimeOnly.MinValue).ToString("d MMMM yyyy", CultureInfo.CurrentCulture);

    public ObservableCollection<TaskItemViewModel> Tasks { get; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressPercentage))]
    [NotifyPropertyChangedFor(nameof(ProgressSummaryText))]
    public partial int CompletedCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressPercentage))]
    [NotifyPropertyChangedFor(nameof(ProgressSummaryText))]
    public partial int TotalCount { get; set; }

    public int ProgressPercentage => TotalCount == 0 ? 0 : (int)Math.Round(CompletedCount * 100.0 / TotalCount);

    public string ProgressSummaryText => $"{CompletedCount} / {TotalCount} Completed";

    public bool HasNoTasks => !IsLoading && TotalCount == 0;

    /// <summary>
    /// Raised when a row's context menu "Edit" is chosen. WidgetWindow owns
    /// actually showing the editor window — a ViewModel shouldn't construct
    /// Views — so this just bubbles the request up.
    /// </summary>
    public event EventHandler<Guid>? TaskEditRequested;

    [ObservableProperty]
    public partial string NewTaskTitle { get; set; } = string.Empty;

    /// <summary>Bound to the "add task" row's Enter key. A blank title is a no-op rather than an error.</summary>
    [RelayCommand]
    private async Task AddTaskAsync()
    {
        var title = NewTaskTitle.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        try
        {
            await _taskService.CreateTaskAsync(PlanDate, title);
            NewTaskTitle = string.Empty;
            await LoadTasksAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add task '{Title}' for {PlanDate}", title, PlanDate);
        }
    }

    public async Task LoadTasksAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        OnPropertyChanged(nameof(HasNoTasks));

        try
        {
            var tasks = await _taskService.GetTasksForDateAsync(PlanDate, cancellationToken);

            foreach (var existing in Tasks)
            {
                existing.PropertyChanged -= OnTaskItemPropertyChanged;
            }

            Tasks.Clear();
            foreach (var task in tasks)
            {
                var itemViewModel = new TaskItemViewModel(task, _taskService, _taskItemLogger, () => _ = LoadTasksAsync(), id => TaskEditRequested?.Invoke(this, id));
                itemViewModel.PropertyChanged += OnTaskItemPropertyChanged;
                Tasks.Add(itemViewModel);
            }

            UpdateProgress();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tasks for {PlanDate}", PlanDate);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoTasks));
        }
    }

    private void OnTaskItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TaskItemViewModel.IsCompleted))
        {
            UpdateProgress();
        }
    }

    private void UpdateProgress()
    {
        TotalCount = Tasks.Count;
        CompletedCount = Tasks.Count(t => t.IsCompleted);
    }

    /// <summary>
    /// Moves <paramref name="draggedTaskId"/> to sit at <paramref name="targetTaskId"/>'s
    /// current position and persists the resulting order. Called from the view's
    /// drag-drop handlers (see <c>WidgetWindow.axaml.cs</c>).
    /// </summary>
    public async Task ReorderAsync(Guid draggedTaskId, Guid targetTaskId)
    {
        if (draggedTaskId == targetTaskId)
        {
            return;
        }

        var orderedIds = Tasks.Select(t => t.Id).ToList();
        if (!orderedIds.Contains(draggedTaskId) || !orderedIds.Contains(targetTaskId))
        {
            return;
        }

        // Re-find targetTaskId's index after removal (it shifts down by one if it
        // originally came after draggedTaskId) rather than reusing the pre-removal index.
        orderedIds.Remove(draggedTaskId);
        orderedIds.Insert(orderedIds.IndexOf(targetTaskId), draggedTaskId);

        try
        {
            await _taskService.ReorderTasksAsync(PlanDate, orderedIds);
            await LoadTasksAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder tasks for {PlanDate}", PlanDate);
        }
    }

    private void OnDayRolloverTick(object? sender, EventArgs e)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (today == PlanDate)
        {
            return;
        }

        _logger.LogInformation("Day rolled over from {PreviousDate} to {NewDate}; reloading widget", PlanDate, today);
        PlanDate = today;
        OnPropertyChanged(nameof(PlanDate));
        OnPropertyChanged(nameof(DayOfWeekText));
        OnPropertyChanged(nameof(DateText));
        _ = LoadTasksAsync();
    }

    public void Dispose()
    {
        _dayRolloverTimer.Stop();
        _dayRolloverTimer.Tick -= OnDayRolloverTick;

        foreach (var task in Tasks)
        {
            task.PropertyChanged -= OnTaskItemPropertyChanged;
        }
    }
}
