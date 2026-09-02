namespace DeskTodo.Application.Services;

/// <summary>Tuning constants for Feature 96's delivery retry/backoff/disable behavior.</summary>
public static class WebhookDispatchOptions
{
    /// <summary>"Disable after repeated failures" — this feature's own spec requirement.</summary>
    public const int MaxConsecutiveFailuresBeforeDisable = 10;

    /// <summary>Delay before each retry after the first attempt — exponential backoff, three retries (four attempts total).</summary>
    public static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4)];

    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
}
