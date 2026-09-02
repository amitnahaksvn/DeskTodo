namespace DeskTodo.Application.Services;

/// <summary>One credential-shaped match <see cref="ISensitiveDataDetector"/> found in a piece of text.</summary>
public sealed record SensitiveDataMatch(string PatternName, string MatchedText, int Index, int Length);

/// <summary>
/// Feature 76 (Roadmap-39-100.md) — warns when task content looks like it contains a credential.
/// Kept as an interface, same "deterministic first, AI-pluggable later" reasoning already
/// established for <see cref="IQuickAddParser"/> (Feature 41) and <see cref="IMeetingActionExtractor"/>
/// (Feature 59). Every implementation must run entirely locally — this feature's own spec is
/// explicit that content is never transmitted externally just to perform detection.
/// </summary>
public interface ISensitiveDataDetector
{
    IReadOnlyList<SensitiveDataMatch> Detect(string text);
}
