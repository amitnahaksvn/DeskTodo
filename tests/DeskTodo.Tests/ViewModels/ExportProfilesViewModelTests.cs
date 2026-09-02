using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class ExportProfilesViewModelTests
{
    private readonly Mock<IExportProfileService> _profileService = new();
    private readonly Mock<IProjectRepository> _projectRepository = new();

    private ExportProfilesViewModel CreateSut()
    {
        _projectRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _profileService.Setup(s => s.GetProfilesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        return new ExportProfilesViewModel(_profileService.Object, _projectRepository.Object, NullLogger<ExportProfilesViewModel>.Instance);
    }

    [Fact]
    public async Task LoadAsync_PopulatesProjectOptionsWithAnAllProjectsEntryFirst()
    {
        var project = new Project { Name = "Website Redesign", ColorHex = "#4A90D9" };
        var sut = CreateSut();
        _projectRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([project]);

        await sut.LoadAsync();

        Assert.Equal(2, sut.ProjectOptions.Count);
        Assert.Null(sut.ProjectOptions[0].Id);
        Assert.Equal("All Projects", sut.ProjectOptions[0].Name);
        Assert.Equal(project.Id, sut.ProjectOptions[1].Id);
    }

    [Fact]
    public async Task LoadAsync_MapsProfileRowsWithTheirProjectName()
    {
        var project = new Project { Name = "Website Redesign", ColorHex = "#4A90D9" };
        var profile = new ExportProfile { Name = "Weekly Report", Format = ExportFormat.Csv, ProjectId = project.Id, DateRange = ExportDateRange.ThisWeek };
        var sut = CreateSut();
        _projectRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([project]);
        _profileService.Setup(s => s.GetProfilesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([profile]);

        await sut.LoadAsync();

        var row = Assert.Single(sut.Profiles);
        Assert.Equal("Website Redesign", row.ProjectName);
        Assert.Equal(ExportDateRange.ThisWeek, row.DateRange);
    }

    [Fact]
    public async Task LoadAsync_ForAProfileWithNoProject_ShowsAllProjects()
    {
        var profile = new ExportProfile { Name = "Everything", ProjectId = null };
        var sut = CreateSut();
        _profileService.Setup(s => s.GetProfilesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([profile]);

        await sut.LoadAsync();

        Assert.Equal("All Projects", Assert.Single(sut.Profiles).ProjectName);
    }

    [Fact]
    public async Task CreateProfileAsync_WhenNameIsBlank_SetsErrorMessage_WithoutCallingTheService()
    {
        var sut = CreateSut();
        sut.NewProfileName = "  ";

        await sut.CreateProfileCommand.ExecuteAsync(null);

        Assert.Equal("Enter a name for the profile.", sut.ErrorMessage);
        _profileService.Verify(s => s.CreateProfileAsync(It.IsAny<string>(), It.IsAny<ExportFormat>(), It.IsAny<Guid?>(), It.IsAny<ExportDateRange>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateProfileAsync_TrimsNameAndPassesTheSelectedProjectId()
    {
        var sut = CreateSut();
        sut.NewProfileName = "  Weekly Report  ";
        var projectId = Guid.NewGuid();
        sut.NewProfileProject = new ProjectOption(projectId, "Website Redesign");

        await sut.CreateProfileCommand.ExecuteAsync(null);

        _profileService.Verify(s => s.CreateProfileAsync("Weekly Report", It.IsAny<ExportFormat>(), projectId, It.IsAny<ExportDateRange>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(string.Empty, sut.NewProfileName);
    }

    [Fact]
    public async Task RunSelectedProfileAsync_WhenNoProfileSelected_SetsErrorMessage()
    {
        var sut = CreateSut();

        await sut.RunSelectedProfileAsync(new MemoryStream());

        Assert.Equal("Pick a profile to run.", sut.ErrorMessage);
    }

    [Fact]
    public async Task RunSelectedProfileAsync_OnSuccess_SetsAnInformativeStatusMessage()
    {
        var sut = CreateSut();
        var row = new ExportProfileRow(Guid.NewGuid(), "Weekly Report", ExportFormat.Csv, "All Projects", ExportDateRange.ThisWeek);
        sut.SelectedProfile = row;
        _profileService.Setup(s => s.RunProfileAsync(row.Id, It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync(7);

        await sut.RunSelectedProfileAsync(new MemoryStream());

        Assert.Contains("7 tasks", sut.StatusMessage);
        Assert.Contains("Weekly Report", sut.StatusMessage);
    }

    [Theory]
    [InlineData(ExportFormat.Csv, "csv")]
    [InlineData(ExportFormat.Json, "json")]
    [InlineData(ExportFormat.Markdown, "md")]
    [InlineData(ExportFormat.Excel, "xlsx")]
    public void SelectedProfileExtension_MatchesTheSelectedProfilesFormat(ExportFormat format, string expectedExtension)
    {
        var sut = CreateSut();
        sut.SelectedProfile = new ExportProfileRow(Guid.NewGuid(), "P", format, "All Projects", ExportDateRange.All);

        Assert.Equal(expectedExtension, sut.SelectedProfileExtension);
    }
}
