using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class MatrixViewModelTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero));
    private readonly MatrixViewModel _sut;

    public MatrixViewModelTests()
    {
        _sut = new MatrixViewModel(_taskService.Object, _timeProvider, NullLogger<MatrixViewModel>.Instance);
    }

    private static TaskItem CreateTask(TaskPriority priority, DateTime? dueDate) =>
        new() { PlanDate = new DateOnly(2026, 8, 15), Title = $"{priority}-{dueDate:o}", Priority = priority, DueDate = dueDate };

    [Fact]
    public async Task LoadAsync_PutsHighPriorityDueSoonIntoUrgentImportant()
    {
        var task = CreateTask(TaskPriority.Critical, new DateTime(2026, 8, 15));
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        await _sut.LoadAsync();

        Assert.Single(_sut.UrgentImportant.Tasks);
        Assert.Empty(_sut.NotUrgentImportant.Tasks);
        Assert.Empty(_sut.UrgentNotImportant.Tasks);
        Assert.Empty(_sut.NotUrgentNotImportant.Tasks);
    }

    [Fact]
    public async Task LoadAsync_PutsHighPriorityDueLaterIntoNotUrgentImportant()
    {
        var task = CreateTask(TaskPriority.High, new DateTime(2026, 9, 1));
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        await _sut.LoadAsync();

        Assert.Single(_sut.NotUrgentImportant.Tasks);
    }

    [Fact]
    public async Task LoadAsync_PutsLowPriorityDueSoonIntoUrgentNotImportant()
    {
        var task = CreateTask(TaskPriority.Low, new DateTime(2026, 8, 16));
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        await _sut.LoadAsync();

        Assert.Single(_sut.UrgentNotImportant.Tasks);
    }

    [Fact]
    public async Task LoadAsync_PutsLowPriorityWithNoDueDateIntoNeither()
    {
        var task = CreateTask(TaskPriority.Medium, null);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        await _sut.LoadAsync();

        Assert.Single(_sut.NotUrgentNotImportant.Tasks);
    }

    [Fact]
    public async Task LoadAsync_AnOverdueTaskIsUrgentRegardlessOfPriority()
    {
        var task = CreateTask(TaskPriority.Low, new DateTime(2026, 8, 1));
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        await _sut.LoadAsync();

        Assert.Single(_sut.UrgentNotImportant.Tasks);
    }

    [Fact]
    public async Task LoadAsync_ExcludesCompletedAndArchivedTasks()
    {
        var completed = CreateTask(TaskPriority.Critical, new DateTime(2026, 8, 15));
        completed.Complete();
        var archived = CreateTask(TaskPriority.Critical, new DateTime(2026, 8, 15));
        archived.Archive();
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([completed, archived]);

        await _sut.LoadAsync();

        Assert.Empty(_sut.UrgentImportant.Tasks);
    }

    [Fact]
    public async Task ClickingATaskRow_RaisesDateSelected()
    {
        var task = CreateTask(TaskPriority.Critical, new DateTime(2026, 8, 15));
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        await _sut.LoadAsync();
        DateOnly? selected = null;
        _sut.DateSelected += (_, date) => selected = date;

        _sut.UrgentImportant.Tasks[0].SelectCommand.Execute(null);

        Assert.Equal(new DateOnly(2026, 8, 15), selected);
    }
}
