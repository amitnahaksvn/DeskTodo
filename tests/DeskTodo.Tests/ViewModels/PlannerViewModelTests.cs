using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class PlannerViewModelTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly Mock<IGoalService> _goalService = new();
    private readonly Mock<IMilestoneService> _milestoneService = new();
    private readonly Mock<IProjectService> _projectService = new();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero));
    private readonly PlannerViewModel _sut;

    public PlannerViewModelTests()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _goalService.Setup(s => s.GetGoalsAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _milestoneService.Setup(s => s.GetMilestonesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var week = new WeekViewModel(_taskService.Object, _timeProvider, NullLogger<WeekViewModel>.Instance);
        var year = new YearViewModel(_taskService.Object, _timeProvider, NullLogger<YearViewModel>.Instance);
        var agenda = new AgendaViewModel(_taskService.Object, _timeProvider, NullLogger<AgendaViewModel>.Instance);
        var timeline = new TimelineViewModel(_taskService.Object, NullLogger<TimelineViewModel>.Instance);
        var kanban = new KanbanViewModel(_taskService.Object, NullLogger<KanbanViewModel>.Instance);
        var matrix = new MatrixViewModel(_taskService.Object, _timeProvider, NullLogger<MatrixViewModel>.Instance);
        var goals = new GoalsViewModel(_goalService.Object, _timeProvider, NullLogger<GoalsViewModel>.Instance);
        var milestones = new MilestonesViewModel(_milestoneService.Object, NullLogger<MilestonesViewModel>.Instance);
        var projects = new ProjectsViewModel(_projectService.Object, NullLogger<ProjectsViewModel>.Instance);
        _sut = new PlannerViewModel(week, year, agenda, timeline, kanban, matrix, goals, milestones, projects);
    }

    [Fact]
    public async Task LoadAsync_LoadsEveryTab()
    {
        await _sut.LoadAsync(new DateOnly(2026, 8, 1));

        Assert.NotEmpty(_sut.Week.Days);
        Assert.NotEmpty(_sut.Year.Months);
        // Agenda/Timeline/Kanban/Matrix/Goals/Milestones/Projects have nothing in this
        // fixture, so "loaded" just means LoadAsync didn't throw and left them in their
        // empty-but-initialized state.
        Assert.Empty(_sut.Agenda.Groups);
        Assert.Empty(_sut.Timeline.Tasks);
        Assert.Empty(_sut.Kanban.ToDoCards);
        Assert.Empty(_sut.Goals.Goals);
        Assert.Empty(_sut.Milestones.Milestones);
        Assert.Empty(_sut.Projects.Projects);
    }

    [Fact]
    public async Task DateSelected_FromWeek_BubblesUpThroughPlanner()
    {
        await _sut.LoadAsync(new DateOnly(2026, 8, 1));
        DateOnly? selected = null;
        _sut.DateSelected += (_, date) => selected = date;

        _sut.Week.Days[0].SelectCommand.Execute(null);

        Assert.NotNull(selected);
    }

    [Fact]
    public async Task DateSelected_FromYear_BubblesUpThroughPlanner()
    {
        await _sut.LoadAsync(new DateOnly(2026, 8, 1));
        DateOnly? selected = null;
        _sut.DateSelected += (_, date) => selected = date;

        _sut.Year.Months[0].SelectCommand.Execute(null);

        Assert.Equal(new DateOnly(2026, 1, 1), selected);
    }

    [Fact]
    public void CloseCommand_RaisesCloseRequested()
    {
        var raised = false;
        _sut.CloseRequested += (_, _) => raised = true;

        _sut.CloseCommand.Execute(null);

        Assert.True(raised);
    }
}
