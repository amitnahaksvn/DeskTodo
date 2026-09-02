namespace DeskTodo.App.ViewModels;

/// <summary>One <see cref="Domain.Entities.WebhookSubscription"/> as shown in <see cref="WebhooksViewModel"/>'s list.</summary>
public sealed record WebhookRow(
    Guid Id,
    string Name,
    string Url,
    string EventTypesDisplay,
    bool Enabled,
    bool HasSecret,
    int ConsecutiveFailureCount,
    string StatusDisplay);
