using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// A child <see cref="TaskItem"/> shown inline in the full-field editor's Subtasks
/// section. Mirrors <see cref="ChecklistItemRowViewModel"/>'s shape (owns its own
/// toggle/remove) even though a subtask is a full task, not a lightweight checklist
/// line — the editor only needs to show/toggle/remove it here, not edit every field.
/// </summary>
public sealed partial class SubtaskRowViewModel : ObservableObject
{
    private readonly ITaskService _taskService;
    private readonly ILogger _logger;
    private readonly Action<SubtaskRowViewModel> _requestRemove;

    public SubtaskRowViewModel(TaskItem task, ITaskService taskService, ILogger logger, Action<SubtaskRowViewModel> requestRemove)
    {
        _taskService = taskService;
        _logger = logger;
        _requestRemove = requestRemove;

        Id = task.Id;
        Title = task.Title;
        IsCompleted = task.IsCompleted;
    }

    public Guid Id { get; }

    public string Title { get; }

    [ObservableProperty]
    public partial bool IsCompleted { get; set; }

    [RelayCommand]
    private async Task ToggleAsync()
    {
        var newValue = !IsCompleted;
        try
        {
            await (newValue ? _taskService.CompleteTaskAsync(Id) : _taskService.ReopenTaskAsync(Id));
            IsCompleted = newValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle completion for subtask {SubtaskId}", Id);
        }
    }

    [RelayCommand]
    private async Task RemoveAsync()
    {
        try
        {
            await _taskService.DeleteTaskAsync(Id);
            _requestRemove(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove subtask {SubtaskId}", Id);
        }
    }
}
