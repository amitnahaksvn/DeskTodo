using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Moq;

namespace DeskTodo.Tests.Application;

public class WebhookServiceTests
{
    private readonly Mock<IWebhookRepository> _webhookRepository = new();
    private readonly Mock<IWebhookDeliveryLogRepository> _deliveryLogRepository = new();
    private readonly Mock<IWebhookDeliveryClient> _deliveryClient = new();
    private readonly WebhookService _sut;

    public WebhookServiceTests()
    {
        _sut = new WebhookService(_webhookRepository.Object, _deliveryLogRepository.Object, _deliveryClient.Object);
    }

    [Fact]
    public async Task CreateWebhookAsync_TrimsFields_AndAdds()
    {
        var webhook = await _sut.CreateWebhookAsync("  Slack  ", "  https://example.com/hook  ", ["TaskCreated"], "  secret  ");

        Assert.Equal("Slack", webhook.Name);
        Assert.Equal("https://example.com/hook", webhook.Url);
        Assert.Equal("secret", webhook.Secret);
        Assert.Equal(["TaskCreated"], webhook.EventTypes);
        _webhookRepository.Verify(r => r.AddAsync(webhook, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateWebhookAsync_WithABlankSecret_StoresNull()
    {
        var webhook = await _sut.CreateWebhookAsync("Slack", "https://example.com/hook", ["TaskCreated"], "   ");

        Assert.Null(webhook.Secret);
    }

    [Fact]
    public async Task DeleteWebhookAsync_DelegatesToTheRepository()
    {
        var id = Guid.NewGuid();

        await _sut.DeleteWebhookAsync(id);

        _webhookRepository.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendTestDeliveryAsync_WithAnUnknownWebhook_Throws()
    {
        _webhookRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((WebhookSubscription?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SendTestDeliveryAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SendTestDeliveryAsync_DeliversATestEventToTheWebhook()
    {
        var webhook = new WebhookSubscription { Name = "Slack", Url = "https://example.com/hook", EventTypes = ["TaskCreated"] };
        _webhookRepository.Setup(r => r.GetByIdAsync(webhook.Id, It.IsAny<CancellationToken>())).ReturnsAsync(webhook);
        var expectedLog = new WebhookDeliveryLog { WebhookId = webhook.Id, EventType = "Test", Success = true };
        _deliveryClient.Setup(c => c.DeliverAsync(webhook, "Test", Guid.Empty, null, It.IsAny<CancellationToken>())).ReturnsAsync(expectedLog);

        var result = await _sut.SendTestDeliveryAsync(webhook.Id);

        Assert.Equal(expectedLog, result);
    }

    [Fact]
    public async Task GetDeliveryHistoryAsync_DelegatesToTheRepository()
    {
        var webhookId = Guid.NewGuid();

        await _sut.GetDeliveryHistoryAsync(webhookId);

        _deliveryLogRepository.Verify(r => r.GetForWebhookAsync(webhookId, 20, It.IsAny<CancellationToken>()), Times.Once);
    }
}
