using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Moq;

namespace DeskTodo.Tests.Application;

public class TagServiceTests
{
    private readonly Mock<ITagRepository> _tagRepository = new();
    private readonly TagService _sut;

    public TagServiceTests()
    {
        _sut = new TagService(_tagRepository.Object);
    }

    [Fact]
    public async Task AssignTagAsync_GetsOrCreatesTheTag_ThenAssignsIt()
    {
        var taskId = Guid.NewGuid();
        var tag = new Tag { Name = "Urgent" };
        _tagRepository.Setup(r => r.GetOrCreateByNameAsync("Urgent", It.IsAny<CancellationToken>())).ReturnsAsync(tag);

        await _sut.AssignTagAsync(taskId, "  Urgent  ");

        _tagRepository.Verify(r => r.GetOrCreateByNameAsync("Urgent", It.IsAny<CancellationToken>()), Times.Once);
        _tagRepository.Verify(r => r.AssignToTaskAsync(taskId, tag.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignTagAsync_OnBlankName_DoesNothing()
    {
        await _sut.AssignTagAsync(Guid.NewGuid(), "   ");

        _tagRepository.Verify(r => r.GetOrCreateByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveTagAsync_DelegatesToRepository()
    {
        var taskId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        await _sut.RemoveTagAsync(taskId, tagId);

        _tagRepository.Verify(r => r.RemoveFromTaskAsync(taskId, tagId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTagAsync_DelegatesToRepository()
    {
        var tagId = Guid.NewGuid();

        await _sut.DeleteTagAsync(tagId);

        _tagRepository.Verify(r => r.DeleteAsync(tagId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
