namespace DeskTodo.Application.Abstractions;

/// <summary>
/// Shows a native OS notification. Implemented per-platform in
/// <c>DeskTodo.Platform.Windows</c>/<c>DeskTodo.Platform.Mac</c> — see
/// docs/ARCHITECTURE.md's "Phase 13" section for why each does it the way
/// it does. A no-op fallback covers unsupported OSes and design time.
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(string title, string message, CancellationToken cancellationToken = default);
}
