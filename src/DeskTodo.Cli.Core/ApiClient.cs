using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DeskTodo.Cli;

/// <summary>A thin HTTP client for DeskTodo's Local REST API (Feature 97) — the CLI's only way to reach the app, per this feature's own "CLI → Local REST API → Application Layer" architecture note.</summary>
public sealed class ApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<(bool Success, int StatusCode, JsonElement? Body, string? Error)> SendAsync(HttpMethod method, string path, object? body = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
            }

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            JsonElement? parsed = string.IsNullOrWhiteSpace(text) ? null : JsonSerializer.Deserialize<JsonElement>(text);

            if (response.IsSuccessStatusCode)
            {
                return (true, (int)response.StatusCode, parsed, null);
            }

            var error = parsed?.TryGetProperty("error", out var errorProp) == true ? errorProp.GetString() : $"HTTP {(int)response.StatusCode}";
            return (false, (int)response.StatusCode, parsed, error);
        }
        catch (HttpRequestException ex)
        {
            return (false, 0, null, $"Could not reach DeskTodo's Local REST API: {ex.Message}. Is DeskTodo running with the API enabled (Settings → Local REST API)?");
        }
    }

    public static HttpClient CreateHttpClient(string host, int port, string token)
    {
        var client = new HttpClient { BaseAddress = new Uri($"{host}:{port}/") };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
