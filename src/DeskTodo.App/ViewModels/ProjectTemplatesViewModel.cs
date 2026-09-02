using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Project Templates window — Feature 86 (Project Starter Kits: save a reusable
/// project shape, then instantiate it on a chosen start date) and Feature 87 (Recurring
/// Project Templates: a schedule that instantiates a template automatically on a cadence),
/// combined into one window since a schedule is only ever "a template plus a cadence" — there
/// is nothing to configure about recurrence beyond picking an existing template.
/// </summary>
public sealed partial class ProjectTemplatesViewModel(
    IProjectTemplateService templateService,
    IRecurringProjectScheduleService scheduleService,
    ILogger<ProjectTemplatesViewModel> logger) : ViewModelBase
{
    public ObservableCollection<ProjectTemplateRow> Templates { get; } = [];

    public ObservableCollection<ProjectTemplateOption> TemplateOptions { get; } = [];

    public ObservableCollection<RecurringProjectScheduleRow> Schedules { get; } = [];

    public IReadOnlyList<ProjectRecurrenceFrequency> FrequencyOptions { get; } =
        [ProjectRecurrenceFrequency.Weekly, ProjectRecurrenceFrequency.Monthly, ProjectRecurrenceFrequency.Quarterly, ProjectRecurrenceFrequency.Yearly];

    [ObservableProperty]
    public partial string NewTemplateName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewTemplateDescription { get; set; } = string.Empty;

    /// <summary>One task per line: <c>Title | Priority | DayOffsetStart | DurationDays</c> — e.g. <c>Development | Medium | 2 | 6</c>. <see cref="ParseTaskItemsText"/> silently skips a line it can't parse rather than blocking the whole save, the same "best effort, not all-or-nothing" reasoning <c>TaskGroupService</c> uses for a deleted member template.</summary>
    [ObservableProperty]
    public partial string TaskItemsText { get; set; } = string.Empty;

    /// <summary>One milestone per line: <c>Title | DayOffset</c> — e.g. <c>Code Complete | 7</c>.</summary>
    [ObservableProperty]
    public partial string MilestoneItemsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ProjectTemplateOption? SelectedTemplateForProject { get; set; }

    [ObservableProperty]
    public partial string NewProjectName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewProjectColorHex { get; set; } = "#4A90D9";

    [ObservableProperty]
    public partial DateTimeOffset NewProjectStartDate { get; set; } = DateTimeOffset.Now;

    [ObservableProperty]
    public partial string NewScheduleName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ProjectTemplateOption? SelectedTemplateForSchedule { get; set; }

    [ObservableProperty]
    public partial ProjectRecurrenceFrequency NewScheduleFrequency { get; set; } = ProjectRecurrenceFrequency.Monthly;

    [ObservableProperty]
    public partial DateTimeOffset NewScheduleStartDate { get; set; } = DateTimeOffset.Now;

    [ObservableProperty]
    public partial string NewScheduleNamePattern { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = await templateService.GetTemplatesAsync(cancellationToken);
            Templates.Clear();
            TemplateOptions.Clear();
            foreach (var template in templates)
            {
                Templates.Add(new ProjectTemplateRow(template.Id, template.Name, template.Description, template.TaskItems.Count, template.MilestoneItems.Count));
                TemplateOptions.Add(new ProjectTemplateOption(template.Id, template.Name));
            }

            var templateNameById = templates.ToDictionary(t => t.Id, t => t.Name);
            var schedules = await scheduleService.GetSchedulesAsync(cancellationToken);
            Schedules.Clear();
            foreach (var schedule in schedules)
            {
                Schedules.Add(new RecurringProjectScheduleRow(
                    schedule.Id, schedule.Name,
                    templateNameById.GetValueOrDefault(schedule.ProjectTemplateId, "(deleted template)"),
                    schedule.Frequency, schedule.NextOccurrenceDate, schedule.IsActive, schedule.GeneratedProjectIds.Count));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load project templates");
            ErrorMessage = "Couldn't load project templates.";
        }
    }

    internal static List<ProjectTemplateTaskItem> ParseTaskItemsText(string text)
    {
        var items = new List<ProjectTemplateTaskItem>();
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var parts = line.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || parts[0].Length == 0)
            {
                continue;
            }

            var priority = parts.Length > 1 && Enum.TryParse<TaskPriority>(parts[1], ignoreCase: true, out var parsedPriority) ? parsedPriority : TaskPriority.Medium;
            var dayOffsetStart = parts.Length > 2 && int.TryParse(parts[2], out var parsedOffset) ? Math.Max(1, parsedOffset) : 1;
            var durationDays = parts.Length > 3 && int.TryParse(parts[3], out var parsedDuration) ? Math.Max(1, parsedDuration) : 1;

            items.Add(new ProjectTemplateTaskItem { Title = parts[0], Priority = priority, DayOffsetStart = dayOffsetStart, DurationDays = durationDays });
        }

        return items;
    }

    internal static List<ProjectTemplateMilestoneItem> ParseMilestoneItemsText(string text)
    {
        var items = new List<ProjectTemplateMilestoneItem>();
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var parts = line.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || parts[0].Length == 0)
            {
                continue;
            }

            var dayOffset = parts.Length > 1 && int.TryParse(parts[1], out var parsedOffset) ? Math.Max(1, parsedOffset) : 1;
            items.Add(new ProjectTemplateMilestoneItem { Title = parts[0], DayOffset = dayOffset });
        }

        return items;
    }

    [RelayCommand]
    private async Task CreateTemplateAsync()
    {
        ErrorMessage = string.Empty;
        var name = NewTemplateName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ErrorMessage = "Enter a name for the template.";
            return;
        }

        var taskItems = ParseTaskItemsText(TaskItemsText);
        if (taskItems.Count == 0)
        {
            ErrorMessage = "Add at least one task (Title | Priority | DayOffsetStart | DurationDays, one per line).";
            return;
        }

        try
        {
            await templateService.CreateTemplateAsync(name, NewTemplateDescription, taskItems, ParseMilestoneItemsText(MilestoneItemsText));
            NewTemplateName = string.Empty;
            NewTemplateDescription = string.Empty;
            TaskItemsText = string.Empty;
            MilestoneItemsText = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create project template '{Name}'", name);
            ErrorMessage = "Couldn't create the template.";
        }
    }

    [RelayCommand]
    private async Task DeleteTemplateAsync(Guid templateId)
    {
        try
        {
            await templateService.DeleteTemplateAsync(templateId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete project template {TemplateId}", templateId);
        }
    }

    [RelayCommand]
    private async Task CreateProjectFromTemplateAsync()
    {
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;
        if (SelectedTemplateForProject is not { } template)
        {
            ErrorMessage = "Pick a template first.";
            return;
        }

        var projectName = string.IsNullOrWhiteSpace(NewProjectName) ? template.Name : NewProjectName.Trim();
        try
        {
            var project = await templateService.CreateProjectFromTemplateAsync(
                template.Id, projectName, NewProjectColorHex, DateOnly.FromDateTime(NewProjectStartDate.DateTime));
            StatusMessage = $"Created project \"{project.Name}\".";
            NewProjectName = string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create a project from template {TemplateId}", template.Id);
            ErrorMessage = "Couldn't create the project.";
        }
    }

    [RelayCommand]
    private async Task CreateScheduleAsync()
    {
        ErrorMessage = string.Empty;
        var name = NewScheduleName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ErrorMessage = "Enter a name for the schedule.";
            return;
        }

        if (SelectedTemplateForSchedule is not { } template)
        {
            ErrorMessage = "Pick a template for the schedule.";
            return;
        }

        try
        {
            await scheduleService.CreateScheduleAsync(
                name, template.Id, NewProjectColorHex, NewScheduleFrequency,
                DateOnly.FromDateTime(NewScheduleStartDate.DateTime), NewScheduleNamePattern);
            NewScheduleName = string.Empty;
            NewScheduleNamePattern = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create recurring project schedule '{Name}'", name);
            ErrorMessage = "Couldn't create the schedule.";
        }
    }

    [RelayCommand]
    private async Task ToggleScheduleActiveAsync(RecurringProjectScheduleRow row)
    {
        try
        {
            await scheduleService.SetActiveAsync(row.Id, !row.IsActive);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to toggle recurring project schedule {ScheduleId}", row.Id);
        }
    }

    [RelayCommand]
    private async Task DeleteScheduleAsync(Guid scheduleId)
    {
        try
        {
            await scheduleService.DeleteScheduleAsync(scheduleId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete recurring project schedule {ScheduleId}", scheduleId);
        }
    }
}
