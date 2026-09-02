using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class WebhookRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly WebhookRepository _sut;

    public WebhookRepositoryTests()
    {
        _sut = new WebhookRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    private static WebhookSubscription MakeWebhook(string name, params string[] eventTypes) => new()
    {
        Name = name,
        Url = "https://example.com/hook",
        EventTypes = eventTypes.ToList(),
        Headers = new Dictionary<string, string> { ["X-Custom"] = "value" },
    };

    [Fact]
    public async Task AddAsync_ThenGetAllAsync_RoundTripsEventTypesAndHeaders()
    {
        var webhook = MakeWebhook("Slack Notifier", "TaskCreated", "TaskCompleted");

        await _sut.AddAsync(webhook);

        var all = await _sut.GetAllAsync();
        var loaded = Assert.Single(all);
        Assert.Equal(["TaskCreated", "TaskCompleted"], loaded.EventTypes);
        Assert.Equal("value", loaded.Headers["X-Custom"]);
    }

    [Fact]
    public async Task GetEnabledForEventTypeAsync_OnlyReturnsEnabledWebhooksSubscribedToThatType()
    {
        var matching = MakeWebhook("Matching", "TaskCompleted");
        var wrongType = MakeWebhook("Wrong Type", "TaskCreated");
        var disabled = MakeWebhook("Disabled", "TaskCompleted");
        disabled.Enabled = false;
        await _sut.AddAsync(matching);
        await _sut.AddAsync(wrongType);
        await _sut.AddAsync(disabled);

        var results = await _sut.GetEnabledForEventTypeAsync("TaskCompleted");

        var result = Assert.Single(results);
        Assert.Equal("Matching", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var webhook = MakeWebhook("Notifier", "TaskCreated");
        await _sut.AddAsync(webhook);

        webhook.Enabled = false;
        webhook.ConsecutiveFailureCount = 3;
        await _sut.UpdateAsync(webhook);

        var reloaded = await _sut.GetByIdAsync(webhook.Id);
        Assert.False(reloaded!.Enabled);
        Assert.Equal(3, reloaded.ConsecutiveFailureCount);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheWebhook()
    {
        var webhook = MakeWebhook("Notifier", "TaskCreated");
        await _sut.AddAsync(webhook);

        await _sut.DeleteAsync(webhook.Id);

        Assert.Empty(await _sut.GetAllAsync());
    }

    [Fact]
    public async Task GetByIdAsync_WithAnUnknownId_ReturnsNull()
    {
        Assert.Null(await _sut.GetByIdAsync(Guid.NewGuid()));
    }
}
