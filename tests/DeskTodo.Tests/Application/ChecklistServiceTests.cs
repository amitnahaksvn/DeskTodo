using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Moq;

namespace DeskTodo.Tests.Application;

public class ChecklistServiceTests
{
    private readonly Mock<IChecklistRepository> _checklistRepository = new();
    private readonly ChecklistService _sut;

    public ChecklistServiceTests()
    {
        _sut = new ChecklistService(_checklistRepository.Object);
    }

    [Fact]
    public async Task AddItemAsync_AssignsNextOrder_AndAdds()
    {
        var taskId = Guid.NewGuid();
        _checklistRepository.Setup(r => r.GetMaxOrderAsync(taskId, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var item = await _sut.AddItemAsync(taskId, "Buy milk");

        Assert.NotNull(item);
        Assert.Equal(3, item.Order);
        Assert.Equal("Buy milk", item.Text);
        _checklistRepository.Verify(r => r.AddAsync(It.Is<ChecklistItem>(i => i == item), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_OnBlankText_ReturnsNullAndDoesNotAdd()
    {
        var item = await _sut.AddItemAsync(Guid.NewGuid(), "   ");

        Assert.Null(item);
        _checklistRepository.Verify(r => r.AddAsync(It.IsAny<ChecklistItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToggleItemAsync_FlipsIsChecked()
    {
        var item = new ChecklistItem { TaskId = Guid.NewGuid(), Text = "Pack bags", IsChecked = false };
        _checklistRepository.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        await _sut.ToggleItemAsync(item.Id);

        Assert.True(item.IsChecked);
        _checklistRepository.Verify(r => r.UpdateAsync(item, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleItemAsync_WhenItemMissing_DoesNotThrowOrUpdate()
    {
        _checklistRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((ChecklistItem?)null);

        await _sut.ToggleItemAsync(Guid.NewGuid());

        _checklistRepository.Verify(r => r.UpdateAsync(It.IsAny<ChecklistItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveItemAsync_DelegatesToRepository()
    {
        var itemId = Guid.NewGuid();

        await _sut.RemoveItemAsync(itemId);

        _checklistRepository.Verify(r => r.DeleteAsync(itemId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
