using System.Runtime.Versioning;
using DeskTodo.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace DeskTodo.Platform.Windows;

/// <summary>
/// Registers DeskTodo to auto-start via the per-user
/// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c> registry key — the standard,
/// no-admin-rights way to do this, as opposed to a Startup-folder shortcut (which needs a
/// <c>.lnk</c> file built via COM's <c>IShellLink</c>, meaningfully more interop for the
/// same result) or Task Scheduler (needs elevated rights for a per-user login trigger in
/// the common case).
///
/// <b>Authored but not runtime-verified</b> — the equivalent macOS implementation
/// (<c>DeskTodo.Platform.Mac.MacAutoStartService</c>) is exercised by real tests; this one
/// can't be, since this dev environment has no Windows registry to test against.
///
/// <see cref="SupportedOSPlatformAttribute"/> documents the Windows-only contract to the
/// platform-compatibility analyzer — this type is only ever constructed from
/// <c>PlatformServiceCollectionExtensions</c> behind an <c>OperatingSystem.IsWindows()</c>
/// check, so the analyzer's cross-platform warnings on the registry calls below are false
/// positives without it.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsAutoStartService(ILogger<WindowsAutoStartService> logger) : IAutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DeskTodo";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is not null;
        }
    }

    public void Enable()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executablePath))
        {
            logger.LogWarning("Could not resolve the current executable path; skipping auto-start registration");
            return;
        }

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            key.SetValue(ValueName, $"\"{executablePath}\"");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            logger.LogError(ex, "Failed to enable auto-start");
        }
    }

    public void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            logger.LogError(ex, "Failed to disable auto-start");
        }
    }
}
