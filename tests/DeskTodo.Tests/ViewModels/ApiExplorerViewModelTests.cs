using System.Net;
using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class ApiExplorerViewModelTests
{
    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(respond(request));
        }
    }

    private readonly Mock<ISettingsService> _settingsService = new();

    private ApiExplorerViewModel CreateSut(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler), _settingsService.Object, NullLogger<ApiExplorerViewModel>.Instance);

    [Fact]
    public void BuildUrl_WithNoQuery_ReturnsThePlainPath()
    {
        Assert.Equal("http://127.0.0.1:47291/api/v1/tasks", ApiExplorerViewModel.BuildUrl(47291, "/api/v1/tasks", string.Empty));
    }

    [Fact]
    public void BuildUrl_WithQueryParameters_AppendsThem()
    {
        var url = ApiExplorerViewModel.BuildUrl(47291, "/api/v1/tasks", "date=2026-09-02");

        Assert.Equal("http://127.0.0.1:47291/api/v1/tasks?date=2026-09-02", url);
    }

    [Fact]
    public void BuildUrl_WithAPathMissingALeadingSlash_AddsOne()
    {
        var url = ApiExplorerViewModel.BuildUrl(47291, "api/v1/tasks", string.Empty);

        Assert.Equal("http://127.0.0.1:47291/api/v1/tasks", url);
    }

    [Fact]
    public void ParseHeaders_ParsesKeyColonValueLines()
    {
        var headers = ApiExplorerViewModel.ParseHeaders("X-Api-Key: abc123\nAccept: application/json");

        Assert.Equal(2, headers.Count);
        Assert.Contains(headers, h => h.Key == "X-Api-Key" && h.Value == "abc123");
        Assert.Contains(headers, h => h.Key == "Accept" && h.Value == "application/json");
    }

    [Fact]
    public void ParseHeaders_IgnoresLinesWithNoColon()
    {
        var headers = ApiExplorerViewModel.ParseHeaders("not a header");

        Assert.Empty(headers);
    }

    [Fact]
    public void TryPrettyPrint_WithValidJson_IndentsIt()
    {
        var result = ApiExplorerViewModel.TryPrettyPrint("""{"a":1}""");

        Assert.Contains('\n', result);
    }

    [Fact]
    public void TryPrettyPrint_WithInvalidJson_ReturnsItUnchanged()
    {
        Assert.Equal("not json", ApiExplorerViewModel.TryPrettyPrint("not json"));
    }

    [Fact]
    public void TryPrettyPrint_WithEmptyText_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ApiExplorerViewModel.TryPrettyPrint(string.Empty));
    }

    [Fact]
    public async Task LoadAsync_PrefillsTheAuthorizationTokenOverride()
    {
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { LocalApiToken = "real-token" });
        var sut = CreateSut(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        await sut.LoadAsync();

        Assert.Equal("real-token", sut.AuthorizationTokenOverride);
    }

    [Fact]
    public async Task LoadAsync_PopulatesSavedRequests()
    {
        var saved = new ApiExplorerSavedRequest { Name = "List tasks", Method = "GET", Path = "/api/v1/tasks" };
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { ApiExplorerSavedRequests = [saved] });
        var sut = CreateSut(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        await sut.LoadAsync();

        Assert.Single(sut.SavedRequests);
        Assert.Equal("List tasks", sut.SavedRequests[0].Name);
    }

    [Fact]
    public async Task SendAsync_WhenTheApiIsNotEnabled_SetsAnExplanatoryMessage_WithoutSendingARequest()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { LocalApiEnabled = false });
        var sut = CreateSut(handler);

        await sut.SendCommand.ExecuteAsync(null);

        Assert.Null(handler.LastRequest);
        Assert.Contains("Local REST API isn't enabled", sut.ResponseText);
    }

    [Fact]
    public async Task SendAsync_OnSuccess_PopulatesStatusCodeTimingAndPrettyPrintedBody()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"title":"Ship it"}""") });
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { LocalApiEnabled = true, LocalApiPort = 47291, LocalApiToken = "real-token" });
        var sut = CreateSut(handler);
        await sut.LoadAsync();
        sut.Path = "/api/v1/tasks";

        await sut.SendCommand.ExecuteAsync(null);

        Assert.Equal(200, sut.ResponseStatusCode);
        Assert.NotNull(sut.ResponseTimeMs);
        Assert.Contains("Ship it", sut.ResponseText);
        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("real-token", handler.LastRequest!.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task SendAsync_WithAnOverriddenToken_SendsThatTokenInstead()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { LocalApiEnabled = true, LocalApiPort = 47291, LocalApiToken = "real-token" });
        var sut = CreateSut(handler);
        await sut.LoadAsync();
        sut.AuthorizationTokenOverride = "wrong-token";

        await sut.SendCommand.ExecuteAsync(null);

        Assert.Equal("wrong-token", handler.LastRequest!.Headers.Authorization!.Parameter);
        Assert.Equal(401, sut.ResponseStatusCode);
    }

    [Fact]
    public async Task SelectEndpointCommand_SetsMethodAndPath()
    {
        var sut = CreateSut(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        sut.SelectEndpointCommand.Execute(new ApiEndpointOption("POST", "/api/v1/tasks", "Create a task"));

        Assert.Equal("POST", sut.Method);
        Assert.Equal("/api/v1/tasks", sut.Path);
    }

    [Fact]
    public async Task SaveRequestAsync_WithAName_PersistsIt()
    {
        var settings = new AppSettings();
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        var sut = CreateSut(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        await sut.LoadAsync();
        sut.NewRequestName = "My Request";
        sut.Method = "GET";
        sut.Path = "/api/v1/tasks";

        await sut.SaveRequestCommand.ExecuteAsync(null);

        Assert.Single(sut.SavedRequests);
        _settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => a.ApiExplorerSavedRequests.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(string.Empty, sut.NewRequestName);
    }

    [Fact]
    public async Task ApplySavedRequestCommand_PopulatesTheFormFields()
    {
        var sut = CreateSut(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var saved = new ApiExplorerSavedRequest { Name = "X", Method = "POST", Path = "/api/v1/tasks", QueryText = "date=2026-09-02", HeadersText = "X: 1", BodyText = "{}" };

        sut.ApplySavedRequestCommand.Execute(saved);

        Assert.Equal("POST", sut.Method);
        Assert.Equal("/api/v1/tasks", sut.Path);
        Assert.Equal("date=2026-09-02", sut.QueryParametersText);
        Assert.Equal("X: 1", sut.HeadersText);
        Assert.Equal("{}", sut.BodyText);
    }

    [Fact]
    public async Task DeleteSavedRequestAsync_RemovesIt()
    {
        var saved = new ApiExplorerSavedRequest { Name = "X", Method = "GET", Path = "/api/v1/tasks" };
        var settings = new AppSettings { ApiExplorerSavedRequests = [saved] };
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        var sut = CreateSut(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        await sut.LoadAsync();

        await sut.DeleteSavedRequestCommand.ExecuteAsync(sut.SavedRequests[0]);

        Assert.Empty(sut.SavedRequests);
        _settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => a.ApiExplorerSavedRequests.Count == 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void EndpointSearchText_FiltersTheEndpointList()
    {
        var sut = CreateSut(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        sut.EndpointSearchText = "projects";

        Assert.All(sut.FilteredEndpoints, e => Assert.Contains("projects", e.PathTemplate));
    }
}
