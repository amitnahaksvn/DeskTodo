using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Exceptions;
using Moq;

namespace DeskTodo.Tests.Application;

public class TaskGroupServiceTests
{
    private readonly Mock<ITaskGroupRepository> _groupRepository = new();
    private readonly Mock<ITaskTemplateService> _templateService = new();
    private readonly TaskGroupService _sut;

    public TaskGroupServiceTests()
    {
        _sut = new TaskGroupService(_groupRepository.Object, _templateService.Object);
    }

    [Fact]
    public async Task CreateGroupAsync_TrimsTheNameAndPersistsTheTemplateIds()
    {
        var templateIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var group = await _sut.CreateGroupAsync("  Morning Routine  ", templateIds);

        Assert.Equal("Morning Routine", group.Name);
        Assert.Equal(templateIds, group.TemplateIds);
        _groupRepository.Verify(r => r.AddAsync(group, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateGroupAsync_ReplacesNameAndTemplateIds()
    {
        var group = new TaskGroup { Name = "Old", TemplateIds = [Guid.NewGuid()] };
        _groupRepository.Setup(r => r.GetByIdAsync(group.Id, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        var newTemplateIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        await _sut.UpdateGroupAsync(group.Id, "New", newTemplateIds);

        Assert.Equal("New", group.Name);
        Assert.Equal(newTemplateIds, group.TemplateIds);
        _groupRepository.Verify(r => r.UpdateAsync(group, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateGroupAsync_WhenGroupMissing_ThrowsTaskGroupNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _groupRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((TaskGroup?)null);

        await Assert.ThrowsAsync<TaskGroupNotFoundException>(() => _sut.UpdateGroupAsync(missingId, "X", []));
    }

    [Fact]
    public async Task DeleteGroupAsync_DelegatesToRepository()
    {
        var groupId = Guid.NewGuid();

        await _sut.DeleteGroupAsync(groupId);

        _groupRepository.Verify(r => r.DeleteAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTasksFromGroupAsync_CreatesOneTaskPerMemberTemplate_InOrder()
    {
        var planDate = new DateOnly(2026, 8, 20);
        var firstTemplateId = Guid.NewGuid();
        var secondTemplateId = Guid.NewGuid();
        var group = new TaskGroup { Name = "Routine", TemplateIds = [firstTemplateId, secondTemplateId] };
        _groupRepository.Setup(r => r.GetByIdAsync(group.Id, It.IsAny<CancellationToken>())).ReturnsAsync(group);

        var firstTask = new TaskItem { PlanDate = planDate, Title = "First" };
        var secondTask = new TaskItem { PlanDate = planDate, Title = "Second" };
        _templateService.Setup(s => s.CreateTaskFromTemplateAsync(firstTemplateId, planDate, It.IsAny<CancellationToken>())).ReturnsAsync(firstTask);
        _templateService.Setup(s => s.CreateTaskFromTemplateAsync(secondTemplateId, planDate, It.IsAny<CancellationToken>())).ReturnsAsync(secondTask);

        var created = await _sut.CreateTasksFromGroupAsync(group.Id, planDate);

        Assert.Equal([firstTask, secondTask], created);
    }

    [Fact]
    public async Task CreateTasksFromGroupAsync_SkipsMemberTemplatesThatNoLongerExist()
    {
        var planDate = new DateOnly(2026, 8, 20);
        var missingTemplateId = Guid.NewGuid();
        var validTemplateId = Guid.NewGuid();
        var group = new TaskGroup { Name = "Routine", TemplateIds = [missingTemplateId, validTemplateId] };
        _groupRepository.Setup(r => r.GetByIdAsync(group.Id, It.IsAny<CancellationToken>())).ReturnsAsync(group);

        var validTask = new TaskItem { PlanDate = planDate, Title = "Valid" };
        _templateService.Setup(s => s.CreateTaskFromTemplateAsync(missingTemplateId, planDate, It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);
        _templateService.Setup(s => s.CreateTaskFromTemplateAsync(validTemplateId, planDate, It.IsAny<CancellationToken>())).ReturnsAsync(validTask);

        var created = await _sut.CreateTasksFromGroupAsync(group.Id, planDate);

        Assert.Equal([validTask], created);
    }

    [Fact]
    public async Task CreateTasksFromGroupAsync_WhenGroupMissing_ThrowsTaskGroupNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _groupRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((TaskGroup?)null);

        await Assert.ThrowsAsync<TaskGroupNotFoundException>(() => _sut.CreateTasksFromGroupAsync(missingId, new DateOnly(2026, 8, 20)));
    }
}
