using DeskTodo.App.ViewModels;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class WebhooksViewModelTests
{
    private readonly Mock<IWebhookService> _webhookService = new();
    private readonly WebhooksViewModel _sut;

    public WebhooksViewModelTests()
    {
        _sut = new WebhooksViewModel(_webhookService.Object, NullLogger<WebhooksViewModel>.Instance);
    }

    [Fact]
    public async Task LoadAsync_PopulatesTheWebhookList()
    {
        var webhook = new WebhookSubscription { Name = "Slack", Url = "https://example.com/hook", EventTypes = ["TaskCreated", "TaskCompleted"] };
        _webhookService.Setup(s => s.GetWebhooksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([webhook]);

        await _sut.LoadAsync();

        var row = Assert.Single(_sut.Webhooks);
        Assert.Equal("Slack", row.Name);
        Assert.Equal("TaskCreated, TaskCompleted", row.EventTypesDisplay);
    }

    [Fact]
    public async Task AddWebhookCommand_WithMissingFields_SetsAStatusMessage_AndDoesNotCreate()
    {
        _sut.NewWebhookName = "Slack";
        _sut.NewWebhookUrl = string.Empty;

        await _sut.AddWebhookCommand.ExecuteAsync(null);

        Assert.NotEmpty(_sut.StatusMessage);
        _webhookService.Verify(s => s.CreateWebhookAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddWebhookCommand_WithAnInvalidUrl_SetsAStatusMessage_AndDoesNotCreate()
    {
        _sut.NewWebhookName = "Slack";
        _sut.NewWebhookUrl = "not a url";
        _sut.NewWebhookEventTypeOptions[0].IsSelected = true;

        await _sut.AddWebhookCommand.ExecuteAsync(null);

        Assert.NotEmpty(_sut.StatusMessage);
        _webhookService.Verify(s => s.CreateWebhookAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddWebhookCommand_WithValidFields_CreatesTheWebhook_AndReloads()
    {
        _webhookService.Setup(s => s.GetWebhooksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _sut.NewWebhookName = "Slack";
        _sut.NewWebhookUrl = "https://example.com/hook";
        _sut.NewWebhookEventTypeOptions[0].IsSelected = true;

        await _sut.AddWebhookCommand.ExecuteAsync(null);

        _webhookService.Verify(s => s.CreateWebhookAsync("Slack", "https://example.com/hook",
            It.Is<IReadOnlyList<string>>(types => types.Count == 1), null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(string.Empty, _sut.NewWebhookName);
        Assert.False(_sut.NewWebhookEventTypeOptions[0].IsSelected);
    }

    [Fact]
    public async Task DeleteWebhookCommand_DelegatesToTheService_AndReloads()
    {
        _webhookService.Setup(s => s.GetWebhooksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var row = new WebhookRow(Guid.NewGuid(), "Slack", "https://example.com/hook", "TaskCreated", true, false, 0, "Never delivered");

        await _sut.DeleteWebhookCommand.ExecuteAsync(row);

        _webhookService.Verify(s => s.DeleteWebhookAsync(row.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendTestCommand_ReportsSuccess()
    {
        _webhookService.Setup(s => s.GetWebhooksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var row = new WebhookRow(Guid.NewGuid(), "Slack", "https://example.com/hook", "TaskCreated", true, false, 0, "Never delivered");
        _webhookService.Setup(s => s.SendTestDeliveryAsync(row.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebhookDeliveryLog { WebhookId = row.Id, EventType = "Test", Success = true, StatusCode = 200 });

        await _sut.SendTestCommand.ExecuteAsync(row);

        Assert.Contains("200", _sut.StatusMessage);
    }

    [Fact]
    public async Task SendTestCommand_ReportsFailure()
    {
        _webhookService.Setup(s => s.GetWebhooksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var row = new WebhookRow(Guid.NewGuid(), "Slack", "https://example.com/hook", "TaskCreated", true, false, 0, "Never delivered");
        _webhookService.Setup(s => s.SendTestDeliveryAsync(row.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebhookDeliveryLog { WebhookId = row.Id, EventType = "Test", Success = false, ErrorMessage = "Connection refused" });

        await _sut.SendTestCommand.ExecuteAsync(row);

        Assert.Contains("Connection refused", _sut.StatusMessage);
    }
}
