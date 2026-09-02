namespace DeskTodo.Domain.Entities;

/// <summary>One delivery attempt of a <see cref="WebhookSubscription"/> — Feature 96's "Delivery status" / "Failure history".</summary>
public sealed class WebhookDeliveryLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required Guid WebhookId { get; set; }

    public required string EventType { get; set; }

    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    public bool Success { get; set; }

    public int? StatusCode { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>How many attempts (including this one) the retry-with-backoff loop made before this final outcome.</summary>
    public int AttemptCount { get; set; } = 1;
}
