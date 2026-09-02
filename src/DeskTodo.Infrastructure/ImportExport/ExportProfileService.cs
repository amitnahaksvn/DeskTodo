using DeskTodo.Application.Abstractions;
using DeskTodo.Application.DTOs;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Domain.Exceptions;

namespace DeskTodo.Infrastructure.ImportExport;

/// <inheritdoc cref="IExportProfileService"/>
public sealed class ExportProfileService(
    IExportProfileRepository profileRepository,
    ITaskService taskService,
    ITaskExportService taskExportService) : IExportProfileService
{
    public Task<IReadOnlyList<ExportProfile>> GetProfilesAsync(CancellationToken cancellationToken = default) =>
        profileRepository.GetAllAsync(cancellationToken);

    public async Task<ExportProfile> CreateProfileAsync(
        string name,
        ExportFormat format,
        Guid? projectId,
        ExportDateRange dateRange,
        CancellationToken cancellationToken = default)
    {
        var profile = new ExportProfile
        {
            Name = name.Trim(),
            Format = format,
            ProjectId = projectId,
            DateRange = dateRange,
        };

        await profileRepository.AddAsync(profile, cancellationToken);
        return profile;
    }

    public Task DeleteProfileAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        profileRepository.DeleteAsync(profileId, cancellationToken);

    public async Task<int> RunProfileAsync(Guid profileId, Stream destination, CancellationToken cancellationToken = default)
    {
        var profile = await profileRepository.GetByIdAsync(profileId, cancellationToken)
            ?? throw new ExportProfileNotFoundException(profileId);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var allTasks = await taskService.GetAllTasksAsync(cancellationToken);

        var filtered = allTasks
            .Where(task => profile.ProjectId is null || task.ProjectId == profile.ProjectId)
            .Where(task => IsInRange(task.PlanDate, profile.DateRange, today))
            .ToList();

        var records = filtered.Select(ToRecord).ToList();
        await taskExportService.ExportAsync(records, ToTaskExportFormat(profile.Format), destination, cancellationToken);

        return records.Count;
    }

    private static bool IsInRange(DateOnly planDate, ExportDateRange range, DateOnly today) => range switch
    {
        ExportDateRange.Today => planDate == today,
        ExportDateRange.ThisWeek => StartOfWeek(today) <= planDate && planDate <= StartOfWeek(today).AddDays(6),
        ExportDateRange.ThisMonth => planDate.Year == today.Year && planDate.Month == today.Month,
        _ => true,
    };

    // Sunday-based week, matching AnalyticsService.StartOfWeek's existing convention.
    private static DateOnly StartOfWeek(DateOnly date) => date.AddDays(-(int)date.DayOfWeek);

    private static TaskExportFormat ToTaskExportFormat(ExportFormat format) => format switch
    {
        ExportFormat.Csv => TaskExportFormat.Csv,
        ExportFormat.Json => TaskExportFormat.Json,
        ExportFormat.Markdown => TaskExportFormat.Markdown,
        ExportFormat.Excel => TaskExportFormat.Excel,
        _ => TaskExportFormat.Csv,
    };

    private static TaskExportRecord ToRecord(TaskItem task) => new()
    {
        Title = task.Title,
        Description = task.Description,
        PlanDate = task.PlanDate,
        DueDate = task.DueDate,
        Priority = task.Priority.ToString(),
        Category = task.Category?.Name,
        Notes = task.Notes,
        IsCompleted = task.IsCompleted,
        IsPinned = task.IsPinned,
        EstimatedMinutes = task.EstimatedMinutes,
    };
}
