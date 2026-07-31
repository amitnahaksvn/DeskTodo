using DeskTodo.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace DeskTodo.Platform.Mac;

/// <summary>
/// Registers DeskTodo as a per-user LaunchAgent by writing/removing a plist under
/// <c>~/Library/LaunchAgents</c>. Deliberately just a file write — no
/// <c>launchctl bootstrap</c>/<c>load</c> call to activate it for the *current* login
/// session — so <see cref="Enable"/> only touches this repo-external, real user directory
/// when a test explicitly asks it to (see <see cref="MacAutoStartService(string, ILogger{MacAutoStartService})"/>)
/// rather than as a side effect of exercising this class; the tradeoff is that enabling
/// auto-start takes effect at the next login, not immediately. macOS auto-loads a
/// LaunchAgent plist present at login time with no further registration step needed.
/// </summary>
public sealed class MacAutoStartService : IAutoStartService
{
    private const string Label = "com.desktodo.app";

    private readonly string _plistPath;
    private readonly ILogger<MacAutoStartService> _logger;

    public MacAutoStartService(ILogger<MacAutoStartService> logger)
        : this(ResolveDefaultPlistPath(), logger)
    {
    }

    internal MacAutoStartService(string plistPath, ILogger<MacAutoStartService> logger)
    {
        _plistPath = plistPath;
        _logger = logger;
    }

    public bool IsEnabled => File.Exists(_plistPath);

    public void Enable()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executablePath))
        {
            _logger.LogWarning("Could not resolve the current executable path; skipping auto-start registration");
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_plistPath)!);
            File.WriteAllText(_plistPath, BuildPlist(executablePath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to enable auto-start");
        }
    }

    public void Disable()
    {
        try
        {
            if (File.Exists(_plistPath))
            {
                File.Delete(_plistPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to disable auto-start");
        }
    }

    internal static string ResolveDefaultPlistPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "Library", "LaunchAgents", $"{Label}.plist");
    }

    private static string BuildPlist(string executablePath) => $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
        <plist version="1.0">
        <dict>
        <key>Label</key>
        <string>{Label}</string>
        <key>ProgramArguments</key>
        <array>
        <string>{XmlEscape(executablePath)}</string>
        </array>
        <key>RunAtLoad</key>
        <true/>
        </dict>
        </plist>
        """;

    private static string XmlEscape(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
