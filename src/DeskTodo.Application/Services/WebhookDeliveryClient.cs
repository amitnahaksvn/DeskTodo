using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IWebhookDeliveryClient"/>
public sealed class WebhookDeliveryClient(
    HttpClient httpClient,
    IWebhookRepository webhookRepository,
    IWebhookDeliveryLogRepository deliveryLogRepository,
    ILogger<WebhookDeliveryClient> logger) : IWebhookDeliveryClient
{
    public async Task<WebhookDeliveryLog> DeliverAsync(WebhookSubscription webhook, string eventType, Guid entityId, string? payloadJson, CancellationToken cancellationToken = default)
    {
        var body = BuildBody(eventType, entityId, payloadJson);

        int? lastStatusCode = null;
        string? lastError = null;
        var attempt = 0;

        while (true)
        {
            attempt++;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                };

                foreach (var header in webhook.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                if (!string.IsNullOrEmpty(webhook.Secret))
                {
                    request.Headers.TryAddWithoutValidation("X-DeskTodo-Signature", $"sha256={ComputeSignature(webhook.Secret, body)}");
                }

                using var timeoutCts = new CancellationTokenSource(WebhookDispatchOptions.RequestTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                using var response = await httpClient.SendAsync(request, linkedCts.Token);
                lastStatusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    return await RecordOutcomeAsync(webhook, eventType, success: true, lastStatusCode, errorMessage: null, attempt, cancellationToken);
                }

                lastError = $"HTTP {(int)response.StatusCode}";
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
            }

            if (attempt > WebhookDispatchOptions.RetryDelays.Length)
            {
                break;
            }

            await Task.Delay(WebhookDispatchOptions.RetryDelays[attempt - 1], cancellationToken);
        }

        return await RecordOutcomeAsync(webhook, eventType, success: false, lastStatusCode, lastError, attempt, cancellationToken);
    }

    private static string BuildBody(string eventType, Guid entityId, string? payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson is null ? "null" : payloadJson);
        var envelope = new
        {
            eventType,
            entityId,
            timestamp = DateTime.UtcNow,
            payload = document.RootElement,
        };
        return JsonSerializer.Serialize(envelope);
    }

    private static string ComputeSignature(string secret, string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task<WebhookDeliveryLog> RecordOutcomeAsync(WebhookSubscription webhook, string eventType, bool success, int? statusCode, string? errorMessage, int attemptCount, CancellationToken cancellationToken)
    {
        var log = new WebhookDeliveryLog
        {
            WebhookId = webhook.Id,
            EventType = eventType,
            Success = success,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
            AttemptCount = attemptCount,
        };
        await deliveryLogRepository.AddAsync(log, cancellationToken);

        webhook.LastAttemptAt = log.AttemptedAt;
        if (success)
        {
            webhook.LastSuccessAt = log.AttemptedAt;
            webhook.ConsecutiveFailureCount = 0;
            webhook.LastFailureReason = null;
        }
        else
        {
            webhook.ConsecutiveFailureCount++;
            webhook.LastFailureReason = errorMessage;
            if (webhook.ConsecutiveFailureCount >= WebhookDispatchOptions.MaxConsecutiveFailuresBeforeDisable)
            {
                webhook.Enabled = false;
                logger.LogWarning("Webhook '{Name}' auto-disabled after {Count} consecutive failures", webhook.Name, webhook.ConsecutiveFailureCount);
            }
        }

        await webhookRepository.UpdateAsync(webhook, cancellationToken);
        return log;
    }
}
