namespace DeskTodo.Domain.Entities;

/// <summary>Feature 96 (Roadmap-39-100.md) — an outbound webhook: fires a signed HTTP POST to <see cref="Url"/> whenever one of <see cref="EventTypes"/> is published on the app's event bus (<c>DeskTodo.Application.Events.IEventBus</c> — the Domain layer this entity lives in has no dependency on the Application layer, so that type can't be linked here).</summary>
public sealed class WebhookSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public required string Url { get; set; }

    /// <summary>Which event type strings (e.g. <c>"TaskCompleted"</c> — see <c>DeskTodo.Application.Events.ApplicationEventTypes</c>) this webhook fires for.</summary>
    public List<string> EventTypes { get; set; } = [];

    /// <summary>Extra headers sent with every delivery (e.g. an API key the receiving endpoint expects) — never logged in <see cref="WebhookDeliveryLog"/>, only the response status/error.</summary>
    public Dictionary<string, string> Headers { get; set; } = [];

    /// <summary>When set, every delivery is signed: <c>X-DeskTodo-Signature: sha256=&lt;hex HMAC-SHA256 of the JSON body&gt;</c>, so the receiving endpoint can verify the payload actually came from this app.</summary>
    public string? Secret { get; set; }

    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Resets to 0 on every successful delivery; the webhook is auto-disabled once this reaches the dispatcher's configured failure threshold — "Disable after repeated failures" from this feature's own spec.</summary>
    public int ConsecutiveFailureCount { get; set; }

    public DateTime? LastAttemptAt { get; set; }

    public DateTime? LastSuccessAt { get; set; }

    public string? LastFailureReason { get; set; }
}
