using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Settings;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Developer API Explorer window (Feature 100, Roadmap-39-100.md) — a lightweight
/// Postman/Swagger-style client against DeskTodo's own Local REST API (Feature 97). Every request
/// is a loopback call to <c>http://127.0.0.1:{LocalApiPort}</c> — this app talking to itself over
/// the same API any external tool would use, which is exactly the point: it's the same surface
/// the CLI (Feature 99) and any other integration hits.
/// </summary>
public sealed partial class ApiExplorerViewModel(HttpClient httpClient, ISettingsService settingsService, ILogger<ApiExplorerViewModel> logger) : ViewModelBase
{
    private static readonly JsonSerializerOptions PrettyPrintOptions = new() { WriteIndented = true };

    private AppSettings _loaded = new();

    public ObservableCollection<ApiEndpointOption> FilteredEndpoints { get; } = [.. ApiEndpointOption.All];

    public IReadOnlyList<string> MethodOptions { get; } = ["GET", "POST", "PUT", "DELETE"];

    public ObservableCollection<ApiExplorerSavedRequest> SavedRequests { get; } = [];

    [ObservableProperty]
    public partial string EndpointSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Method { get; set; } = "GET";

    [ObservableProperty]
    public partial string Path { get; set; } = "/api/v1/tasks";

    [ObservableProperty]
    public partial string QueryParametersText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HeadersText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BodyText { get; set; } = string.Empty;

    /// <summary>Pre-filled with the real token on load; editable so a deliberately wrong value can be sent — "Authentication testing" from this feature's own spec.</summary>
    [ObservableProperty]
    public partial string AuthorizationTokenOverride { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ResponseText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int? ResponseStatusCode { get; set; }

    [ObservableProperty]
    public partial double? ResponseTimeMs { get; set; }

    [ObservableProperty]
    public partial string NewRequestName { get; set; } = string.Empty;

    partial void OnEndpointSearchTextChanged(string value) => ApplyEndpointFilter();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _loaded = await settingsService.LoadAsync(cancellationToken);
            AuthorizationTokenOverride = _loaded.LocalApiToken ?? string.Empty;
            SavedRequests.Clear();
            foreach (var saved in _loaded.ApiExplorerSavedRequests)
            {
                SavedRequests.Add(saved);
            }

            ApplyEndpointFilter();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load the API Explorer");
        }
    }

    private void ApplyEndpointFilter()
    {
        var search = EndpointSearchText.Trim();
        FilteredEndpoints.Clear();
        foreach (var endpoint in ApiEndpointOption.All.Where(e =>
                     string.IsNullOrEmpty(search) ||
                     e.PathTemplate.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                     e.Description.Contains(search, StringComparison.OrdinalIgnoreCase)))
        {
            FilteredEndpoints.Add(endpoint);
        }
    }

    [RelayCommand]
    private void SelectEndpoint(ApiEndpointOption endpoint)
    {
        Method = endpoint.Method;
        Path = endpoint.PathTemplate;
    }

    [RelayCommand]
    private void ApplySavedRequest(ApiExplorerSavedRequest saved)
    {
        Method = saved.Method;
        Path = saved.Path;
        QueryParametersText = saved.QueryText;
        HeadersText = saved.HeadersText;
        BodyText = saved.BodyText;
    }

    [RelayCommand]
    private async Task SaveRequestAsync()
    {
        var name = NewRequestName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        try
        {
            _loaded.ApiExplorerSavedRequests.RemoveAll(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
            _loaded.ApiExplorerSavedRequests.Add(new ApiExplorerSavedRequest
            {
                Name = name,
                Method = Method,
                Path = Path,
                QueryText = QueryParametersText,
                HeadersText = HeadersText,
                BodyText = BodyText,
            });
            await settingsService.SaveAsync(_loaded);
            NewRequestName = string.Empty;
            SavedRequests.Clear();
            foreach (var saved in _loaded.ApiExplorerSavedRequests)
            {
                SavedRequests.Add(saved);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save API Explorer request '{Name}'", name);
        }
    }

    [RelayCommand]
    private async Task DeleteSavedRequestAsync(ApiExplorerSavedRequest saved)
    {
        try
        {
            _loaded.ApiExplorerSavedRequests.RemoveAll(r => r.Name == saved.Name);
            await settingsService.SaveAsync(_loaded);
            SavedRequests.Remove(saved);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete API Explorer request '{Name}'", saved.Name);
        }
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        try
        {
            var settings = await settingsService.LoadAsync();
            if (!settings.LocalApiEnabled || string.IsNullOrEmpty(settings.LocalApiToken))
            {
                ResponseStatusCode = null;
                ResponseTimeMs = null;
                ResponseText = "The Local REST API isn't enabled. Turn it on in Settings → Local REST API, then restart DeskTodo.";
                return;
            }

            var url = BuildUrl(settings.LocalApiPort, Path, QueryParametersText);
            using var request = new HttpRequestMessage(new HttpMethod(Method), url);

            var token = string.IsNullOrWhiteSpace(AuthorizationTokenOverride) ? settings.LocalApiToken : AuthorizationTokenOverride.Trim();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            foreach (var (key, value) in ParseHeaders(HeadersText))
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }

            if (!string.IsNullOrWhiteSpace(BodyText) && (Method is "POST" or "PUT"))
            {
                request.Content = new StringContent(BodyText, Encoding.UTF8, "application/json");
            }

            var stopwatch = Stopwatch.StartNew();
            using var response = await httpClient.SendAsync(request);
            stopwatch.Stop();

            var text = await response.Content.ReadAsStringAsync();
            ResponseStatusCode = (int)response.StatusCode;
            ResponseTimeMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 1);
            ResponseText = TryPrettyPrint(text);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API Explorer request failed: {Method} {Path}", Method, Path);
            ResponseStatusCode = null;
            ResponseTimeMs = null;
            ResponseText = $"Request failed: {ex.Message}";
        }
    }

    internal static string BuildUrl(int port, string path, string queryText)
    {
        var normalizedPath = path.StartsWith('/') ? path : "/" + path;
        var url = $"http://127.0.0.1:{port}{normalizedPath}";
        var query = ParseQuery(queryText);
        if (query.Count > 0)
        {
            url += "?" + string.Join("&", query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        }

        return url;
    }

    internal static List<KeyValuePair<string, string>> ParseQuery(string text) =>
        text.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts => new KeyValuePair<string, string>(parts[0], parts[1]))
            .ToList();

    internal static List<(string Key, string Value)> ParseHeaders(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(':', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts => (parts[0].Trim(), parts[1].Trim()))
            .ToList();

    internal static string TryPrettyPrint(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document.RootElement, PrettyPrintOptions);
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
