using System.Net;
using DeskTodo.Cli;

namespace DeskTodo.Tests.Cli;

public class ApiClientTests
{
    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        /// <summary>Captured here, not read back from <see cref="LastRequest"/> afterward — <see cref="ApiClient.SendAsync"/> disposes its <see cref="HttpRequestMessage"/> (and its <see cref="HttpContent"/> with it) once it returns.</summary>
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return respond(request);
        }
    }

    [Fact]
    public async Task SendAsync_OnSuccess_ReturnsTheParsedBody()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"title":"Ship it"}"""),
        });
        var client = new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:1/") });

        var (success, statusCode, body, error) = await client.SendAsync(HttpMethod.Get, "api/v1/tasks/x");

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Ship it", body!.Value.GetProperty("title").GetString());
        Assert.Null(error);
    }

    [Fact]
    public async Task SendAsync_OnAnErrorStatus_ReturnsTheErrorMessage()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("""{"error":"Not found"}"""),
        });
        var client = new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:1/") });

        var (success, statusCode, _, error) = await client.SendAsync(HttpMethod.Get, "api/v1/tasks/x");

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Equal("Not found", error);
    }

    [Fact]
    public async Task SendAsync_WithABody_SerializesItAsJson()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{}") });
        var client = new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:1/") });

        await client.SendAsync(HttpMethod.Post, "api/v1/tasks", new { title = "Ship it" });

        Assert.Contains("Ship it", handler.LastRequestBody);
    }

    [Fact]
    public async Task SendAsync_WhenTheServerIsUnreachable_ReturnsAHelpfulError()
    {
        var client = new ApiClient(new HttpClient { BaseAddress = new Uri("http://127.0.0.1:1/") });

        var (success, statusCode, _, error) = await client.SendAsync(HttpMethod.Get, "api/v1/tasks");

        Assert.False(success);
        Assert.Equal(0, statusCode);
        Assert.Contains("DeskTodo", error);
    }

    [Fact]
    public void CreateHttpClient_SetsTheBearerTokenHeader()
    {
        using var client = ApiClient.CreateHttpClient("http://127.0.0.1", 47291, "my-token");

        Assert.Equal("Bearer", client.DefaultRequestHeaders.Authorization!.Scheme);
        Assert.Equal("my-token", client.DefaultRequestHeaders.Authorization!.Parameter);
        Assert.Equal(new Uri("http://127.0.0.1:47291/"), client.BaseAddress);
    }
}
