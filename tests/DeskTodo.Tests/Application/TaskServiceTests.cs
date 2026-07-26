using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Exceptions;
using Moq;

namespace DeskTodo.Tests.Application;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _taskRepository = new();
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(_taskRepository.Object);
    }

    [Fact]
    public async Task CreateTaskAsync_AssignsNextDayOrder_AndAdds()
    {
        var planDate = new DateOnly(2026, 7, 27);
        _taskRepository.Setup(r => r.GetMaxDayOrderAsync(planDate, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var task = await _sut.CreateTaskAsync(planDate, "Morning Exercise");

        Assert.Equal(3, task.DayOrder);
        Assert.Equal("Morning Exercise", task.Title);
        _taskRepository.Verify(r => r.AddAsync(It.Is<TaskItem>(t => t == task), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_OnEmptyDay_StartsAtOrderZero()
    {
        var planDate = new DateOnly(2026, 7, 27);
        _taskRepository.Setup(r => r.GetMaxDayOrderAsync(planDate, It.IsAny<CancellationToken>())).ReturnsAsync(-1);

        var task = await _sut.CreateTaskAsync(planDate, "First task of the day");

        Assert.Equal(0, task.DayOrder);
    }

    [Fact]
    public async Task CompleteTaskAsync_WhenTaskExists_CompletesAndPersists()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "Read System Design" };
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.CompleteTaskAsync(task.Id);

        Assert.True(task.IsCompleted);
        _taskRepository.Verify(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteTaskAsync_WhenTaskMissing_ThrowsTaskNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _taskRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        var exception = await Assert.ThrowsAsync<TaskNotFoundException>(() => _sut.CompleteTaskAsync(missingId));
        Assert.Equal(missingId, exception.TaskId);
    }

    [Fact]
    public async Task DuplicateTaskAsync_CopiesFieldsIntoANewTaskAtTheEndOfTheDay()
    {
        var source = new TaskItem
        {
            PlanDate = new DateOnly(2026, 7, 27),
            DayOrder = 0,
            Title = "LinkedIn Post",
            Notes = "Draft first",
        };
        _taskRepository.Setup(r => r.GetByIdAsync(source.Id, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _taskRepository.Setup(r => r.GetMaxDayOrderAsync(source.PlanDate, It.IsAny<CancellationToken>())).ReturnsAsync(4);

        var copy = await _sut.DuplicateTaskAsync(source.Id);

        Assert.NotEqual(source.Id, copy.Id);
        Assert.Equal(source.Title, copy.Title);
        Assert.Equal(source.Notes, copy.Notes);
        Assert.Equal(5, copy.DayOrder);
    }

    [Fact]
    public async Task ReorderTasksAsync_DelegatesToRepository()
    {
        var planDate = new DateOnly(2026, 7, 27);
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };

        await _sut.ReorderTasksAsync(planDate, ids);

        _taskRepository.Verify(r => r.ReorderAsync(planDate, ids, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ArchiveTaskAsync_SetsIsArchived()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 7, 27), Title = "DSA Practice" };
        _taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        await _sut.ArchiveTaskAsync(task.Id);

        Assert.True(task.IsArchived);
    }
}
