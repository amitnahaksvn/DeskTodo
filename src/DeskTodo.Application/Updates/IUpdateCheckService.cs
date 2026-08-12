namespace DeskTodo.Application.Updates;

/// <summary>
/// Phase 30's Auto-update system, scoped down to an on-demand check — see
/// <c>docs/ARCHITECTURE.md</c>'s "Phase 30" section for why actually downloading/installing
/// an update isn't part of this. Deliberately not polled automatically on a timer: the app
/// makes no outbound network calls otherwise (SQLite/JSON settings are the only I/O it does),
/// and an update check should be something the user asks for, not something that happens
/// silently in the background the first time this ships.
/// </summary>
public interface IUpdateCheckService
{
    Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default);
}
