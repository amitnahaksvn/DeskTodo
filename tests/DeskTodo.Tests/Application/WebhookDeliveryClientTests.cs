using System.Net;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.Application;

/// <summary>
/// <see cref="WebhookDeliveryClient"/> against a fake <see cref="HttpMessageHandler"/> — same
/// offline/deterministic reasoning as <c>GitHubUpdateCheckServiceTests</c>. The
/// "exhausts every retry" case genuinely sleeps through the real backoff delays (~7s) rather than
/// faking the clock — kept to exactly one such test to bound how much wall-clock time that costs.
/// </summary>
public class WebhookDeliveryClientTests
{
    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }
        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return respond(request);
        }
    }

    private readonly Mock<IWebhookRepository> _webhookRepository = new();
    private readonly Mock<IWebhookDeliveryLogRepository> _deliveryLogRepository = new();

    private WebhookDeliveryClient CreateSut(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler), _webhookRepository.Object, _deliveryLogRepository.Object, NullLogger<WebhookDeliveryClient>.Instance);

    private static WebhookSubscription MakeWebhook(string? secret = null) => new()
    {
        Name = "Notifier",
        Url = "https://example.com/hook",
        EventTypes = ["TaskCompleted"],
        Secret = secret,
    };

    [Fact]
    public async Task DeliverAsync_OnSuccess_LogsSuccess_AndResetsFailureCount()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateSut(handler);
        var webhook = MakeWebhook();
        webhook.ConsecutiveFailureCount = 2;

        var log = await sut.DeliverAsync(webhook, "TaskCompleted", Guid.NewGuid(), payloadJson: """{"title":"Test"}""");

        Assert.True(log.Success);
        Assert.Equal(200, log.StatusCode);
        Assert.Equal(1, log.AttemptCount);
        Assert.Equal(0, webhook.ConsecutiveFailureCount);
        Assert.NotNull(webhook.LastSuccessAt);
        _deliveryLogRepository.Verify(r => r.AddAsync(It.Is<WebhookDeliveryLog>(l => l.Success), It.IsAny<CancellationToken>()), Times.Once);
        _webhookRepository.Verify(r => r.UpdateAsync(webhook, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeliverAsync_SendsTheEventTypeAndEntityIdInTheBody()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateSut(handler);
        var entityId = Guid.NewGuid();

        await sut.DeliverAsync(MakeWebhook(), "TaskCompleted", entityId, payloadJson: """{"title":"Test"}""");

        Assert.Contains("TaskCompleted", handler.LastRequestBody);
        Assert.Contains(entityId.ToString(), handler.LastRequestBody);
        Assert.Contains("Test", handler.LastRequestBody);
    }

    [Fact]
    public async Task DeliverAsync_WithASecret_SignsTheRequest()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateSut(handler);

        await sut.DeliverAsync(MakeWebhook(secret: "shh"), "TaskCompleted", Guid.NewGuid(), payloadJson: null);

        Assert.True(handler.LastRequest!.Headers.TryGetValues("X-DeskTodo-Signature", out var values));
        Assert.StartsWith("sha256=", values!.Single());
    }

    [Fact]
    public async Task DeliverAsync_WithoutASecret_DoesNotSignTheRequest()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateSut(handler);

        await sut.DeliverAsync(MakeWebhook(), "TaskCompleted", Guid.NewGuid(), payloadJson: null);

        Assert.False(handler.LastRequest!.Headers.Contains("X-DeskTodo-Signature"));
    }

    [Fact]
    public async Task DeliverAsync_IncludesCustomHeaders()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateSut(handler);
        var webhook = MakeWebhook();
        webhook.Headers["X-Api-Key"] = "abc123";

        await sut.DeliverAsync(webhook, "TaskCompleted", Guid.NewGuid(), payloadJson: null);

        Assert.True(handler.LastRequest!.Headers.TryGetValues("X-Api-Key", out var values));
        Assert.Equal("abc123", values!.Single());
    }

    [Fact]
    public async Task DeliverAsync_WhenEveryAttemptFails_RetriesThenLogsFailure()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = CreateSut(handler);
        var webhook = MakeWebhook();

        var log = await sut.DeliverAsync(webhook, "TaskCompleted", Guid.NewGuid(), payloadJson: null);

        Assert.False(log.Success);
        Assert.Equal(500, log.StatusCode);
        Assert.Equal(4, log.AttemptCount); // 1 initial + 3 retries
        Assert.Equal(4, handler.CallCount);
        Assert.Equal(1, webhook.ConsecutiveFailureCount);
        Assert.True(webhook.Enabled); // one failure is well below the auto-disable threshold.
    }

    [Fact]
    public async Task DeliverAsync_WhenReachingTheFailureThreshold_AutoDisablesTheWebhook()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = CreateSut(handler);
        var webhook = MakeWebhook();
        webhook.ConsecutiveFailureCount = WebhookDispatchOptions.MaxConsecutiveFailuresBeforeDisable - 1;

        await sut.DeliverAsync(webhook, "TaskCompleted", Guid.NewGuid(), payloadJson: null);

        Assert.Equal(WebhookDispatchOptions.MaxConsecutiveFailuresBeforeDisable, webhook.ConsecutiveFailureCount);
        Assert.False(webhook.Enabled);
    }
}
