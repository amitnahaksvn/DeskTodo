using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class TaskGroupViewModelTests
{
    private readonly Mock<ITaskGroupService> _groupService = new();
    private readonly Mock<ITaskTemplateService> _templateService = new();
    private readonly TaskGroupViewModel _sut;

    public TaskGroupViewModelTests()
    {
        _groupService.Setup(s => s.GetGroupsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _templateService.Setup(s => s.GetTemplatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _sut = new TaskGroupViewModel(_groupService.Object, _templateService.Object, NullLogger<TaskGroupViewModel>.Instance);
    }

    private static TaskTemplate MakeTemplate(string name) => new() { Name = name, TaskTitle = name };

    [Fact]
    public async Task LoadAsync_PopulatesAvailableTemplatesAndGroups()
    {
        var template = MakeTemplate("Meditate");
        _templateService.Setup(s => s.GetTemplatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([template]);
        var group = new TaskGroup { Name = "Morning", TemplateIds = [template.Id] };
        _groupService.Setup(s => s.GetGroupsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([group]);

        await _sut.LoadAsync();

        Assert.Single(_sut.AvailableTemplates);
        Assert.Equal("Meditate", _sut.AvailableTemplates[0].Name);
        Assert.Single(_sut.Groups);
        Assert.Equal("Morning", _sut.Groups[0].Name);
        Assert.Equal("Meditate", _sut.Groups[0].MemberSummary);
    }

    [Fact]
    public async Task CreateGroupAsync_WithNoNameEntered_SetsAnErrorAndDoesNotCallTheService()
    {
        await _sut.LoadAsync();
        _sut.NewGroupName = "   ";

        await _sut.CreateGroupCommand.ExecuteAsync(null);

        Assert.False(string.IsNullOrEmpty(_sut.ErrorMessage));
        _groupService.Verify(s => s.CreateGroupAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateGroupAsync_WithNoTemplatesSelected_SetsAnErrorAndDoesNotCallTheService()
    {
        _templateService.Setup(s => s.GetTemplatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([MakeTemplate("Meditate")]);
        await _sut.LoadAsync();
        _sut.NewGroupName = "Morning";

        await _sut.CreateGroupCommand.ExecuteAsync(null);

        Assert.False(string.IsNullOrEmpty(_sut.ErrorMessage));
        _groupService.Verify(s => s.CreateGroupAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateGroupAsync_WithASelectedTemplate_CreatesTheGroup_AndResetsTheForm()
    {
        var template = MakeTemplate("Meditate");
        _templateService.Setup(s => s.GetTemplatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([template]);
        await _sut.LoadAsync();
        _sut.NewGroupName = "Morning";
        _sut.AvailableTemplates[0].IsSelected = true;
        _groupService.Setup(s => s.CreateGroupAsync("Morning", It.Is<IReadOnlyList<Guid>>(ids => ids.SequenceEqual(new[] { template.Id })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskGroup { Name = "Morning", TemplateIds = [template.Id] });

        await _sut.CreateGroupCommand.ExecuteAsync(null);

        _groupService.Verify(s => s.CreateGroupAsync("Morning", It.Is<IReadOnlyList<Guid>>(ids => ids.SequenceEqual(new[] { template.Id })), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(string.Empty, _sut.NewGroupName);
        Assert.False(_sut.AvailableTemplates[0].IsSelected);
    }

    [Fact]
    public async Task DeleteGroupCommand_DelegatesToTheService_AndRefreshesTheList()
    {
        var groupId = Guid.NewGuid();

        await _sut.DeleteGroupCommand.ExecuteAsync(groupId);

        _groupService.Verify(s => s.DeleteGroupAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
        _groupService.Verify(s => s.GetGroupsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyGroupCommand_WithNoTasksCreated_SetsAnError()
    {
        var groupId = Guid.NewGuid();
        _groupService.Setup(s => s.CreateTasksFromGroupAsync(groupId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.ApplyGroupCommand.ExecuteAsync(groupId);

        Assert.False(string.IsNullOrEmpty(_sut.ErrorMessage));
    }

    [Fact]
    public async Task ApplyGroupCommand_UsesApplyDateAsThePlanDate()
    {
        var groupId = Guid.NewGuid();
        _sut.ApplyDate = new DateTimeOffset(2026, 8, 25, 0, 0, 0, TimeSpan.Zero);
        _groupService.Setup(s => s.CreateTasksFromGroupAsync(groupId, new DateOnly(2026, 8, 25), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TaskItem { PlanDate = new DateOnly(2026, 8, 25), Title = "Created" }]);

        await _sut.ApplyGroupCommand.ExecuteAsync(groupId);

        _groupService.Verify(s => s.CreateTasksFromGroupAsync(groupId, new DateOnly(2026, 8, 25), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(string.Empty, _sut.ErrorMessage);
    }
}
