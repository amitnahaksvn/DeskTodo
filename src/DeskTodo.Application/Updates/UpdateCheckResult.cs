namespace DeskTodo.Application.Updates;

/// <summary>
/// Result of Phase 30's on-demand update check. <see cref="ErrorMessage"/> being set means
/// the check itself failed (offline, GitHub API unreachable, etc.) — distinct from
/// <see cref="IsUpdateAvailable"/> being false, which means the check succeeded and the app
/// is already current. No releases published yet on GitHub is treated as "already current,"
/// not an error — it's the expected state for a project that hasn't cut its first release.
/// </summary>
public sealed record UpdateCheckResult(bool IsUpdateAvailable, string? LatestVersion, string? ReleaseUrl, string? ErrorMessage);
