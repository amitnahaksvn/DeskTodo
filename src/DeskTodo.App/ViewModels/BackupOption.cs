using DeskTodo.Application.Abstractions;

namespace DeskTodo.App.ViewModels;

/// <summary>One <see cref="BackupInfo"/> row as shown in <see cref="BackupViewModel"/>'s list — Feature 67 (Roadmap-39-100.md).</summary>
public sealed record BackupOption(string FilePath, string FileName, string CreatedAtDisplay, string SizeDisplay)
{
    public static BackupOption FromInfo(BackupInfo info) =>
        new(info.FilePath, info.FileName, info.CreatedAt.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt"), FormatSize(info.SizeBytes));

    private static string FormatSize(long bytes) =>
        bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
            _ => $"{bytes / (1024.0 * 1024.0):0.#} MB",
        };
}
