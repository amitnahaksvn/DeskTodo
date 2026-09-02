using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Application.Services;

/// <summary>Roadmap-39-100.md Feature 87 — generates a new <see cref="Project"/> from a <see cref="ProjectTemplate"/> on a recurring cadence.</summary>
public interface IRecurringProjectScheduleService
{
    Task<IReadOnlyList<RecurringProjectSchedule>> GetSchedulesAsync(CancellationToken cancellationToken = default);

    Task<RecurringProjectSchedule> CreateScheduleAsync(
        string name,
        Guid projectTemplateId,
        string colorHex,
        ProjectRecurrenceFrequency frequency,
        DateOnly startDate,
        string? generatedProjectNamePattern,
        CancellationToken cancellationToken = default);

    Task DeleteScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    Task SetActiveAsync(Guid scheduleId, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Materializes a new <see cref="Project"/> for every active schedule whose
    /// <see cref="RecurringProjectSchedule.NextOccurrenceDate"/> is on or before
    /// <paramref name="asOf"/>, via <see cref="IProjectTemplateService.CreateProjectFromTemplateAsync"/>,
    /// then advances each schedule to its next occurrence. Returns the generated projects.
    /// </summary>
    Task<IReadOnlyList<Project>> GenerateDueProjectsAsync(DateOnly asOf, CancellationToken cancellationToken = default);
}
