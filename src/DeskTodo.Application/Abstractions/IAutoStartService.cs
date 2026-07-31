namespace DeskTodo.Application.Abstractions;

/// <summary>
/// Registers/unregisters DeskTodo to launch automatically when the user logs in.
/// Implemented per-platform in <c>DeskTodo.Platform.Windows</c>/<c>DeskTodo.Platform.Mac</c>
/// — see docs/ARCHITECTURE.md's "Phase 15" section. Synchronous: both implementations are
/// plain local file/registry I/O, fast enough not to need an async signature.
/// </summary>
public interface IAutoStartService
{
    bool IsEnabled { get; }

    void Enable();

    void Disable();
}
