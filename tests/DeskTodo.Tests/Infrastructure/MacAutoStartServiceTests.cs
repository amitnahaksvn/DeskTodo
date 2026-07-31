using DeskTodo.Platform.Mac;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeskTodo.Tests.Infrastructure;

/// <summary>
/// Exercises <see cref="MacAutoStartService"/> for real (this dev environment is macOS),
/// against a scratch plist path — never the real <c>~/Library/LaunchAgents</c>, since that's
/// live user system state outside this repo that a test run shouldn't touch. See the
/// class's own doc comment for why <c>Enable()</c> is a plain file write with no
/// <c>launchctl</c> call, which is exactly what keeps this safe to test at all.
/// </summary>
public sealed class MacAutoStartServiceTests : IDisposable
{
    // xunit creates a fresh instance (and re-runs this field initializer) per test method, so
    // each test already gets its own isolated scratch directory — no shared-state risk even
    // if tests run in parallel.
    private readonly string _plistPath = Path.Combine(Path.GetTempPath(), "DeskTodoTests", Guid.NewGuid().ToString("N"), "com.desktodo.app.plist");

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_plistPath)!;
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void IsEnabled_WithNoPlistOnDisk_IsFalse()
    {
        var sut = new MacAutoStartService(_plistPath, NullLogger<MacAutoStartService>.Instance);

        Assert.False(sut.IsEnabled);
    }

    [Fact]
    public void Enable_WritesAValidPlist_AndIsEnabledBecomesTrue()
    {
        var sut = new MacAutoStartService(_plistPath, NullLogger<MacAutoStartService>.Instance);

        sut.Enable();

        Assert.True(sut.IsEnabled);
        Assert.True(File.Exists(_plistPath));
        var content = File.ReadAllText(_plistPath);
        Assert.Contains("<key>Label</key>", content);
        Assert.Contains("com.desktodo.app", content);
        Assert.Contains("<key>RunAtLoad</key>", content);
        Assert.Contains(Environment.ProcessPath!, content);
    }

    [Fact]
    public void Disable_RemovesThePlist_AndIsEnabledBecomesFalse()
    {
        var sut = new MacAutoStartService(_plistPath, NullLogger<MacAutoStartService>.Instance);
        sut.Enable();

        sut.Disable();

        Assert.False(sut.IsEnabled);
        Assert.False(File.Exists(_plistPath));
    }

    [Fact]
    public void Disable_WithNoPlistOnDisk_IsANoOp()
    {
        var sut = new MacAutoStartService(_plistPath, NullLogger<MacAutoStartService>.Instance);

        sut.Disable(); // Shouldn't throw.

        Assert.False(sut.IsEnabled);
    }
}
