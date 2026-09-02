using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Events;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>Backs the Webhooks window (Feature 96, Roadmap-39-100.md) — subscriptions, delivery history, and a manual "Send Test".</summary>
public sealed partial class WebhooksViewModel(IWebhookService webhookService, ILogger<WebhooksViewModel> logger) : ViewModelBase
{
    public ObservableCollection<WebhookRow> Webhooks { get; } = [];

    public ObservableCollection<WebhookEventTypeOption> NewWebhookEventTypeOptions { get; } =
        [.. new[] { ApplicationEventTypes.TaskCreated, ApplicationEventTypes.TaskUpdated, ApplicationEventTypes.TaskCompleted, ApplicationEventTypes.TaskDeleted, ApplicationEventTypes.TaskRestored }
            .Select(t => new WebhookEventTypeOption(t))];

    public ObservableCollection<string> SelectedWebhookDeliveryHistory { get; } = [];

    [ObservableProperty]
    public partial string NewWebhookName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewWebhookUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewWebhookSecret { get; set; } = string.Empty;

    [ObservableProperty]
    public partial WebhookRow? SelectedWebhook { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    partial void OnSelectedWebhookChanged(WebhookRow? value) => _ = LoadDeliveryHistoryAsync(value);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var webhooks = await webhookService.GetWebhooksAsync(cancellationToken);
            Webhooks.Clear();
            foreach (var webhook in webhooks)
            {
                Webhooks.Add(ToRow(webhook));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load webhooks");
        }
    }

    private async Task LoadDeliveryHistoryAsync(WebhookRow? webhook)
    {
        SelectedWebhookDeliveryHistory.Clear();
        if (webhook is null)
        {
            return;
        }

        try
        {
            var history = await webhookService.GetDeliveryHistoryAsync(webhook.Id);
            foreach (var entry in history)
            {
                var outcome = entry.Success ? $"OK ({entry.StatusCode})" : $"Failed: {entry.ErrorMessage}";
                SelectedWebhookDeliveryHistory.Add($"{entry.AttemptedAt.ToLocalTime():MMM d, h:mm tt} — {entry.EventType} — {outcome} ({entry.AttemptCount} attempt(s))");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load delivery history for webhook {WebhookId}", webhook.Id);
        }
    }

    private static WebhookRow ToRow(Domain.Entities.WebhookSubscription webhook)
    {
        var status = webhook.LastAttemptAt is null
            ? "Never delivered"
            : webhook.ConsecutiveFailureCount > 0
                ? $"{webhook.ConsecutiveFailureCount} consecutive failure(s) — last: {webhook.LastFailureReason}"
                : $"Last delivered {webhook.LastSuccessAt:g}";

        return new WebhookRow(
            webhook.Id,
            webhook.Name,
            webhook.Url,
            string.Join(", ", webhook.EventTypes),
            webhook.Enabled,
            !string.IsNullOrEmpty(webhook.Secret),
            webhook.ConsecutiveFailureCount,
            status);
    }

    [RelayCommand]
    private async Task AddWebhookAsync()
    {
        var name = NewWebhookName.Trim();
        var url = NewWebhookUrl.Trim();
        var eventTypes = NewWebhookEventTypeOptions.Where(o => o.IsSelected).Select(o => o.EventType).ToList();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url) || eventTypes.Count == 0)
        {
            StatusMessage = "A name, URL, and at least one event type are required.";
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            StatusMessage = "Enter a valid http:// or https:// URL.";
            return;
        }

        try
        {
            await webhookService.CreateWebhookAsync(name, url, eventTypes, string.IsNullOrWhiteSpace(NewWebhookSecret) ? null : NewWebhookSecret, CancellationToken.None);
            NewWebhookName = string.Empty;
            NewWebhookUrl = string.Empty;
            NewWebhookSecret = string.Empty;
            foreach (var option in NewWebhookEventTypeOptions)
            {
                option.IsSelected = false;
            }

            StatusMessage = "Webhook added.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create webhook '{Name}'", name);
            StatusMessage = "Failed to add the webhook.";
        }
    }

    [RelayCommand]
    private async Task ToggleEnabledAsync(WebhookRow row)
    {
        try
        {
            var webhook = await webhookService.GetWebhookAsync(row.Id);
            if (webhook is null)
            {
                return;
            }

            webhook.Enabled = !webhook.Enabled;
            webhook.ConsecutiveFailureCount = 0;
            await webhookService.UpdateWebhookAsync(webhook);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to toggle webhook {WebhookId}", row.Id);
        }
    }

    [RelayCommand]
    private async Task DeleteWebhookAsync(WebhookRow row)
    {
        try
        {
            await webhookService.DeleteWebhookAsync(row.Id);
            if (SelectedWebhook?.Id == row.Id)
            {
                SelectedWebhook = null;
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete webhook {WebhookId}", row.Id);
        }
    }

    [RelayCommand]
    private async Task SendTestAsync(WebhookRow row)
    {
        try
        {
            var log = await webhookService.SendTestDeliveryAsync(row.Id);
            StatusMessage = log.Success ? $"Test delivered successfully ({log.StatusCode})." : $"Test delivery failed: {log.ErrorMessage}";
            await LoadAsync();
            if (SelectedWebhook?.Id == row.Id)
            {
                await LoadDeliveryHistoryAsync(SelectedWebhook);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send a test delivery for webhook {WebhookId}", row.Id);
            StatusMessage = "Failed to send the test delivery.";
        }
    }
}
