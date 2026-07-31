using DeskTodo.Application.Settings;

namespace DeskTodo.Application.Abstractions;

/// <summary>Loads/saves the user's <see cref="AppSettings"/>. Implemented in Infrastructure as a JSON file under the app's storage root.</summary>
public interface ISettingsService
{
    /// <summary>Returns <c>new AppSettings()</c> defaults if no settings file exists yet (first run) or it can't be read.</summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
