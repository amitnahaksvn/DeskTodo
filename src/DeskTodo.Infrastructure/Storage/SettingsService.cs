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

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var path = GetSettingsFilePath();

        if (!File.Exists(path))
        {
            return new AppSettings();
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken);
            return settings ?? new AppSettings();
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
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "Failed to write settings file at {Path}", path);
        }
    }

    private string GetSettingsFilePath() =>
        Path.Combine(storageOptions.Value.RootDirectory, storageOptions.Value.SettingsFileName);
}
