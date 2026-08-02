using System.Text.Json;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Options;
using DeskTodo.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeskTodo.Infrastructure.Storage;

/// <inheritdoc cref="ISettingsService"/>
public sealed class SettingsService(IOptions<AppStorageOptions> storageOptions, ILogger<SettingsService> logger) : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // Every await below is ConfigureAwait(false): both LoadAsync and SaveAsync are called
    // synchronously (.GetAwaiter().GetResult()) from the Avalonia UI thread — App.axaml.cs
    // at startup and WidgetWindow.axaml.cs on close — and once Avalonia's UI-thread
    // SynchronizationContext is installed, an unconfigured await here would try to resume
    // on that same (blocked) thread and deadlock forever. This was a real, timing-dependent
    // hang: a tiny local JSON read/write usually completes synchronously and never actually
    // yields, so the deadlock only surfaces when the I/O is slow enough to force a real
    // await — confirmed live via a process sample showing the main thread parked in
    // Monitor_Wait with zero windows ever created.
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var path = GetSettingsFilePath();

        if (!File.Exists(path))
        {
            return new AppSettings();
        }

        try
        {
            var stream = File.OpenRead(path);
            await using (stream.ConfigureAwait(false))
            {
                var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
                return settings ?? new AppSettings();
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            logger.LogWarning(ex, "Failed to read settings file at {Path}; falling back to defaults", path);
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var path = GetSettingsFilePath();

        try
        {
            var stream = File.Create(path);
            await using (stream.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "Failed to write settings file at {Path}", path);
        }
    }

    private string GetSettingsFilePath() =>
        Path.Combine(storageOptions.Value.RootDirectory, storageOptions.Value.SettingsFileName);
}
