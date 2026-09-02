using DeskTodo.Domain.Enums;

namespace DeskTodo.App.ViewModels;

/// <summary>A read-only row shown in the Project Templates window's recurring schedules list.</summary>
public sealed record RecurringProjectScheduleRow(
    Guid Id,
    string Name,
    string TemplateName,
    ProjectRecurrenceFrequency Frequency,
    DateOnly NextOccurrenceDate,
    bool IsActive,
    int GeneratedCount)
{
    public string ToggleLabel => IsActive ? "Pause" : "Resume";
}
