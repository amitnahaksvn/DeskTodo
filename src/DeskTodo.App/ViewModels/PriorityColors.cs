using DeskTodo.Domain.Enums;

namespace DeskTodo.App.ViewModels;

/// <summary>Shared priority→hex-color mapping — <see cref="TaskItemViewModel"/>'s row dot and every Phase 21 planner view (Agenda/Timeline/Kanban/Matrix) that shows a task's priority as a color both read from here, so the mapping can't drift between them.</summary>
internal static class PriorityColors
{
    public static string ForPriority(TaskPriority priority) => priority switch
    {
        TaskPriority.Low => "#94A3B8",
        TaskPriority.Medium => "#3B82F6",
        TaskPriority.High => "#F97316",
        TaskPriority.Critical => "#EF4444",
        _ => "#94A3B8",
    };
}
