namespace DeskTodo.Domain.Entities;

/// <summary>
/// A file attached to a <see cref="TaskItem"/>. The file itself is copied into the app's
/// own data directory (see <c>AppStorageOptions.RootDirectory</c>) under a name derived
/// from <see cref="Id"/> — <see cref="FileName"/> is only the original, user-facing name,
/// kept separate so re-attaching two files with the same name never collides on disk.
/// </summary>
public sealed class Attachment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required Guid TaskId { get; set; }

    public TaskItem? Task { get; set; }

    public required string FileName { get; set; }

    /// <summary>Path on disk, relative to <c>AppStorageOptions.RootDirectory</c> — never an absolute path, so the whole data directory stays relocatable.</summary>
    public required string StoredRelativePath { get; set; }

    public long FileSizeBytes { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
