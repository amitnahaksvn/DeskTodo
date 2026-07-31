using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Settings;

namespace DeskTodo.App.DesignTime;

/// <summary>
/// No-op <see cref="ISettingsService"/> used only as a fallback when
/// <see cref="App.Services"/> is null — i.e. at XAML-designer time, which
/// never runs through <c>Program.Main</c>'s DI container.
/// </summary>
internal sealed class DesignTimeSettingsService : ISettingsService
{
    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new AppSettings());

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
