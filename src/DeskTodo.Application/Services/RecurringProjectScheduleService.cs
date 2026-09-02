using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Domain.Exceptions;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IRecurringProjectScheduleService"/>
public sealed class RecurringProjectScheduleService(
    IRecurringProjectScheduleRepository scheduleRepository,
    IProjectTemplateService projectTemplateService) : IRecurringProjectScheduleService
{
    public Task<IReadOnlyList<RecurringProjectSchedule>> GetSchedulesAsync(CancellationToken cancellationToken = default) =>
        scheduleRepository.GetAllAsync(cancellationToken);

    public async Task<RecurringProjectSchedule> CreateScheduleAsync(
        string name,
        Guid projectTemplateId,
        string colorHex,
        ProjectRecurrenceFrequency frequency,
        DateOnly startDate,
        string? generatedProjectNamePattern,
        CancellationToken cancellationToken = default)
    {
        var template = await projectTemplateService.GetTemplateAsync(projectTemplateId, cancellationToken)
            ?? throw new ProjectTemplateNotFoundException(projectTemplateId);

        var schedule = new RecurringProjectSchedule
        {
            Name = name.Trim(),
            ProjectTemplateId = template.Id,
            ColorHex = colorHex,
            Frequency = frequency,
            StartDate = startDate,
            NextOccurrenceDate = startDate,
            GeneratedProjectNamePattern = string.IsNullOrWhiteSpace(generatedProjectNamePattern) ? string.Empty : generatedProjectNamePattern.Trim(),
        };

        await scheduleRepository.AddAsync(schedule, cancellationToken);
        return schedule;
    }

    public Task DeleteScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default) =>
        scheduleRepository.DeleteAsync(scheduleId, cancellationToken);

    public async Task SetActiveAsync(Guid scheduleId, bool isActive, CancellationToken cancellationToken = default)
    {
        var schedule = await scheduleRepository.GetByIdAsync(scheduleId, cancellationToken)
            ?? throw new RecurringProjectScheduleNotFoundException(scheduleId);

        schedule.IsActive = isActive;
        await scheduleRepository.UpdateAsync(schedule, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GenerateDueProjectsAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        var due = await scheduleRepository.GetDueAsync(asOf, cancellationToken);
        var generated = new List<Project>();

        foreach (var schedule in due)
        {
            var occurrenceDate = schedule.NextOccurrenceDate;
            var projectName = string.IsNullOrEmpty(schedule.GeneratedProjectNamePattern)
                ? $"{schedule.Name} — {occurrenceDate:yyyy-MM-dd}"
                : string.Format(schedule.GeneratedProjectNamePattern, occurrenceDate);

            var project = await projectTemplateService.CreateProjectFromTemplateAsync(
                schedule.ProjectTemplateId, projectName, schedule.ColorHex, occurrenceDate, cancellationToken);

            schedule.GeneratedProjectIds.Add(project.Id);
            schedule.NextOccurrenceDate = Advance(occurrenceDate, schedule.Frequency);
            await scheduleRepository.UpdateAsync(schedule, cancellationToken);

            generated.Add(project);
        }

        return generated;
    }

    private static DateOnly Advance(DateOnly date, ProjectRecurrenceFrequency frequency) => frequency switch
    {
        ProjectRecurrenceFrequency.Weekly => date.AddDays(7),
        ProjectRecurrenceFrequency.Monthly => date.AddMonths(1),
        ProjectRecurrenceFrequency.Quarterly => date.AddMonths(3),
        ProjectRecurrenceFrequency.Yearly => date.AddYears(1),
        _ => date.AddMonths(1),
    };
}
