using System.Diagnostics;
using DeskTodo.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace DeskTodo.Platform.Mac;

/// <summary>
/// Shows a native macOS notification via <c>osascript -e 'display notification ...'</c>.
/// Chosen over binding <c>NSUserNotificationCenter</c>/<c>UNUserNotificationCenter</c>
/// directly (which would need Objective-C runtime interop and, for the modern
/// <c>UNUserNotificationCenter</c> API, a proper app bundle identity) because
/// <c>display notification</c> works from a plain unsigned process with no special
/// entitlements or permission prompt — confirmed by actually running it in this dev
/// environment, not assumed.
/// </summary>
public sealed class MacNotificationService(ILogger<MacNotificationService> logger) : INotificationService
{
    public async Task NotifyAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        var script = $"display notification {AppleScriptString(message)} with title {AppleScriptString(title)}";

        try
        {
            using var process = Process.Start(new ProcessStartInfo("osascript", ["-e", script])
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });

            if (process is not null)
            {
                await process.WaitForExitAsync(cancellationToken);
                if (process.ExitCode != 0)
                {
                    var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
                    logger.LogWarning("osascript exited with code {ExitCode} showing a notification: {Error}", process.ExitCode, stderr);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to show macOS notification {Title}", title);
        }
    }

    // AppleScript string literals: backslash and double-quote need escaping. Guards against
    // both injection (a task title like `" & do shell script "rm -rf ~` breaking out of the
    // string) and plain syntax errors from titles containing quotes.
    private static string AppleScriptString(string value) =>
        "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
