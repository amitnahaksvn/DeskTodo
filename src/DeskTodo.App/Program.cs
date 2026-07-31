using Avalonia;
using DeskTodo.App.DependencyInjection;
using DeskTodo.Infrastructure.Data;
using DeskTodo.Infrastructure.DependencyInjection;
using DeskTodo.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DeskTodo.App;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        IHost? host = null;

        try
        {
            Log.Information("DeskTodo starting up (version {Version})", typeof(Program).Assembly.GetName().Version);

            host = Host.CreateDefaultBuilder(args)
                .UseDeskTodoLogging()
                .ConfigureServices((context, services) =>
                {
                    services.AddInfrastructure(context.Configuration);
                    services.AddDeskTodoApp();
                    services.AddDeskTodoPlatform();
                })
                .Build();

            // Start (not Run) — this only starts hosted services in the background.
            // Avalonia's StartWithClassicDesktopLifetime below owns the blocking main loop.
            host.Start();
            App.Services = host.Services;

            host.MigrateDeskTodoDatabaseAsync().GetAwaiter().GetResult();

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (HostAbortedException)
        {
            // Thrown deliberately by `dotnet ef`'s design-time tooling once it has
            // captured the host's service provider (e.g. while generating migrations)
            // — not a real failure, so it's rethrown without Fatal-level logging.
            throw;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "DeskTodo terminated unexpectedly");
        }
        finally
        {
            Log.Information("DeskTodo shutting down");

            if (host is not null)
            {
                host.StopAsync().GetAwaiter().GetResult();
                host.Dispose();
            }

            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
