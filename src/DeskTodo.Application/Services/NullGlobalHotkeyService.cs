using DeskTodo.Application.Abstractions;

namespace DeskTodo.Application.Services;

/// <summary>No-op <see cref="IGlobalHotkeyService"/> for OSes without a platform-specific implementation (e.g. Linux) and for design time.</summary>
public sealed class NullGlobalHotkeyService : IGlobalHotkeyService
{
    public event EventHandler? Pressed { add { } remove { } }

    public bool Register() => false;

    public void Unregister()
    {
    }

    public void Dispose()
    {
    }
}
