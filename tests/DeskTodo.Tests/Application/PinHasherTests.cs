using DeskTodo.Application.Security;

namespace DeskTodo.Tests.Application;

public class PinHasherTests
{
    [Fact]
    public void Hash_ThenVerify_WithTheSamePin_ReturnsTrue()
    {
        var (salt, hash) = PinHasher.Hash("4242");

        Assert.True(PinHasher.Verify("4242", salt, hash));
    }

    [Fact]
    public void Verify_WithAWrongPin_ReturnsFalse()
    {
        var (salt, hash) = PinHasher.Hash("4242");

        Assert.False(PinHasher.Verify("0000", salt, hash));
    }

    [Fact]
    public void Hash_NeverStoresThePlaintextPin()
    {
        var (salt, hash) = PinHasher.Hash("4242");

        Assert.DoesNotContain("4242", salt);
        Assert.DoesNotContain("4242", hash);
    }

    [Fact]
    public void Hash_TwoCallsWithTheSamePin_ProduceDifferentSaltsAndHashes()
    {
        var (salt1, hash1) = PinHasher.Hash("4242");
        var (salt2, hash2) = PinHasher.Hash("4242");

        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(hash1, hash2);
    }

    [Theory]
    [InlineData(null, "hash")]
    [InlineData("salt", null)]
    [InlineData(null, null)]
    [InlineData("", "")]
    public void Verify_WithMissingSaltOrHash_ReturnsFalse_RatherThanThrowing(string? salt, string? hash)
    {
        Assert.False(PinHasher.Verify("4242", salt, hash));
    }

    [Fact]
    public void Verify_WithMalformedBase64_ReturnsFalse_RatherThanThrowing()
    {
        Assert.False(PinHasher.Verify("4242", "not valid base64!!", "also not valid base64!!"));
    }
}
