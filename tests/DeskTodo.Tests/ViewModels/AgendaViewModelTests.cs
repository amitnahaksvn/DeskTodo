using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class AgendaViewModelTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero));
    private readonly AgendaViewModel _sut;

    public AgendaViewModelTests()
    {
        _sut = new AgendaViewModel(_taskService.Object, _timeProvider, NullLogger<AgendaViewModel>.Instance);
    }

    [Fact]
    public async Task LoadAsync_WithNoUpcomingTasks_SetsTheEmptyFlag()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.LoadAsync();

        Assert.True(_sut.HasNoUpcomingTasks);
        Assert.Empty(_sut.Groups);
    }

    [Fact]
    public async Task LoadAsync_GroupsIncompleteTasksByDate_InChronologicalOrder()
    {
        var tasks = new[]
        {
            new TaskItem { PlanDate = new DateOnly(2026, 8, 17), Title = "Later task" },
            new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Today task" },
        };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tasks);

        await _sut.LoadAsync();

        Assert.False(_sut.HasNoUpcomingTasks);
        Assert.Equal(2, _sut.Groups.Count);
        Assert.Equal(new DateOnly(2026, 8, 15), _sut.Groups[0].Date);
        Assert.Equal("Today", _sut.Groups[0].DateLabel);
        Assert.Equal(new DateOnly(2026, 8, 17), _sut.Groups[1].Date);
    }

    [Fact]
    public async Task LoadAsync_LabelsTomorrowAndOverdueDistinctly()
    {
        var tasks = new[]
        {
            new TaskItem { PlanDate = new DateOnly(2026, 8, 16), Title = "Tomorrow task" },
            new TaskItem { PlanDate = new DateOnly(2026, 8, 10), Title = "Overdue task" },
        };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tasks);

        await _sut.LoadAsync();

        Assert.Equal("Tomorrow", _sut.Groups.Single(g => g.Date == new DateOnly(2026, 8, 16)).DateLabel);
        Assert.StartsWith("Overdue", _sut.Groups.Single(g => g.Date == new DateOnly(2026, 8, 10)).DateLabel);
    }

    [Fact]
    public async Task LoadAsync_ExcludesCompletedAndArchivedAndFarFutureTasks()
    {
        var completed = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Done" };
        completed.Complete();
        var archived = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Archived" };
        archived.Archive();
        var farFuture = new TaskItem { PlanDate = new DateOnly(2026, 12, 1), Title = "Far future" };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([completed, archived, farFuture]);

        await _sut.LoadAsync();

        Assert.True(_sut.HasNoUpcomingTasks);
    }

    [Fact]
    public async Task ClickingATaskRow_RaisesDateSelected()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Task" };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        await _sut.LoadAsync();
        DateOnly? selected = null;
        _sut.DateSelected += (_, date) => selected = date;

        _sut.Groups[0].Tasks[0].SelectCommand.Execute(null);

        Assert.Equal(new DateOnly(2026, 8, 15), selected);
    }
}
