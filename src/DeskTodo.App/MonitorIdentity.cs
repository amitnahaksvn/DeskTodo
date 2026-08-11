using Avalonia.Controls;
using Avalonia.Platform;

namespace DeskTodo.App;

/// <summary>
/// Avalonia's <see cref="Screen"/> exposes no native unique/stable ID cross-platform (just
/// <see cref="Screen.DisplayName"/>, <see cref="Screen.Bounds"/>, etc. — confirmed via
/// reflection against the compiled 12.1.0 package, the same discipline as every other
/// Avalonia API this project depends on). <see cref="Screen.DisplayName"/>+<see cref="Screen.Bounds"/>
/// combined is the best available stand-in: stable across a session and across reboots as
/// long as the monitor arrangement itself doesn't change, which is exactly the case Phase
/// 22's "Multi Monitor Support" needs to handle gracefully (falling back to the primary
/// screen), not guarantee against.
/// </summary>
public static class MonitorIdentity
{
    public static string GetId(Screen screen) => $"{screen.DisplayName}|{screen.Bounds}";

    public static string GetLabel(Screen screen) =>
        $"{screen.DisplayName} ({screen.Bounds.Width}×{screen.Bounds.Height})" + (screen.IsPrimary ? " — Primary" : string.Empty);

    /// <summary>Null if <paramref name="id"/> is null/empty ("use the default placement") or no longer matches any connected screen (it was unplugged or the arrangement changed) — callers should fall back to <see cref="Screens.Primary"/> in that case.</summary>
    public static Screen? Resolve(Screens screens, string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return screens.All.FirstOrDefault(screen => GetId(screen) == id);
    }
}
