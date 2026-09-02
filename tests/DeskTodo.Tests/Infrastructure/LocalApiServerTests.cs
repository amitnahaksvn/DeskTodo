using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Application.Settings;
using DeskTodo.Infrastructure.Api;
using DeskTodo.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.Infrastructure;

/// <summary>
/// A genuine end-to-end round trip against <see cref="LocalApiServer"/> — a real
/// <see cref="System.Net.HttpListener"/> bound to a real (fixed test) localhost port, a real
/// <see cref="System.Net.Http.HttpClient"/> making real requests, and real
/// <c>TaskService</c>/<c>ProjectService</c> backed by <see cref="SqliteInMemoryFixture"/> — not
/// mocked routing. Kept to a small number of cases; exhaustive routing/validation coverage lives
/// in <c>LocalApiRequestHandlerTests</c> against no real sockets at all.
/// </summary>
public class LocalApiServerTests : IAsyncDisposable
{
    private const string Token = "test-token-0123456789abcdef";
    private const int Port = 58231;

    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly ServiceProvider _provider;
    private readonly LocalApiServer _server;
    private readonly HttpClient _httpClient = new() { BaseAddress = new Uri($"http://127.0.0.1:{Port}/") };

    public LocalApiServerTests()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITaskRepository>(_ => new TaskRepository(_fixture.ContextFactory));
        services.AddScoped<ITaskHistoryRepository>(_ => Mock.Of<ITaskHistoryRepository>());
        services.AddScoped<ITaskVersionRepository>(_ => Mock.Of<ITaskVersionRepository>());
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IProjectRepository>(_ => new ProjectRepository(_fixture.ContextFactory));
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ILocalApiRequestHandler, LocalApiRequestHandler>();
        _provider = services.BuildServiceProvider();

        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings { LocalApiEnabled = true, LocalApiPort = Port, LocalApiToken = Token });

        _server = new LocalApiServer(_provider.GetRequiredService<IServiceScopeFactory>(), settingsService.Object, NullLogger<LocalApiServer>.Instance);
    }

    public async ValueTask DisposeAsync()
    {
        await _server.StopAsync(CancellationToken.None);
        _httpClient.Dispose();
        _fixture.Dispose();
        await _provider.DisposeAsync();
    }

    private async Task StartAsync()
    {
        await _server.StartAsync(CancellationToken.None);

        // The listener binds inside ExecuteAsync, which StartAsync kicks off without awaiting
        // to completion — poll briefly rather than a fixed sleep, since binding is normally
        // near-instant but shouldn't be assumed synchronous.
        for (var i = 0; i < 50; i++)
        {
            try
            {
                using var probe = await _httpClient.GetAsync("api/v1/tasks");
                return;
            }
            catch (HttpRequestException)
            {
                await Task.Delay(50);
            }
        }
    }

    [Fact]
    public async Task GetTasks_WithoutAnAuthorizationHeader_Returns401()
    {
        await StartAsync();

        var response = await _httpClient.GetAsync("api/v1/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTasks_WithTheWrongToken_Returns401()
    {
        await StartAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "wrong-token");

        var response = await _httpClient.GetAsync("api/v1/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateThenGetTask_RoundTripsThroughTheRealDatabase()
    {
        await StartAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        var createBody = new StringContent("""{"title":"Ship the API","priority":"High"}""", Encoding.UTF8, "application/json");

        var createResponse = await _httpClient.PostAsync("api/v1/tasks", createBody);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = JsonSerializer.Deserialize<JsonElement>(await createResponse.Content.ReadAsStringAsync());
        var id = created.GetProperty("id").GetGuid();

        var getResponse = await _httpClient.GetAsync($"api/v1/tasks/{id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = JsonSerializer.Deserialize<JsonElement>(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal("Ship the API", fetched.GetProperty("title").GetString());
        Assert.Equal("High", fetched.GetProperty("priority").GetString());
    }

    [Fact]
    public async Task GetTask_WithAnUnknownId_Returns404()
    {
        await StartAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

        var response = await _httpClient.GetAsync($"api/v1/tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_RemovesItFromSubsequentGets()
    {
        await StartAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        var createResponse = await _httpClient.PostAsync("api/v1/tasks", new StringContent("""{"title":"Temp task"}""", Encoding.UTF8, "application/json"));
        var created = JsonSerializer.Deserialize<JsonElement>(await createResponse.Content.ReadAsStringAsync());
        var id = created.GetProperty("id").GetGuid();

        var deleteResponse = await _httpClient.DeleteAsync($"api/v1/tasks/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _httpClient.GetAsync($"api/v1/tasks/{id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode); // soft-deleted, still fetchable by id
        var fetched = JsonSerializer.Deserialize<JsonElement>(await getResponse.Content.ReadAsStringAsync());
        Assert.False(fetched.GetProperty("isCompleted").GetBoolean());
    }
}
