using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Moq;
using TaskPriority = DeskTodo.Domain.Enums.TaskPriority;

namespace DeskTodo.Tests.Application;

public class InboxServiceTests
{
    private readonly Mock<IInboxRepository> _inboxRepository = new();
    private readonly Mock<ITaskService> _taskService = new();
    private readonly InboxService _sut;

    public InboxServiceTests()
    {
        _sut = new InboxService(_inboxRepository.Object, _taskService.Object);
    }

    [Fact]
    public async Task CaptureAsync_AddsANewUnprocessedItem()
    {
        var result = await _sut.CaptureAsync("Buy milk");

        Assert.Equal("Buy milk", result.Content);
        Assert.Equal(InboxItemStatus.Unprocessed, result.Status);
        _inboxRepository.Verify(r => r.AddAsync(It.Is<InboxItem>(i => i.Content == "Buy milk"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConvertToTaskAsync_CreatesATask_AndMarksTheItemConverted()
    {
        var item = new InboxItem { Content = "Write report" };
        var planDate = new DateOnly(2026, 9, 1);
        var createdTask = new TaskItem { PlanDate = planDate, Title = "Write report" };
        _inboxRepository.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _taskService.Setup(s => s.CreateTaskAsync(planDate, "Write report", null, It.IsAny<TaskPriority>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTask);

        var result = await _sut.ConvertToTaskAsync(item.Id, planDate);

        Assert.Equal(createdTask, result);
        Assert.Equal(InboxItemStatus.Converted, item.Status);
        Assert.Equal(createdTask.Id, item.ConvertedTaskId);
        _inboxRepository.Verify(r => r.UpdateAsync(item, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConvertToTaskAsync_WithUnknownId_Throws()
    {
        _inboxRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((InboxItem?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ConvertToTaskAsync(Guid.NewGuid(), new DateOnly(2026, 9, 1)));
    }

    [Fact]
    public async Task ArchiveAsync_MarksTheItemArchived()
    {
        var item = new InboxItem { Content = "Someday" };
        _inboxRepository.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        await _sut.ArchiveAsync(item.Id);

        Assert.Equal(InboxItemStatus.Archived, item.Status);
        _inboxRepository.Verify(r => r.UpdateAsync(item, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToTheRepository()
    {
        var itemId = Guid.NewGuid();

        await _sut.DeleteAsync(itemId);

        _inboxRepository.Verify(r => r.RemoveAsync(itemId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUnprocessedAsync_DelegatesToTheRepository()
    {
        var item = new InboxItem { Content = "Queued" };
        _inboxRepository.Setup(r => r.GetUnprocessedAsync(It.IsAny<CancellationToken>())).ReturnsAsync([item]);

        var results = await _sut.GetUnprocessedAsync();

        Assert.Equal([item], results);
    }
}
