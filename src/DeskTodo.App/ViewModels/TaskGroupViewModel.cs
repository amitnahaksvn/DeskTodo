using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Task Groups window — creating a named group from a multi-selected set of
/// existing <see cref="TemplateOption"/>s, deleting a group, and applying one (creating one
/// real task per member template, via <see cref="ITaskGroupService.CreateTasksFromGroupAsync"/>)
/// onto a chosen date in one click.
/// </summary>
public sealed partial class TaskGroupViewModel : ViewModelBase
{
    private readonly ITaskGroupService _groupService;
    private readonly ITaskTemplateService _templateService;
    private readonly ILogger<TaskGroupViewModel> _logger;

    public TaskGroupViewModel(ITaskGroupService groupService, ITaskTemplateService templateService, ILogger<TaskGroupViewModel> logger)
    {
        _groupService = groupService;
        _templateService = templateService;
        _logger = logger;
    }

    public ObservableCollection<TaskGroupOption> Groups { get; } = [];

    public ObservableCollection<SelectableTemplateOption> AvailableTemplates { get; } = [];

    [ObservableProperty]
    public partial string NewGroupName { get; set; } = string.Empty;

    /// <summary>The date a group's tasks get created on when "Add to Day" runs — defaults to today, but is a real <c>DatePicker</c>-bound date, not locked to today, matching the original "add to day, days, week, or month" ask's "not just today" half. Every task in the group lands on this one chosen day (see this feature's own scoping note in IMPLEMENTATION.md for why a group doesn't repeat itself across a date range).</summary>
    [ObservableProperty]
    public partial DateTimeOffset ApplyDate { get; set; } = DateTimeOffset.Now;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        ErrorMessage = string.Empty;

        var templates = await _templateService.GetTemplatesAsync(cancellationToken);
        AvailableTemplates.Clear();
        foreach (var template in templates)
        {
            AvailableTemplates.Add(new SelectableTemplateOption(template.Id, template.Name));
        }

        await RefreshGroupsAsync(cancellationToken);
    }

    private async Task RefreshGroupsAsync(CancellationToken cancellationToken = default)
    {
        var groups = await _groupService.GetGroupsAsync(cancellationToken);

        Groups.Clear();
        foreach (var group in groups)
        {
            var memberNames = group.TemplateIds
                .Select(id => AvailableTemplates.FirstOrDefault(t => t.Id == id)?.Name)
                .Where(name => name is not null);
            Groups.Add(new TaskGroupOption(group.Id, group.Name, string.Join(", ", memberNames)));
        }
    }

    [RelayCommand]
    private async Task CreateGroupAsync()
    {
        ErrorMessage = string.Empty;
        var name = NewGroupName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ErrorMessage = "Enter a name for the group.";
            return;
        }

        var selectedIds = AvailableTemplates.Where(t => t.IsSelected).Select(t => t.Id).ToList();
        if (selectedIds.Count == 0)
        {
            ErrorMessage = "Pick at least one template for the group.";
            return;
        }

        try
        {
            await _groupService.CreateGroupAsync(name, selectedIds);
            NewGroupName = string.Empty;
            foreach (var template in AvailableTemplates)
            {
                template.IsSelected = false;
            }

            await RefreshGroupsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task group '{Name}'", name);
            ErrorMessage = "Couldn't create the group.";
        }
    }

    [RelayCommand]
    private async Task DeleteGroupAsync(Guid groupId)
    {
        try
        {
            await _groupService.DeleteGroupAsync(groupId);
            await RefreshGroupsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete task group {GroupId}", groupId);
        }
    }

    [RelayCommand]
    private async Task ApplyGroupAsync(Guid groupId)
    {
        ErrorMessage = string.Empty;
        try
        {
            var planDate = DateOnly.FromDateTime(ApplyDate.DateTime);
            var created = await _groupService.CreateTasksFromGroupAsync(groupId, planDate);
            if (created.Count == 0)
            {
                ErrorMessage = "That group has no valid templates left to add.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tasks from group {GroupId}", groupId);
            ErrorMessage = "Couldn't add that group's tasks.";
        }
    }
}
