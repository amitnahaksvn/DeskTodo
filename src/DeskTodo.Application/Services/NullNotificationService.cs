using DeskTodo.Application.Abstractions;

namespace DeskTodo.Application.Services;

/// <summary>
/// No-op <see cref="INotificationService"/> for OSes without a platform-specific
/// implementation (e.g. Linux) and for design time. Never throws, so callers don't need to
/// special-case "notifications aren't supported here."
/// </summary>
public sealed class NullNotificationService : INotificationService
{
    public Task NotifyAsync(string title, string message, bool playSound = true, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
