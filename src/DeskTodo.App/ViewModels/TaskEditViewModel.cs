using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the full-field task editor (description, priority, category, due
/// date, estimated time, notes — everything <see cref="TaskItemViewModel"/>'s
/// inline title edit doesn't cover). Depends on <see cref="ICategoryRepository"/>
/// directly (rather than only <see cref="ITaskService"/>) purely to populate
/// the category dropdown — a plain read, not a use case, so routing it
/// through the service layer would just be ceremony.
/// </summary>
public sealed partial class TaskEditViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<TaskEditViewModel> _logger;
    private Guid _taskId;

    public TaskEditViewModel(ITaskService taskService, ICategoryRepository categoryRepository, ILogger<TaskEditViewModel> logger)
    {
        _taskService = taskService;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public ObservableCollection<CategoryOption> Categories { get; } = [CategoryOption.None];

    public IReadOnlyList<TaskPriority> Priorities { get; } = Enum.GetValues<TaskPriority>();

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial TaskPriority Priority { get; set; } = TaskPriority.Medium;

    [ObservableProperty]
    public partial CategoryOption SelectedCategory { get; set; } = CategoryOption.None;

    [ObservableProperty]
    public partial DateTimeOffset? DueDate { get; set; }

    [ObservableProperty]
    public partial decimal? EstimatedMinutes { get; set; }

    /// <summary>Raised after a successful save; the view closes itself in response.</summary>
    public event EventHandler? Saved;

    public event EventHandler? CancelRequested;

    public async Task LoadAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        _taskId = taskId;

        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        Categories.Clear();
        Categories.Add(CategoryOption.None);
        foreach (var category in categories)
        {
            Categories.Add(new CategoryOption(category.Id, category.Name, category.ColorHex));
        }

        var task = await _taskService.GetTaskAsync(taskId, cancellationToken);
        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} could not be loaded for editing (it may have just been deleted)", taskId);
            CancelRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        Title = task.Title;
        Description = task.Description ?? string.Empty;
        Notes = task.Notes ?? string.Empty;
        Priority = task.Priority;
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == task.CategoryId) ?? CategoryOption.None;
        DueDate = task.DueDate is { } due ? new DateTimeOffset(due) : null;
        EstimatedMinutes = task.EstimatedMinutes;
        IsLoaded = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var title = Title.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        try
        {
            var task = await _taskService.GetTaskAsync(_taskId);
            if (task is null)
            {
                _logger.LogWarning("Task {TaskId} no longer exists; discarding edit", _taskId);
                CancelRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            task.Title = title;
            task.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
            task.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
            task.Priority = Priority;
            task.CategoryId = SelectedCategory.Id;
            task.DueDate = DueDate?.DateTime;
            task.EstimatedMinutes = EstimatedMinutes.HasValue ? (int)EstimatedMinutes.Value : null;

            await _taskService.UpdateTaskAsync(task);
            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save edits for task {TaskId}", _taskId);
        }
    }

    [RelayCommand]
    private void Cancel() => CancelRequested?.Invoke(this, EventArgs.Empty);
}
