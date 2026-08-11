using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Exceptions;
using Moq;

namespace DeskTodo.Tests.Application;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _sut = new ProjectService(_projectRepository.Object);
    }

    [Fact]
    public async Task GetProjectsAsync_DelegatesToRepository()
    {
        var project = new Project { Name = "Website Redesign", ColorHex = "#6366F1" };
        _projectRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([project]);

        var projects = await _sut.GetProjectsAsync();

        Assert.Single(projects);
    }

    [Fact]
    public async Task CreateProjectAsync_TrimsNameAndDescription_AndAdds()
    {
        var project = await _sut.CreateProjectAsync("  Website Redesign  ", "  Q3 site refresh  ", "#6366F1");

        Assert.Equal("Website Redesign", project.Name);
        Assert.Equal("Q3 site refresh", project.Description);
        Assert.Equal("#6366F1", project.ColorHex);
        _projectRepository.Verify(r => r.AddAsync(It.Is<Project>(p => p.Name == "Website Redesign"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProjectAsync_WithBlankDescription_StoresNull()
    {
        var project = await _sut.CreateProjectAsync("Website Redesign", "   ", "#6366F1");

        Assert.Null(project.Description);
    }

    [Fact]
    public async Task UpdateProjectAsync_UpdatesFields()
    {
        var project = new Project { Name = "Website Redesign", ColorHex = "#6366F1" };
        _projectRepository.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        await _sut.UpdateProjectAsync(project.Id, "Website Redesign v2", "Updated", "#0EA5E9");

        _projectRepository.Verify(r => r.UpdateAsync(
            It.Is<Project>(p => p.Name == "Website Redesign v2" && p.Description == "Updated" && p.ColorHex == "#0EA5E9"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProjectAsync_WhenProjectDoesNotExist_ThrowsProjectNotFoundException()
    {
        _projectRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

        await Assert.ThrowsAsync<ProjectNotFoundException>(() => _sut.UpdateProjectAsync(Guid.NewGuid(), "Name", null, "#000000"));
    }

    [Fact]
    public async Task SetArchivedAsync_TogglesIsArchived()
    {
        var project = new Project { Name = "Website Redesign", ColorHex = "#6366F1" };
        _projectRepository.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        await _sut.SetArchivedAsync(project.Id, true);

        _projectRepository.Verify(r => r.UpdateAsync(It.Is<Project>(p => p.IsArchived), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetArchivedAsync_WhenProjectDoesNotExist_ThrowsProjectNotFoundException()
    {
        _projectRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

        await Assert.ThrowsAsync<ProjectNotFoundException>(() => _sut.SetArchivedAsync(Guid.NewGuid(), true));
    }

    [Fact]
    public async Task DeleteProjectAsync_DelegatesToRepository()
    {
        var projectId = Guid.NewGuid();

        await _sut.DeleteProjectAsync(projectId);

        _projectRepository.Verify(r => r.DeleteAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
