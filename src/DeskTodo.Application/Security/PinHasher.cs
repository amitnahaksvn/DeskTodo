using System.Security.Cryptography;

namespace DeskTodo.Application.Security;

/// <summary>
/// PBKDF2-based PIN hashing for Phase 29's PIN Lock. Uses
/// <see cref="Rfc2898DeriveBytes.Pbkdf2(string, byte[], int, HashAlgorithmName, int)"/> —
/// built into the BCL since .NET 6, so this needs no new NuGet dependency. A PIN is short
/// and deliberately low-entropy by nature (this is a quick local-desktop gate against casual
/// snooping, not a real multi-user authentication system), so this doesn't aim for
/// password-manager-grade KDF tuning — just "never stored in plaintext, not trivially
/// reversible from the settings file."
/// </summary>
public static class PinHasher
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int Iterations = 100_000;

    /// <summary>Hashes <paramref name="pin"/> with a freshly-generated random salt — call once when a PIN is first set or changed.</summary>
    public static (string Salt, string Hash) Hash(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
        return (Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    /// <summary>True if <paramref name="pin"/> hashes (with the stored salt) to <paramref name="expectedHash"/>. False for any malformed/missing input rather than throwing — a locked-out state should never crash the lock screen.</summary>
    public static bool Verify(string pin, string? salt, string? expectedHash)
    {
        if (string.IsNullOrEmpty(pin) || string.IsNullOrEmpty(salt) || string.IsNullOrEmpty(expectedHash))
        {
            return false;
        }

        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            var expectedHashBytes = Convert.FromBase64String(expectedHash);
            var computedHash = Rfc2898DeriveBytes.Pbkdf2(pin, saltBytes, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
            return CryptographicOperations.FixedTimeEquals(computedHash, expectedHashBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
