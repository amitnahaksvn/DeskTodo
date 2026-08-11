using DeskTodo.Platform.Mac;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeskTodo.Tests.Infrastructure;

/// <summary>
/// Exercises <see cref="MacGlobalHotkeyService"/> for real (this dev environment is macOS) —
/// registering/unregistering Cmd+Shift+N against the actual Carbon APIs is harmless as long
/// as it's always paired with <see cref="MacGlobalHotkeyService.Unregister"/>, same safety
/// argument as <see cref="MacAutoStartServiceTests"/> makes for its own real-OS exercise.
/// Simulating an actual keypress isn't attempted here — too fragile/slow for a unit test —
/// that was verified live once during Phase 22 development instead (see docs/ARCHITECTURE.md).
/// </summary>
public sealed class MacGlobalHotkeyServiceTests
{
    [Fact]
    public void Register_SucceedsAgainstTheRealCarbonApis()
    {
        using var sut = new MacGlobalHotkeyService(NullLogger<MacGlobalHotkeyService>.Instance);

        var registered = sut.Register();

        Assert.True(registered);
    }

    [Fact]
    public void Unregister_WithNoPriorRegister_IsANoOp()
    {
        using var sut = new MacGlobalHotkeyService(NullLogger<MacGlobalHotkeyService>.Instance);

        sut.Unregister(); // Shouldn't throw.
    }

    [Fact]
    public void Register_ThenUnregister_ThenRegisterAgain_Succeeds()
    {
        using var sut = new MacGlobalHotkeyService(NullLogger<MacGlobalHotkeyService>.Instance);
        Assert.True(sut.Register());

        sut.Unregister();

        Assert.True(sut.Register());
        sut.Unregister();
    }
}
