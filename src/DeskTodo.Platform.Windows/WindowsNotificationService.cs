using System.Diagnostics;
using System.Text;
using DeskTodo.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace DeskTodo.Platform.Windows;

/// <summary>
/// Shows a native Windows notification by shelling out to a short PowerShell script that
/// pops a <c>System.Windows.Forms.NotifyIcon</c> balloon tip. Mirrors
/// <c>DeskTodo.Platform.Mac.MacNotificationService</c>'s design (trust an always-present OS
/// scripting facility over raw P/Invoke) rather than calling <c>Shell_NotifyIcon</c>
/// directly, which would need a persistent message-only window to own the icon handle —
/// meaningfully more Win32 interop surface for the same result. Modern toast notifications
/// (<c>Windows.UI.Notifications</c>) were considered too but need a registered
/// AppUserModelID (effectively an MSIX package identity) to show reliably; a balloon tip
/// works from a plain unpackaged process.
///
/// <b>Authored but not runtime-verified</b> — this dev environment is macOS-only, so this
/// class has no equivalent of <c>DeskTodo.Platform.Mac.MacNotificationService</c>'s
/// "confirmed by actually running it" claim. See docs/ARCHITECTURE.md's "Phase 13" section.
/// </summary>
public sealed class WindowsNotificationService(ILogger<WindowsNotificationService> logger) : INotificationService
{
    /// <summary>
    /// <paramref name="playSound"/> is accepted for interface symmetry with
    /// <c>MacNotificationService</c> but has no effect here: a <c>NotifyIcon</c> balloon tip
    /// always plays the OS's default notification sound with no documented way to suppress
    /// just the sound while keeping the balloon — an honest, documented gap rather than a
    /// silently-ignored parameter.
    /// </summary>
    public async Task NotifyAsync(string title, string message, bool playSound = true, CancellationToken cancellationToken = default)
    {
        var script = $$"""
            Add-Type -AssemblyName System.Windows.Forms
            Add-Type -AssemblyName System.Drawing
            $icon = New-Object System.Windows.Forms.NotifyIcon
            $icon.Icon = [System.Drawing.SystemIcons]::Information
            $icon.Visible = $true
            $icon.BalloonTipTitle = {{PowerShellString(title)}}
            $icon.BalloonTipText = {{PowerShellString(message)}}
            $icon.ShowBalloonTip(5000)
            Start-Sleep -Seconds 6
            $icon.Dispose()
            """;
        var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

        try
        {
            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                ["-NoProfile", "-NonInteractive", "-WindowStyle", "Hidden", "-EncodedCommand", encodedCommand])
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is not null)
            {
                // Not awaited to full exit: the script deliberately sleeps ~6s to keep the
                // NotifyIcon alive long enough to be seen. A brief sanity check is enough to
                // catch "powershell.exe failed to even launch the script" without blocking
                // NotifyAsync's caller for the balloon's whole lifetime.
                await Task.Delay(200, cancellationToken);
                if (process.HasExited && process.ExitCode != 0)
                {
                    logger.LogWarning("powershell.exe exited early with code {ExitCode} showing a notification", process.ExitCode);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to show Windows notification {Title}", title);
        }
    }

    // PowerShell single-quoted string literals: '' escapes a literal single quote. Guards
    // against both injection and plain syntax errors from titles containing apostrophes.
    private static string PowerShellString(string value) => "'" + value.Replace("'", "''") + "'";
}
