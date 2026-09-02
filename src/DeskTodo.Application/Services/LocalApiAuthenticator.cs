using System.Security.Cryptography;
using System.Text;

namespace DeskTodo.Application.Services;

/// <summary>Bearer-token check for Feature 97's Local REST API — "Require authentication" / "Use API tokens" from that feature's own spec.</summary>
public static class LocalApiAuthenticator
{
    public static bool IsAuthorized(string? authorizationHeader, string? expectedToken)
    {
        if (string.IsNullOrEmpty(expectedToken) || string.IsNullOrEmpty(authorizationHeader))
        {
            return false;
        }

        const string prefix = "Bearer ";
        if (!authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var provided = authorizationHeader[prefix.Length..].Trim();
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedToken);

        // Constant-time comparison (same reasoning as PinHasher's PIN check, Phase 29) — a
        // length-dependent early-return via a naive == would leak how many leading characters
        // matched through response timing.
        return providedBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
