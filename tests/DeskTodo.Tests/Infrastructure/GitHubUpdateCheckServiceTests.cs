using System.Net;
using DeskTodo.Infrastructure.Updates;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeskTodo.Tests.Infrastructure;

/// <summary>
/// <see cref="GitHubUpdateCheckService"/> against a fake <see cref="HttpMessageHandler"/> —
/// not the real GitHub API, so these run offline/deterministically. See
/// <c>docs/ARCHITECTURE.md</c>'s "Phase 30" section for the one real network round trip that
/// *was* made against the live API, manually, before writing this class.
/// </summary>
public class GitHubUpdateCheckServiceTests
{
    private sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    private static GitHubUpdateCheckService CreateSut(HttpResponseMessage response)
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(response));
        return new GitHubUpdateCheckService(httpClient, NullLogger<GitHubUpdateCheckService>.Instance);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithAMuchNewerReleaseTag_ReportsAnUpdateIsAvailable()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"tag_name": "v99.0.0", "html_url": "https://github.com/amitnahaksvn/DeskTodo/releases/tag/v99.0.0"}"""),
        };
        var sut = CreateSut(response);

        var result = await sut.CheckForUpdateAsync();

        Assert.True(result.IsUpdateAvailable);
        Assert.Equal("99.0.0", result.LatestVersion);
        Assert.Equal("https://github.com/amitnahaksvn/DeskTodo/releases/tag/v99.0.0", result.ReleaseUrl);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithAnOlderReleaseTag_ReportsNoUpdateAvailable()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"tag_name": "v0.0.1", "html_url": "https://example.com"}"""),
        };
        var sut = CreateSut(response);

        var result = await sut.CheckForUpdateAsync();

        Assert.False(result.IsUpdateAvailable);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckForUpdateAsync_With404_TreatsNoReleasesYetAsNotAnError()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var sut = CreateSut(response);

        var result = await sut.CheckForUpdateAsync();

        Assert.False(result.IsUpdateAvailable);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithAMalformedTag_DoesNotThrow_AndReportsNoUpdate()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"tag_name": "not-a-version", "html_url": "https://example.com"}"""),
        };
        var sut = CreateSut(response);

        var result = await sut.CheckForUpdateAsync();

        Assert.False(result.IsUpdateAvailable);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenTheRequestThrows_ReturnsAnErrorMessage_RatherThanThrowing()
    {
        var httpClient = new HttpClient(new ThrowingHttpMessageHandler());
        var sut = new GitHubUpdateCheckService(httpClient, NullLogger<GitHubUpdateCheckService>.Instance);

        var result = await sut.CheckForUpdateAsync();

        Assert.False(result.IsUpdateAvailable);
        Assert.NotNull(result.ErrorMessage);
    }
}
