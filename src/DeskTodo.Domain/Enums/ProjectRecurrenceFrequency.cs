namespace DeskTodo.Domain.Enums;

/// <summary>How often a <see cref="Entities.RecurringProjectSchedule"/> generates its next project — deliberately separate from <see cref="RecurrenceFrequency"/> (which recurs single tasks) since project-level recurrence needs Quarterly/Yearly cadences the roadmap's own examples ("Quarterly Planning", "Annual Audit") call for and task recurrence never has.</summary>
public enum ProjectRecurrenceFrequency
{
    Weekly = 0,
    Monthly = 1,
    Quarterly = 2,
    Yearly = 3,
}
