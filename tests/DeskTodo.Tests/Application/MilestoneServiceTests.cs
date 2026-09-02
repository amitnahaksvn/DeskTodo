using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Exceptions;
using Moq;

namespace DeskTodo.Tests.Application;

public class MilestoneServiceTests
{
    private readonly Mock<IMilestoneRepository> _milestoneRepository = new();
    private readonly MilestoneService _sut;

    public MilestoneServiceTests()
    {
        _sut = new MilestoneService(_milestoneRepository.Object);
    }

    [Fact]
    public async Task GetMilestonesAsync_DelegatesToRepository()
    {
        var milestone = new Milestone { Title = "Ship v1" };
        _milestoneRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([milestone]);

        var milestones = await _sut.GetMilestonesAsync();

        Assert.Single(milestones);
    }

    [Fact]
    public async Task CreateMilestoneAsync_TrimsTitleAndDescription_AndAdds()
    {
        var targetDate = new DateOnly(2026, 9, 1);

        var milestone = await _sut.CreateMilestoneAsync("  Ship v1  ", "  First release  ", targetDate);

        Assert.Equal("Ship v1", milestone.Title);
        Assert.Equal("First release", milestone.Description);
        Assert.Equal(targetDate, milestone.TargetDate);
        _milestoneRepository.Verify(r => r.AddAsync(It.Is<Milestone>(m => m.Title == "Ship v1"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateMilestoneAsync_WithBlankDescription_StoresNull()
    {
        var milestone = await _sut.CreateMilestoneAsync("Ship v1", "   ", null);

        Assert.Null(milestone.Description);
    }

    [Fact]
    public async Task CreateMilestoneAsync_WithNoProjectId_LeavesOrderAtZero()
    {
        var milestone = await _sut.CreateMilestoneAsync("Standalone", null, null);

        Assert.Null(milestone.ProjectId);
        Assert.Equal(0, milestone.Order);
    }

    [Fact]
    public async Task CreateMilestoneAsync_WithAProjectId_AppendsToTheEndOfThatProjectsOrder()
    {
        var projectId = Guid.NewGuid();
        var otherProjectId = Guid.NewGuid();
        _milestoneRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new Milestone { Title = "First", ProjectId = projectId, Order = 0 },
            new Milestone { Title = "Second", ProjectId = projectId, Order = 1 },
            new Milestone { Title = "Unrelated", ProjectId = otherProjectId, Order = 5 },
        ]);

        var milestone = await _sut.CreateMilestoneAsync("Third", null, null, projectId);

        Assert.Equal(projectId, milestone.ProjectId);
        Assert.Equal(2, milestone.Order);
    }

    [Fact]
    public async Task CreateMilestoneAsync_AsTheFirstMilestoneInAProject_StartsAtOrderZero()
    {
        var projectId = Guid.NewGuid();
        _milestoneRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var milestone = await _sut.CreateMilestoneAsync("First", null, null, projectId);

        Assert.Equal(0, milestone.Order);
    }

    [Fact]
    public async Task UpdateMilestoneAsync_UpdatesFields()
    {
        var milestone = new Milestone { Title = "Ship v1" };
        _milestoneRepository.Setup(r => r.GetByIdAsync(milestone.Id, It.IsAny<CancellationToken>())).ReturnsAsync(milestone);
        var newTargetDate = new DateOnly(2026, 10, 1);

        await _sut.UpdateMilestoneAsync(milestone.Id, "Ship v1.1", "Updated", newTargetDate);

        _milestoneRepository.Verify(r => r.UpdateAsync(
            It.Is<Milestone>(m => m.Title == "Ship v1.1" && m.Description == "Updated" && m.TargetDate == newTargetDate),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateMilestoneAsync_WhenMilestoneDoesNotExist_ThrowsMilestoneNotFoundException()
    {
        _milestoneRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Milestone?)null);

        await Assert.ThrowsAsync<MilestoneNotFoundException>(() => _sut.UpdateMilestoneAsync(Guid.NewGuid(), "Title", null, null));
    }

    [Fact]
    public async Task SetCompletedAsync_TogglesIsCompleted()
    {
        var milestone = new Milestone { Title = "Ship v1" };
        _milestoneRepository.Setup(r => r.GetByIdAsync(milestone.Id, It.IsAny<CancellationToken>())).ReturnsAsync(milestone);

        await _sut.SetCompletedAsync(milestone.Id, true);

        _milestoneRepository.Verify(r => r.UpdateAsync(It.Is<Milestone>(m => m.IsCompleted), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetCompletedAsync_WhenMilestoneDoesNotExist_ThrowsMilestoneNotFoundException()
    {
        _milestoneRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Milestone?)null);

        await Assert.ThrowsAsync<MilestoneNotFoundException>(() => _sut.SetCompletedAsync(Guid.NewGuid(), true));
    }

    [Fact]
    public async Task DeleteMilestoneAsync_DelegatesToRepository()
    {
        var milestoneId = Guid.NewGuid();

        await _sut.DeleteMilestoneAsync(milestoneId);

        _milestoneRepository.Verify(r => r.DeleteAsync(milestoneId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
