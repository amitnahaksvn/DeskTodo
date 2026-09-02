using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Task Relationships Graph window (Feature 48, Roadmap-39-100.md). Deliberately shows
/// only the centered task's direct (1-hop) relationships, not a transitive walk of the whole
/// graph — this feature's own "avoid infinite graph loading" requirement, satisfied by never
/// having an unbounded query in the first place rather than by capping one. Clicking a neighbor
/// node can re-center the graph on it ("Recenter"), which is how a user explores further out one
/// hop at a time. Layout is a fixed hub-and-spoke placement (the centered task in the middle,
/// its neighbors evenly spaced around it) rather than a force-directed algorithm — simple,
/// deterministic, and this app has no graph-layout library dependency to add for it.
/// </summary>
public sealed partial class TaskGraphViewModel : ViewModelBase
{
    private const double CenterX = 320;
    private const double CenterY = 320;
    private const double Radius = 220;

    private readonly ITaskService _taskService;
    private readonly ITaskRelationshipService _relationshipService;
    private readonly ILogger<TaskGraphViewModel> _logger;

    private IReadOnlyList<Domain.Entities.TaskRelationship> _relationships = [];

    public TaskGraphViewModel(ITaskService taskService, ITaskRelationshipService relationshipService, ILogger<TaskGraphViewModel> logger)
    {
        _taskService = taskService;
        _relationshipService = relationshipService;
        _logger = logger;

        foreach (var type in Enum.GetValues<TaskRelationshipType>())
        {
            var filter = new RelationshipTypeFilterOption(type);
            filter.PropertyChanged += (_, _) => RebuildGraph();
            TypeFilters.Add(filter);
        }
    }

    [ObservableProperty]
    public partial Guid CenterTaskId { get; set; }

    [ObservableProperty]
    public partial string CenterTaskTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double ZoomLevel { get; set; } = 1.0;

    [ObservableProperty]
    public partial GraphNode? SelectedNode { get; set; }

    [ObservableProperty]
    public partial TaskOption? TargetTaskToAdd { get; set; }

    [ObservableProperty]
    public partial TaskRelationshipType RelationshipTypeToAdd { get; set; } = TaskRelationshipType.Related;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public ObservableCollection<GraphNode> Nodes { get; } = [];

    public ObservableCollection<GraphEdge> Edges { get; } = [];

    public ObservableCollection<TaskOption> AvailableTasks { get; } = [];

    public ObservableCollection<RelationshipTypeFilterOption> TypeFilters { get; } = [];

    public IReadOnlyList<TaskRelationshipType> RelationshipTypeOptions { get; } = Enum.GetValues<TaskRelationshipType>();

    /// <summary>Raised when the user clicks "Open Task" on the selected node — same "ViewModel shouldn't construct Views" split as everywhere else in this app.</summary>
    public event EventHandler<Guid>? OpenTaskRequested;

    public async Task LoadAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            var centerTask = await _taskService.GetTaskAsync(taskId, cancellationToken);
            if (centerTask is null)
            {
                StatusMessage = "That task couldn't be found.";
                return;
            }

            CenterTaskId = taskId;
            CenterTaskTitle = centerTask.Title;
            SelectedNode = null;

            _relationships = await _relationshipService.GetRelationshipsForTaskAsync(taskId, cancellationToken);

            if (AvailableTasks.Count == 0)
            {
                var allTasks = await _taskService.GetAllTasksAsync(cancellationToken);
                foreach (var task in allTasks.OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase))
                {
                    AvailableTasks.Add(new TaskOption(task.Id, task.Title));
                }
            }

            RebuildGraph();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the relationship graph for task {TaskId}", taskId);
            StatusMessage = "Failed to load the graph.";
        }
    }

    private void RebuildGraph()
    {
        Nodes.Clear();
        Edges.Clear();

        if (CenterTaskId == Guid.Empty)
        {
            return;
        }

        var enabledTypes = TypeFilters.Where(f => f.IsEnabled).Select(f => f.Type).ToHashSet();
        var visible = _relationships.Where(r => enabledTypes.Contains(r.RelationshipType)).ToList();

        Nodes.Add(new GraphNode(CenterTaskId, CenterTaskTitle, IsCenter: true, CenterX, CenterY));

        var neighbors = visible
            .Select(r => r.SourceTaskId == CenterTaskId
                ? (Id: r.TargetTaskId, Title: r.TargetTask?.Title ?? "(unknown task)")
                : (Id: r.SourceTaskId, Title: r.SourceTask?.Title ?? "(unknown task)"))
            .DistinctBy(n => n.Id)
            .ToList();

        for (var i = 0; i < neighbors.Count; i++)
        {
            var angle = 2 * Math.PI * i / neighbors.Count;
            var x = CenterX + (Radius * Math.Cos(angle));
            var y = CenterY + (Radius * Math.Sin(angle));
            Nodes.Add(new GraphNode(neighbors[i].Id, neighbors[i].Title, IsCenter: false, x, y));
        }

        var positionsByTaskId = Nodes.ToDictionary(n => n.TaskId, n => n);
        foreach (var relationship in visible)
        {
            if (!positionsByTaskId.TryGetValue(relationship.SourceTaskId, out var source) ||
                !positionsByTaskId.TryGetValue(relationship.TargetTaskId, out var target))
            {
                continue;
            }

            var description = relationship.SourceTaskId == CenterTaskId
                ? $"{CenterTaskTitle} — {relationship.RelationshipType} — {target.Title}"
                : $"{source.Title} — {relationship.RelationshipType} — {CenterTaskTitle}";

            Edges.Add(new GraphEdge(
                relationship.Id,
                relationship.RelationshipType.ToString(),
                new Avalonia.Point(source.X, source.Y),
                new Avalonia.Point(target.X, target.Y),
                description));
        }
    }

    [RelayCommand]
    private void SelectNode(GraphNode node) => SelectedNode = node;

    [RelayCommand]
    private async Task RecenterAsync(Guid taskId) => await LoadAsync(taskId);

    [RelayCommand]
    private void OpenTask(Guid taskId) => OpenTaskRequested?.Invoke(this, taskId);

    [RelayCommand]
    private async Task AddRelationshipAsync()
    {
        if (TargetTaskToAdd is not { Id: { } targetTaskId })
        {
            return;
        }

        try
        {
            var added = await _relationshipService.AddRelationshipAsync(CenterTaskId, targetTaskId, RelationshipTypeToAdd);
            if (added is null)
            {
                StatusMessage = "That relationship already exists (or a task can't relate to itself).";
                return;
            }

            TargetTaskToAdd = null;
            await LoadAsync(CenterTaskId);
            StatusMessage = "Relationship added.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add a relationship from task {SourceTaskId} to {TargetTaskId}", CenterTaskId, TargetTaskToAdd?.Id);
            StatusMessage = "Failed to add the relationship.";
        }
    }

    [RelayCommand]
    private async Task RemoveRelationshipAsync(Guid relationshipId)
    {
        try
        {
            await _relationshipService.RemoveRelationshipAsync(relationshipId);
            await LoadAsync(CenterTaskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove relationship {RelationshipId}", relationshipId);
        }
    }
}
