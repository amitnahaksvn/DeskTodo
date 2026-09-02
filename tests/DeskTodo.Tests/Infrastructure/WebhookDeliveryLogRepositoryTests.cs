using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class WebhookDeliveryLogRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly WebhookDeliveryLogRepository _sut;
    private readonly WebhookRepository _webhookRepository;

    public WebhookDeliveryLogRepositoryTests()
    {
        _sut = new WebhookDeliveryLogRepository(_fixture.ContextFactory);
        _webhookRepository = new WebhookRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    private async Task<WebhookSubscription> CreateWebhookAsync()
    {
        var webhook = new WebhookSubscription { Name = "Notifier", Url = "https://example.com/hook", EventTypes = ["TaskCreated"] };
        await _webhookRepository.AddAsync(webhook);
        return webhook;
    }

    [Fact]
    public async Task AddAsync_ThenGetForWebhookAsync_ReturnsMostRecentFirst()
    {
        var webhook = await CreateWebhookAsync();
        var older = new WebhookDeliveryLog { WebhookId = webhook.Id, EventType = "TaskCreated", Success = true, AttemptedAt = DateTime.UtcNow.AddMinutes(-5) };
        var newer = new WebhookDeliveryLog { WebhookId = webhook.Id, EventType = "TaskCreated", Success = false, ErrorMessage = "timeout", AttemptedAt = DateTime.UtcNow };
        await _sut.AddAsync(older);
        await _sut.AddAsync(newer);

        var results = await _sut.GetForWebhookAsync(webhook.Id);

        Assert.Equal(2, results.Count);
        Assert.Equal(newer.Id, results[0].Id);
        Assert.Equal(older.Id, results[1].Id);
    }

    [Fact]
    public async Task GetForWebhookAsync_DoesNotIncludeLogsForOtherWebhooks()
    {
        var webhookA = await CreateWebhookAsync();
        var webhookB = await CreateWebhookAsync();
        await _sut.AddAsync(new WebhookDeliveryLog { WebhookId = webhookA.Id, EventType = "TaskCreated", Success = true });
        await _sut.AddAsync(new WebhookDeliveryLog { WebhookId = webhookB.Id, EventType = "TaskCreated", Success = true });

        var results = await _sut.GetForWebhookAsync(webhookA.Id);

        Assert.Single(results);
    }

    [Fact]
    public async Task GetForWebhookAsync_RespectsTheLimit()
    {
        var webhook = await CreateWebhookAsync();
        for (var i = 0; i < 5; i++)
        {
            await _sut.AddAsync(new WebhookDeliveryLog { WebhookId = webhook.Id, EventType = "TaskCreated", Success = true });
        }

        var results = await _sut.GetForWebhookAsync(webhook.Id, limit: 3);

        Assert.Equal(3, results.Count);
    }
}
