using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class QuickAddViewModelTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly QuickAddViewModel _sut;

    public QuickAddViewModelTests()
    {
        _categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _sut = new QuickAddViewModel(
            _taskService.Object,
            _categoryRepository.Object,
            new RuleBasedQuickAddParser(),
            TimeProvider.System,
            NullLogger<QuickAddViewModel>.Instance);
    }

    [Fact]
    public async Task LoadAsync_ResetsTitleAndPriority_AndPopulatesCategoriesWithNoneFirst()
    {
        _sut.Title = "leftover from last time";
        _sut.Priority = TaskPriority.High;
        var work = new Category { Name = "Work", ColorHex = "#3B82F6" };
        var home = new Category { Name = "Home", ColorHex = "#22C55E" };
        _categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([home, work]);

        await _sut.LoadAsync();

        Assert.Equal(string.Empty, _sut.Title);
        Assert.Equal(TaskPriority.Medium, _sut.Priority);
        Assert.Equal(["No category", "Home", "Work"], _sut.Categories.Select(c => c.Name));
        Assert.Equal(CategoryOption.None, _sut.SelectedCategory);
    }

    [Fact]
    public async Task AddCommand_WithABlankTitle_DoesNotCreateATaskOrRaiseClosed()
    {
        _sut.Title = "   ";
        var closedRaised = false;
        _sut.Closed += (_, _) => closedRaised = true;

        await _sut.AddCommand.ExecuteAsync(null);

        _taskService.Verify(s => s.CreateTaskAsync(
            It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TaskPriority>(),
            It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.False(closedRaised);
    }

    [Fact]
    public async Task AddCommand_WithATitle_CreatesATaskOnTodayWithThePriorityAndCategory_AndRaisesClosed()
    {
        var category = new Category { Name = "Work", ColorHex = "#3B82F6" };
        _sut.Title = "  Ship the release  ";
        _sut.Priority = TaskPriority.High;
        _sut.SelectedCategory = new CategoryOption(category.Id, category.Name, category.ColorHex);
        var closedRaised = false;
        _sut.Closed += (_, _) => closedRaised = true;

        await _sut.AddCommand.ExecuteAsync(null);

        var today = DateOnly.FromDateTime(DateTime.Now);
        _taskService.Verify(s => s.CreateTaskAsync(
            today, "Ship the release", null, TaskPriority.High, category.Id, null, null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(closedRaised);
    }

    [Fact]
    public void CancelCommand_RaisesClosedWithoutCreatingATask()
    {
        _sut.Title = "Something";
        var closedRaised = false;
        _sut.Closed += (_, _) => closedRaised = true;

        _sut.CancelCommand.Execute(null);

        _taskService.Verify(s => s.CreateTaskAsync(
            It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TaskPriority>(),
            It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.True(closedRaised);
    }
}
