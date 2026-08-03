using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>A card in <see cref="KanbanViewModel"/>'s To Do/Done board. Owns its own <see cref="MoveCommand"/> (via constructor-injected service, same pattern as <see cref="SubtaskRowViewModel"/>) — completing/reopening a task through <see cref="ITaskService"/> is what actually moves it between columns; <see cref="KanbanViewModel"/> reloads afterward rather than this card mutating the parent's collections itself.</summary>
public sealed class KanbanCardViewModel
{
    public KanbanCardViewModel(TaskItem task, ITaskService taskService, ILogger logger, Action requestRefresh)
    {
        Id = task.Id;
        Title = task.Title;
        IsCompleted = task.IsCompleted;
        PriorityColorHex = PriorityColors.ForPriority(task.Priority);
        CategoryName = task.Category?.Name;

        MoveCommand = new AsyncRelayCommand(async () =>
        {
            try
            {
                if (IsCompleted)
                {
                    await taskService.ReopenTaskAsync(Id);
                }
                else
                {
                    await taskService.CompleteTaskAsync(Id);
                }

                requestRefresh();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to move kanban card {TaskId} between columns", Id);
            }
        });
    }

    public Guid Id { get; }

    public string Title { get; }

    public bool IsCompleted { get; }

    public string PriorityColorHex { get; }

    public string? CategoryName { get; }

    public string MoveButtonLabel => IsCompleted ? "Move to To Do" : "Move to Done";

    /// <summary>"Move to Done"/"Move back to To Do" — a click, not drag-and-drop between columns. A real drag gesture (Avalonia's DragDrop API, already used for the widget's own row reordering) is a reasonable follow-up, deliberately not built here to keep this pass's scope to the underlying To Do/Done model itself.</summary>
    public IAsyncRelayCommand MoveCommand { get; }
}
