namespace DeskTodo.Infrastructure.Storage;

/// <summary>
/// Resolves the OS-appropriate per-user application data root when
/// <see cref="Application.Options.AppStorageOptions.RootDirectory"/> is not
/// explicitly configured.
/// </summary>
public static class AppStoragePaths
{
    private const string AppFolderName = "DeskTodo";

    /// <summary>
    /// Windows: %LOCALAPPDATA%\DeskTodo. macOS: ~/Library/Application Support/DeskTodo.
    /// Anything else (e.g. Linux): the XDG-mapped local application data folder.
    /// </summary>
    public static string ResolveDefaultRootDirectory()
    {
        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", AppFolderName);
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, AppFolderName);
    }
}
