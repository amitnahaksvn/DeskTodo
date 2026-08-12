using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeskTodo.Application.Updates;
using Microsoft.Extensions.Logging;

namespace DeskTodo.Infrastructure.Updates;

/// <summary>
/// Checks GitHub's public Releases API for a newer version — chosen over a bespoke
/// version-feed server for the same "trust an existing, always-present facility" reasoning
/// <c>MacNotificationService</c>/<c>WindowsNotificationService</c> already established (an
/// unauthenticated GET against a project that's already git-hosted, rather than standing up
/// and maintaining new infrastructure just for this). Confirmed live against the real
/// <c>amitnahaksvn/DeskTodo</c> repo before writing this: the repo is public (200) and has no
/// releases published yet (404 on <c>/releases/latest</c>) — both states this class handles,
/// not just the "a release exists" happy path.
/// </summary>
public sealed class GitHubUpdateCheckService(HttpClient httpClient, ILogger<GitHubUpdateCheckService> logger) : IUpdateCheckService
{
    private const string ReleasesUrl = "https://api.github.com/repos/amitnahaksvn/DeskTodo/releases/latest";

    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesUrl);
            // GitHub's API rejects requests with no User-Agent header outright.
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("DeskTodo-UpdateCheck", GetCurrentVersion()));

            using var response = await httpClient.SendAsync(request, cancellationToken);

            // 404 here means "this repo has no releases yet," not "the request failed" —
            // the expected state for a project that hasn't cut its first release. Any other
            // non-success status is treated the same way: no update to report, not an error
            // worth alarming the user about (rate limiting, a transient GitHub outage, etc.).
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult(false, null, null, null);
            }

            var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

            var latestVersionText = release?.TagName?.TrimStart('v', 'V');
            if (latestVersionText is null || !Version.TryParse(latestVersionText, out var latestVersion))
            {
                return new UpdateCheckResult(false, null, null, null);
            }

            var runningVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0, 0);
            var isNewer = latestVersion > runningVersion;

            return new UpdateCheckResult(isNewer, latestVersionText, release?.HtmlUrl, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to check for updates");
            return new UpdateCheckResult(false, null, null, "Couldn't check for updates — check your internet connection.");
        }
    }

    private static string GetCurrentVersion() => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0.0";

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }
    }
}
