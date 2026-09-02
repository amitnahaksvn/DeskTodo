namespace DeskTodo.App.ViewModels;

/// <summary>What the user chose on the Sensitive Data Detector's warning dialog (Feature 76, Roadmap-39-100.md).</summary>
public sealed record SensitiveDataPromptResult(bool ShouldSave, bool RemoveFlagged, bool DontWarnAgain)
{
    /// <summary>Closed via the OS close button (or any other non-choice) — the save is aborted, same "never silently proceed" convention as <c>ConfirmDialogWindow</c>.</summary>
    public static readonly SensitiveDataPromptResult Cancelled = new(false, false, false);
}
