using System.Text.RegularExpressions;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="ISensitiveDataDetector"/>
/// <remarks>
/// Deterministic, entirely local pattern matching — no network call, matching the spec's own
/// "never transmit the content externally just to perform detection" requirement trivially, since
/// there is nothing here that could transmit anything. Patterns cover this feature's own
/// "Potential patterns" list: cloud-provider API keys (AWS/Google/GitHub/Slack/Stripe — each
/// vendor's key format is distinctive enough for a low-false-positive regex), JWTs, PEM private
/// key blocks, and a generic password/secret/token keyword-plus-value heuristic for everything
/// else (connection strings included, since "Password=..." inside one matches that same pattern).
/// </remarks>
public sealed partial class RegexSensitiveDataDetector : ISensitiveDataDetector
{
    public IReadOnlyList<SensitiveDataMatch> Detect(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var matches = new List<SensitiveDataMatch>();
        AddMatches(matches, "AWS Access Key", AwsAccessKeyRegex(), text);
        AddMatches(matches, "Google API Key", GoogleApiKeyRegex(), text);
        AddMatches(matches, "GitHub Token", GitHubTokenRegex(), text);
        AddMatches(matches, "Slack Token", SlackTokenRegex(), text);
        AddMatches(matches, "Stripe Live Key", StripeKeyRegex(), text);
        AddMatches(matches, "JWT", JwtRegex(), text);
        AddMatches(matches, "Private Key Block", PrivateKeyRegex(), text);
        AddMatches(matches, "Password/Secret", PasswordKeywordRegex(), text);

        return matches;
    }

    private static void AddMatches(List<SensitiveDataMatch> matches, string patternName, Regex regex, string text)
    {
        foreach (Match match in regex.Matches(text))
        {
            matches.Add(new SensitiveDataMatch(patternName, match.Value, match.Index, match.Length));
        }
    }

    [GeneratedRegex(@"\bAKIA[0-9A-Z]{16}\b")]
    private static partial Regex AwsAccessKeyRegex();

    [GeneratedRegex(@"\bAIza[0-9A-Za-z\-_]{35}\b")]
    private static partial Regex GoogleApiKeyRegex();

    [GeneratedRegex(@"\bgh[pousr]_[A-Za-z0-9]{36,}\b")]
    private static partial Regex GitHubTokenRegex();

    [GeneratedRegex(@"\bxox[baprs]-[0-9A-Za-z-]{10,}\b")]
    private static partial Regex SlackTokenRegex();

    [GeneratedRegex(@"\bsk_live_[0-9a-zA-Z]{24,}\b")]
    private static partial Regex StripeKeyRegex();

    [GeneratedRegex(@"\beyJ[A-Za-z0-9_-]{10,}\.eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\b")]
    private static partial Regex JwtRegex();

    [GeneratedRegex(@"-----BEGIN ((RSA|EC|DSA|OPENSSH|PGP) )?PRIVATE KEY-----")]
    private static partial Regex PrivateKeyRegex();

    [GeneratedRegex(@"\b(password|passwd|secret|api[_-]?key|access[_-]?token)\s*[:=]\s*['""]?([^\s'""]{6,})", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordKeywordRegex();
}
