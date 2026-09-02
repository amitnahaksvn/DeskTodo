using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Moq;

namespace DeskTodo.Tests.Application;

public class DecisionServiceTests
{
    private readonly Mock<IDecisionRepository> _decisionRepository = new();
    private readonly DecisionService _sut;

    public DecisionServiceTests()
    {
        _sut = new DecisionService(_decisionRepository.Object);
    }

    [Fact]
    public async Task RecordDecisionAsync_TrimsFields_AndAdds()
    {
        var decision = await _sut.RecordDecisionAsync("  Use PostgreSQL  ", "  Context  ", "  Decision  ", "  Alt  ", "  Reason  ", null);

        Assert.Equal("Use PostgreSQL", decision.Title);
        Assert.Equal("Context", decision.Context);
        Assert.Equal("Decision", decision.DecisionText);
        Assert.Equal("Alt", decision.Alternatives);
        Assert.Equal("Reason", decision.Reason);
        _decisionRepository.Verify(r => r.AddAsync(decision, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordDecisionAsync_WithBlankOptionalFields_StoresNull()
    {
        var decision = await _sut.RecordDecisionAsync("Title", "   ", "Decision", "   ", "   ", null);

        Assert.Null(decision.Context);
        Assert.Null(decision.Alternatives);
        Assert.Null(decision.Reason);
    }

    [Fact]
    public async Task GetDecisionsAsync_DelegatesToTheRepository()
    {
        var decision = new Decision { Title = "X", DecisionText = "Y" };
        _decisionRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([decision]);

        var results = await _sut.GetDecisionsAsync();

        Assert.Equal([decision], results);
    }

    [Fact]
    public async Task DeleteDecisionAsync_DelegatesToTheRepository()
    {
        var decisionId = Guid.NewGuid();

        await _sut.DeleteDecisionAsync(decisionId);

        _decisionRepository.Verify(r => r.DeleteAsync(decisionId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
