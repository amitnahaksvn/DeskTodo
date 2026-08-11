using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class ProjectsViewModelTests
{
    private readonly Mock<IProjectService> _projectService = new();
    private readonly ProjectsViewModel _sut;

    public ProjectsViewModelTests()
    {
        _sut = new ProjectsViewModel(_projectService.Object, NullLogger<ProjectsViewModel>.Instance);
    }

    private static Project CreateProject(string name, bool isArchived = false, int totalTasks = 0, int completedTasks = 0)
    {
        var project = new Project { Name = name, ColorHex = "#6366F1", IsArchived = isArchived };
        for (var i = 0; i < totalTasks; i++)
        {
            var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = $"Task {i}" };
            if (i < completedTasks)
            {
                task.Complete();
            }

            project.Tasks.Add(task);
        }

        return project;
    }

    [Fact]
    public async Task LoadAsync_WithNoProjects_SetsTheEmptyFlag()
    {
        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.LoadAsync();

        Assert.True(_sut.HasNoProjects);
        Assert.Empty(_sut.Projects);
    }

    [Fact]
    public async Task LoadAsync_PopulatesDisplayFieldsAndTaskProgress()
    {
        var project = CreateProject("Website Redesign", totalTasks: 3, completedTasks: 1);
        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([project]);

        await _sut.LoadAsync();

        Assert.False(_sut.HasNoProjects);
        var row = Assert.Single(_sut.Projects);
        Assert.Equal("Website Redesign", row.Name);
        Assert.Equal("1/3 tasks done", row.ProgressDisplay);
        Assert.Equal("Archive", row.ToggleButtonLabel);
    }

    [Fact]
    public async Task LoadAsync_WithNoLinkedTasks_ShowsFallbackText()
    {
        var project = CreateProject("Website Redesign");
        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([project]);

        await _sut.LoadAsync();

        Assert.Equal("No linked tasks", _sut.Projects[0].ProgressDisplay);
    }

    [Fact]
    public async Task LoadAsync_WithAnArchivedProject_ShowsUnarchiveLabel()
    {
        var project = CreateProject("Old Project", isArchived: true);
        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([project]);

        await _sut.LoadAsync();

        Assert.Equal("Unarchive", _sut.Projects[0].ToggleButtonLabel);
    }

    [Fact]
    public async Task AddProjectAsync_WithAName_CreatesItAndReloads()
    {
        _projectService.SetupSequence(s => s.GetProjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([])
            .ReturnsAsync([CreateProject("Website Redesign")]);
        await _sut.LoadAsync();
        _sut.NewProjectName = "  Website Redesign  ";

        await _sut.AddProjectCommand.ExecuteAsync(null);

        _projectService.Verify(s => s.CreateProjectAsync("Website Redesign", null, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(string.Empty, _sut.NewProjectName);
        Assert.Single(_sut.Projects);
    }

    [Fact]
    public async Task AddProjectAsync_WithABlankName_DoesNotCreateAnything()
    {
        await _sut.AddProjectCommand.ExecuteAsync(null);

        _projectService.Verify(s => s.CreateProjectAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToggleArchivedCommand_TogglesArchivedState()
    {
        var project = CreateProject("Website Redesign");
        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([project]);
        await _sut.LoadAsync();
        var row = _sut.Projects[0];

        await row.ToggleArchivedCommand.ExecuteAsync(null);

        _projectService.Verify(s => s.SetArchivedAsync(project.Id, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCommand_DeletesTheProject()
    {
        var project = CreateProject("Website Redesign");
        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([project]);
        await _sut.LoadAsync();
        var row = _sut.Projects[0];

        await row.DeleteCommand.ExecuteAsync(null);

        _projectService.Verify(s => s.DeleteProjectAsync(project.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
