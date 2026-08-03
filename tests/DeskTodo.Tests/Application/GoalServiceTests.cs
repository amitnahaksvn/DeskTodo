using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Exceptions;
using Moq;

namespace DeskTodo.Tests.Application;

public class GoalServiceTests
{
    private readonly Mock<IGoalRepository> _goalRepository = new();
    private readonly GoalService _sut;

    public GoalServiceTests()
    {
        _sut = new GoalService(_goalRepository.Object);
    }

    [Fact]
    public async Task GetGoalsAsync_ByDefault_ExcludesArchivedGoals()
    {
        var active = new Goal { Name = "Active" };
        var archived = new Goal { Name = "Archived", IsArchived = true };
        _goalRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([active, archived]);

        var goals = await _sut.GetGoalsAsync();

        Assert.Single(goals);
        Assert.Equal("Active", goals[0].Name);
    }

    [Fact]
    public async Task GetGoalsAsync_WithIncludeArchivedTrue_ReturnsEverything()
    {
        var active = new Goal { Name = "Active" };
        var archived = new Goal { Name = "Archived", IsArchived = true };
        _goalRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([active, archived]);

        var goals = await _sut.GetGoalsAsync(includeArchived: true);

        Assert.Equal(2, goals.Count);
    }

    [Fact]
    public async Task CreateGoalAsync_TrimsNameAndDescription_AndAdds()
    {
        var goal = await _sut.CreateGoalAsync("  Meditate  ", "  Daily  ");

        Assert.Equal("Meditate", goal.Name);
        Assert.Equal("Daily", goal.Description);
        _goalRepository.Verify(r => r.AddAsync(It.Is<Goal>(g => g.Name == "Meditate"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateGoalAsync_WithBlankDescription_StoresNull()
    {
        var goal = await _sut.CreateGoalAsync("Read more", "   ");

        Assert.Null(goal.Description);
    }

    [Fact]
    public async Task ArchiveGoalAsync_SetsIsArchived()
    {
        var goal = new Goal { Name = "Exercise" };
        _goalRepository.Setup(r => r.GetByIdAsync(goal.Id, It.IsAny<CancellationToken>())).ReturnsAsync(goal);

        await _sut.ArchiveGoalAsync(goal.Id);

        _goalRepository.Verify(r => r.UpdateAsync(It.Is<Goal>(g => g.IsArchived), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ArchiveGoalAsync_WhenGoalDoesNotExist_ThrowsGoalNotFoundException()
    {
        _goalRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Goal?)null);

        await Assert.ThrowsAsync<GoalNotFoundException>(() => _sut.ArchiveGoalAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UnarchiveGoalAsync_ClearsIsArchived()
    {
        var goal = new Goal { Name = "Exercise", IsArchived = true };
        _goalRepository.Setup(r => r.GetByIdAsync(goal.Id, It.IsAny<CancellationToken>())).ReturnsAsync(goal);

        await _sut.UnarchiveGoalAsync(goal.Id);

        _goalRepository.Verify(r => r.UpdateAsync(It.Is<Goal>(g => !g.IsArchived), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteGoalAsync_DelegatesToRepository()
    {
        var goalId = Guid.NewGuid();

        await _sut.DeleteGoalAsync(goalId);

        _goalRepository.Verify(r => r.DeleteAsync(goalId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkCompletedAsync_DelegatesToRepository()
    {
        var goalId = Guid.NewGuid();
        var date = new DateOnly(2026, 8, 15);

        await _sut.MarkCompletedAsync(goalId, date);

        _goalRepository.Verify(r => r.AddCompletionAsync(goalId, date, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnmarkCompletedAsync_DelegatesToRepository()
    {
        var goalId = Guid.NewGuid();
        var date = new DateOnly(2026, 8, 15);

        await _sut.UnmarkCompletedAsync(goalId, date);

        _goalRepository.Verify(r => r.RemoveCompletionAsync(goalId, date, It.IsAny<CancellationToken>()), Times.Once);
    }
}
