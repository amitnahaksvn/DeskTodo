using DeskTodo.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace DeskTodo.Infrastructure.Logging;

/// <summary>
/// Wires Serilog into the generic host so the App composition root never
/// needs to reference Serilog directly beyond bootstrap logging.
/// </summary>
public static class SerilogHostingExtensions
{
    private const string LogFilePrefix = "desktodo-.log";

    /// <summary>
    /// Configures Serilog as the application's logging provider. Sink and level
    /// configuration is read declaratively from the "Serilog" configuration
    /// section; a rolling file sink under the resolved <see cref="AppStorageOptions"/>
    /// root is always added in code, since its path is only known at runtime.
    /// </summary>
    public static IHostBuilder UseDeskTodoLogging(this IHostBuilder hostBuilder) =>
        hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            var storage = services.GetRequiredService<IOptions<AppStorageOptions>>().Value;
            var logFilePath = Path.Combine(storage.RootDirectory, storage.LogsDirectoryName, LogFilePrefix);

            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    shared: true);
        });
}
