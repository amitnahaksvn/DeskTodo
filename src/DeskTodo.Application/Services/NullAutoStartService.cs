using DeskTodo.Application.Abstractions;

namespace DeskTodo.Application.Services;

/// <summary>No-op <see cref="IAutoStartService"/> for OSes without a platform-specific implementation (e.g. Linux) and for design time.</summary>
public sealed class NullAutoStartService : IAutoStartService
{
    public bool IsEnabled => false;

    public void Enable()
    {
    }

    public void Disable()
    {
    }
}
