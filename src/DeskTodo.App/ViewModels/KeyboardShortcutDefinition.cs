using Avalonia.Input;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// One re-bindable shortcut — Feature 77 (Roadmap-39-100.md). <see cref="DefaultCombo"/> and any
/// user override (<see cref="Application.Settings.AppSettings.KeyboardShortcutOverrides"/>) are
/// stored as an OS-neutral string like <c>"Mod+K"</c> or <c>"Mod+Shift+Z"</c> — "Mod" resolves to
/// Cmd on macOS / Ctrl elsewhere at bind time, the same per-OS resolution
/// <c>WidgetWindow.RegisterKeyboardShortcuts</c> already used before this feature existed. Every
/// shortcut requires "Mod" (optionally plus Shift) — not an arbitrary modifier combination — matching
/// the shape every one of the app's existing built-in shortcuts already has.
/// </summary>
public sealed record KeyboardShortcutDefinition(string CommandId, string DisplayName, string DefaultCombo, string Scope)
{
    /// <summary>The fixed set of shortcuts this app currently exposes — <see cref="WidgetViewModel"/>'s own commands, all "Application" scope today (no per-surface scopes like Task Editor/Calendar/Grid exist yet).</summary>
    public static readonly IReadOnlyList<KeyboardShortcutDefinition> All =
    [
        new("CommandPalette", "Open Command Palette", "Mod+K", "Application"),
        new("ToggleSearch", "Toggle Search & Filter", "Mod+F", "Application"),
        new("Settings", "Open Settings", "Mod+OemComma", "Application"),
        new("Undo", "Undo", "Mod+Z", "Application"),
        new("Redo", "Redo", "Mod+Shift+Z", "Application"),
    ];

    /// <summary>Parses a combo string (e.g. <c>"Mod+Shift+Z"</c>) into a real <see cref="KeyGesture"/>, resolving "Mod" to the platform's own primary modifier. Returns null for a malformed combo rather than throwing — a corrupt/hand-edited settings file shouldn't crash shortcut registration.</summary>
    public static KeyGesture? TryParseGesture(string combo, KeyModifiers platformModifier)
    {
        var parts = combo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var modifiers = KeyModifiers.None;
        Key? key = null;

        foreach (var part in parts)
        {
            if (string.Equals(part, "Mod", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= platformModifier;
            }
            else if (string.Equals(part, "Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= KeyModifiers.Shift;
            }
            else if (Enum.TryParse<Key>(part, ignoreCase: true, out var parsedKey))
            {
                key = parsedKey;
            }
            else
            {
                return null;
            }
        }

        return key is { } resolvedKey ? new KeyGesture(resolvedKey, modifiers) : null;
    }

    /// <summary>Formats a captured key press back into the OS-neutral combo string this app persists — the inverse of <see cref="TryParseGesture"/>.</summary>
    public static string? TryFormatCombo(Key key, KeyModifiers pressedModifiers, KeyModifiers platformModifier)
    {
        if (!pressedModifiers.HasFlag(platformModifier))
        {
            return null; // every re-bindable shortcut in this app requires the primary modifier.
        }

        var parts = new List<string> { "Mod" };
        if (pressedModifiers.HasFlag(KeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        parts.Add(key.ToString());
        return string.Join('+', parts);
    }
}
