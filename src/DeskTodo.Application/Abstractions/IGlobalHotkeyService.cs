namespace DeskTodo.Application.Abstractions;

/// <summary>
/// Registers Quick Add's single systemwide keyboard shortcut — <see cref="Pressed"/> fires
/// even when DeskTodo isn't the focused app. Implemented per-platform in
/// <c>DeskTodo.Platform.Windows</c>/<c>DeskTodo.Platform.Mac</c> — see docs/ARCHITECTURE.md's
/// "Phase 22" section. This is not a general-purpose multi-binding hotkey manager: one fixed
/// shortcut (Cmd/Ctrl+Shift+N), one purpose.
/// </summary>
public interface IGlobalHotkeyService : IDisposable
{
    /// <summary>
    /// Raised when the shortcut is pressed. macOS delivers this on the app's main/UI thread
    /// already, but Windows fires it from a dedicated Win32 message-loop thread — subscribers
    /// must marshal to the Avalonia dispatcher themselves before touching UI.
    /// </summary>
    event EventHandler? Pressed;

    /// <summary>
    /// Registers the shortcut. Returns false if the OS refused it (e.g. already claimed by
    /// another app) — callers should treat that as "the feature quietly isn't available,"
    /// not a fatal error.
    /// </summary>
    bool Register();

    void Unregister();
}
