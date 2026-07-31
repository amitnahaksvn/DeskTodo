using DeskTodo.Platform.Mac;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeskTodo.Tests.Infrastructure;

/// <summary>
/// Exercises <see cref="MacNotificationService"/> for real — this dev environment is
/// macOS, and <c>osascript -e 'display notification'</c> was confirmed (manually, once,
/// before writing this class at all) to work without any permission prompt from a plain
/// unsigned process. These tests don't assert a notification was actually seen (no API
/// exposes that), only that the whole pipeline — script construction, escaping, process
/// launch, exit code — completes without throwing, including for input designed to break
/// naive string interpolation into the AppleScript source.
/// </summary>
public class MacNotificationServiceTests
{
    [Fact]
    public async Task NotifyAsync_WithPlainText_CompletesWithoutThrowing()
    {
        var sut = new MacNotificationService(NullLogger<MacNotificationService>.Instance);

        await sut.NotifyAsync("DeskTodo test", "Plain notification body");
    }

    [Fact]
    public async Task NotifyAsync_WithQuotesAndBackslashes_DoesNotBreakTheAppleScript()
    {
        var sut = new MacNotificationService(NullLogger<MacNotificationService>.Instance);

        await sut.NotifyAsync(
            "Task \"with quotes\"",
            "Body with a backslash \\ and \"quotes\" and a newline-ish \\n sequence");
    }

    [Fact]
    public async Task NotifyAsync_AttemptedAppleScriptInjection_IsTreatedAsLiteralText()
    {
        // A title containing `" & do shell script "..."` would, if not escaped, let an
        // attacker-controlled task title (or category name, etc.) execute arbitrary shell
        // commands via AppleScript's `do shell script`. This only proves NotifyAsync doesn't
        // throw / doesn't hang — the actual injection-safety property (that this runs as an
        // inert string, not a script continuation) was verified by reading
        // MacNotificationService.AppleScriptString's escaping logic, not by this test alone.
        var sut = new MacNotificationService(NullLogger<MacNotificationService>.Instance);

        await sut.NotifyAsync(
            "\" & do shell script \"touch /tmp/desktodo-injection-canary\" & \"",
            "message");

        Assert.False(File.Exists("/tmp/desktodo-injection-canary"));
    }
}
