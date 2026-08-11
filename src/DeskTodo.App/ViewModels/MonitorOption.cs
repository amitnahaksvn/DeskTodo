using DeskTodo.Application.Settings;

namespace DeskTodo.App.ViewModels;

/// <summary>A monitor entry in Settings' placement picker. <see cref="Id"/> is opaque — see <c>MonitorIdentity</c> for how it's built/resolved.</summary>
public sealed record MonitorOption(string Id, string Label)
{
    /// <summary>"Use the current/default position" — <see cref="Id"/> is empty, which <see cref="AppSettings.PreferredMonitorId"/> maps to <c>null</c>.</summary>
    public static readonly MonitorOption Unspecified = new(string.Empty, "Default (current position)");

    public override string ToString() => Label;
}
