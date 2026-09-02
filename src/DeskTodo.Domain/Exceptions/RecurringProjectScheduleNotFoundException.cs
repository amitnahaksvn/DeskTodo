namespace DeskTodo.Domain.Exceptions;

/// <summary>Thrown when an operation references a recurring project schedule ID that doesn't exist.</summary>
public sealed class RecurringProjectScheduleNotFoundException(Guid scheduleId)
    : Exception($"No recurring project schedule was found with id '{scheduleId}'.")
{
    public Guid ScheduleId { get; } = scheduleId;
}
