namespace DeskTodo.Application.Abstractions;

/// <summary>
/// Shows a native OS notification. Implemented per-platform in
/// <c>DeskTodo.Platform.Windows</c>/<c>DeskTodo.Platform.Mac</c> — see
/// docs/ARCHITECTURE.md's "Phase 13" section for why each does it the way
/// it does. A no-op fallback covers unsupported OSes and design time.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// <paramref name="playSound"/> is Phase 26's Sound Notification setting
    /// (<see cref="Settings.AppSettings.NotificationSoundEnabled"/>) — the caller decides
    /// whether this particular notification should play a sound, this interface just carries
    /// that choice through to the platform implementation. Defaults to <c>true</c> (today's
    /// existing behavior, since every platform's notification facility already plays some
    /// sound by default) so existing call sites are unaffected.
    /// </summary>
    Task NotifyAsync(string title, string message, bool playSound = true, CancellationToken cancellationToken = default);
}
