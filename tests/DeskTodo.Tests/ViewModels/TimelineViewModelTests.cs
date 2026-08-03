using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class TimelineViewModelTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly TimelineViewModel _sut;

    public TimelineViewModelTests()
    {
        _sut = new TimelineViewModel(_taskService.Object, NullLogger<TimelineViewModel>.Instance);
    }

    [Fact]
    public async Task LoadAsync_WithNoTasksHavingDueDates_SetsTheEmptyFlag()
    {
        var noDueDate = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "No due date" };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([noDueDate]);

        await _sut.LoadAsync();

        Assert.True(_sut.HasNoTasksWithDueDates);
        Assert.Empty(_sut.Tasks);
    }

    [Fact]
    public async Task LoadAsync_OrdersTasksByDueDate()
    {
        var later = new TaskItem { PlanDate = new DateOnly(2026, 8, 1), Title = "Later due", DueDate = new DateTime(2026, 8, 20, 10, 0, 0) };
        var sooner = new TaskItem { PlanDate = new DateOnly(2026, 8, 1), Title = "Sooner due", DueDate = new DateTime(2026, 8, 16, 9, 0, 0) };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([later, sooner]);

        await _sut.LoadAsync();

        Assert.False(_sut.HasNoTasksWithDueDates);
        Assert.Equal(["Sooner due", "Later due"], _sut.Tasks.Select(t => t.Title));
    }

    [Fact]
    public async Task LoadAsync_ExcludesCompletedArchivedAndNoDueDateTasks()
    {
        var completed = new TaskItem { PlanDate = new DateOnly(2026, 8, 1), Title = "Done", DueDate = new DateTime(2026, 8, 20) };
        completed.Complete();
        var archived = new TaskItem { PlanDate = new DateOnly(2026, 8, 1), Title = "Archived", DueDate = new DateTime(2026, 8, 20) };
        archived.Archive();
        var noDueDate = new TaskItem { PlanDate = new DateOnly(2026, 8, 1), Title = "No due date" };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([completed, archived, noDueDate]);

        await _sut.LoadAsync();

        Assert.True(_sut.HasNoTasksWithDueDates);
    }

    [Fact]
    public async Task ClickingATaskRow_RaisesDateSelectedWithItsPlanDate()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 12), Title = "Task", DueDate = new DateTime(2026, 8, 20) };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        await _sut.LoadAsync();
        DateOnly? selected = null;
        _sut.DateSelected += (_, date) => selected = date;

        _sut.Tasks[0].SelectCommand.Execute(null);

        Assert.Equal(new DateOnly(2026, 8, 12), selected);
    }
}
