using System.Net;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeskTodo.Infrastructure.Api;

/// <summary>
/// Feature 97's Local REST API host — a background service (started/stopped automatically by the
/// generic <c>IHost</c> that already runs everything else in this app, see <c>Program.cs</c>) that
/// owns a <see cref="System.Net.HttpListener"/> and delegates each authenticated request to
/// <see cref="ILocalApiRequestHandler"/>.
/// </summary>
/// <remarks>
/// Deliberately <see cref="HttpListener"/> rather than embedding ASP.NET Core/Kestrel — this is a
/// small, low-traffic local API for a desktop app, and the BCL's own HTTP server needs no new
/// package dependency, no <c>FrameworkReference</c>, and no separate DI container to bridge (this
/// class is registered in the exact same <see cref="IServiceCollection"/> as everything else, so
/// it resolves scoped services — one <see cref="IServiceScope"/> per request, exactly like
/// ASP.NET Core's own per-request scoping — without any cross-container plumbing).
/// <para/>
/// "Bind to localhost by default" / "Do not expose the API publicly by default" (this feature's
/// own spec) is enforced structurally, not just by a default setting: the listener prefix is
/// always <c>http://127.0.0.1:{port}/</c>, with no configuration path in this pass that can widen
/// it to <c>0.0.0.0</c> or a hostname.
/// </remarks>
public sealed class LocalApiServer(
    IServiceScopeFactory scopeFactory,
    ISettingsService settingsService,
    ILogger<LocalApiServer> logger) : BackgroundService
{
    private HttpListener? _listener;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = await settingsService.LoadAsync(stoppingToken);
        if (!settings.LocalApiEnabled)
        {
            return;
        }

        if (string.IsNullOrEmpty(settings.LocalApiToken))
        {
            logger.LogWarning("Local REST API is enabled but has no token configured; refusing to start");
            return;
        }

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{settings.LocalApiPort}/");

        try
        {
            listener.Start();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start the Local REST API on port {Port}", settings.LocalApiPort);
            return;
        }

        _listener = listener;
        logger.LogInformation("Local REST API listening on http://127.0.0.1:{Port}/", settings.LocalApiPort);

        using var registration = stoppingToken.Register(() =>
        {
            try
            {
                listener.Stop();
            }
            catch (ObjectDisposedException)
            {
            }
        });

        while (!stoppingToken.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync();
            }
            catch (Exception) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Local REST API accept loop error");
                continue;
            }

            _ = HandleRequestAsync(context, settings.LocalApiToken, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _listener?.Stop();
            _listener?.Close();
        }
        catch (ObjectDisposedException)
        {
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task HandleRequestAsync(HttpListenerContext context, string expectedToken, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (!LocalApiAuthenticator.IsAuthorized(request.Headers["Authorization"], expectedToken))
            {
                await WriteAsync(response, 401, """{"error":"Unauthorized"}""");
                return;
            }

            var localRequest = await ParseRequestAsync(request, cancellationToken);

            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ILocalApiRequestHandler>();
            var result = await handler.HandleAsync(localRequest, cancellationToken);

            await WriteAsync(response, result.StatusCode, result.BodyJson);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Local REST API request failed: {Method} {Path}", request.HttpMethod, request.Url);
            try
            {
                await WriteAsync(response, 500, """{"error":"Internal server error"}""");
            }
            catch (Exception)
            {
            }
        }
        finally
        {
            response.Close();
        }
    }

    private static async Task<LocalApiRequest> ParseRequestAsync(HttpListenerRequest request, CancellationToken cancellationToken)
    {
        string? body = null;
        if (request.HasEntityBody)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            body = await reader.ReadToEndAsync(cancellationToken);
        }

        var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in request.QueryString.AllKeys)
        {
            if (key is not null && request.QueryString[key] is { } value)
            {
                query[key] = value;
            }
        }

        return new LocalApiRequest(request.HttpMethod, request.Url?.AbsolutePath ?? string.Empty, query, body);
    }

    private static async Task WriteAsync(HttpListenerResponse response, int statusCode, string? bodyJson)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        if (bodyJson is null)
        {
            return;
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(bodyJson);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
    }
}
