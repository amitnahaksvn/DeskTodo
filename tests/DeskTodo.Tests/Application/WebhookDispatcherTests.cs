using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Events;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.Application;

/// <summary>
/// <see cref="WebhookDispatcher"/> resolves its per-event dependencies through a real
/// <see cref="IServiceScopeFactory"/> (not injecting them directly — see that class's own remark
/// on why a singleton can't hold scoped dependencies), so these tests build a real
/// <see cref="ServiceProvider"/> with mocked repository/client registered, rather than mocking
/// the scope machinery itself.
/// </summary>
public class WebhookDispatcherTests
{
    private static (WebhookDispatcher Dispatcher, InMemoryEventBus EventBus, Mock<IWebhookRepository> Repository, Mock<IWebhookDeliveryClient> Client) CreateSut()
    {
        var repository = new Mock<IWebhookRepository>();
        var client = new Mock<IWebhookDeliveryClient>();

        var services = new ServiceCollection();
        services.AddScoped(_ => repository.Object);
        services.AddScoped(_ => client.Object);
        var provider = services.BuildServiceProvider();

        var eventBus = new InMemoryEventBus(NullLogger<InMemoryEventBus>.Instance);
        var dispatcher = new WebhookDispatcher(eventBus, provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<WebhookDispatcher>.Instance);
        return (dispatcher, eventBus, repository, client);
    }

    [Fact]
    public async Task Start_ThenAnEventIsPublished_DeliversToEveryMatchingEnabledWebhook()
    {
        var (dispatcher, eventBus, repository, client) = CreateSut();
        var webhook = new WebhookSubscription { Name = "Notifier", Url = "https://example.com/hook", EventTypes = ["TaskCompleted"] };
        repository.Setup(r => r.GetEnabledForEventTypeAsync("TaskCompleted", It.IsAny<CancellationToken>())).ReturnsAsync([webhook]);
        var delivered = new TaskCompletionSource();
        client.Setup(c => c.DeliverAsync(webhook, "TaskCompleted", It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                delivered.TrySetResult();
                return new WebhookDeliveryLog { WebhookId = webhook.Id, EventType = "TaskCompleted", Success = true };
            });

        dispatcher.Start();
        var entityId = Guid.NewGuid();
        eventBus.Publish(new ApplicationEvent("TaskCompleted", entityId, DateTime.UtcNow, "Test", PayloadJson: null));

        await delivered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        client.Verify(c => c.DeliverAsync(webhook, "TaskCompleted", entityId, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Start_WithNoMatchingWebhooks_DoesNotDeliverAnything()
    {
        var (dispatcher, eventBus, repository, client) = CreateSut();
        repository.Setup(r => r.GetEnabledForEventTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        dispatcher.Start();
        eventBus.Publish(new ApplicationEvent("TaskCompleted", Guid.NewGuid(), DateTime.UtcNow, "Test", PayloadJson: null));

        await Task.Delay(200);
        client.Verify(c => c.DeliverAsync(It.IsAny<WebhookSubscription>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Start_CalledTwice_DoesNotDoubleSubscribe()
    {
        var (dispatcher, eventBus, repository, client) = CreateSut();
        var webhook = new WebhookSubscription { Name = "Notifier", Url = "https://example.com/hook", EventTypes = ["TaskCompleted"] };
        repository.Setup(r => r.GetEnabledForEventTypeAsync("TaskCompleted", It.IsAny<CancellationToken>())).ReturnsAsync([webhook]);
        var deliveryCount = 0;
        client.Setup(c => c.DeliverAsync(webhook, "TaskCompleted", It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref deliveryCount);
                return new WebhookDeliveryLog { WebhookId = webhook.Id, EventType = "TaskCompleted", Success = true };
            });

        dispatcher.Start();
        dispatcher.Start();
        eventBus.Publish(new ApplicationEvent("TaskCompleted", Guid.NewGuid(), DateTime.UtcNow, "Test", PayloadJson: null));

        await Task.Delay(300);
        Assert.Equal(1, deliveryCount);
    }
}
