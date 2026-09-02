using DeskTodo.Application.Services;

namespace DeskTodo.Tests.Application;

public class LocalApiAuthenticatorTests
{
    [Fact]
    public void IsAuthorized_WithTheCorrectBearerToken_ReturnsTrue()
    {
        Assert.True(LocalApiAuthenticator.IsAuthorized("Bearer abc123", "abc123"));
    }

    [Fact]
    public void IsAuthorized_IsCaseInsensitiveAboutTheBearerScheme()
    {
        Assert.True(LocalApiAuthenticator.IsAuthorized("bearer abc123", "abc123"));
    }

    [Fact]
    public void IsAuthorized_WithTheWrongToken_ReturnsFalse()
    {
        Assert.False(LocalApiAuthenticator.IsAuthorized("Bearer wrong", "abc123"));
    }

    [Fact]
    public void IsAuthorized_WithNoHeader_ReturnsFalse()
    {
        Assert.False(LocalApiAuthenticator.IsAuthorized(null, "abc123"));
    }

    [Fact]
    public void IsAuthorized_WithoutTheBearerScheme_ReturnsFalse()
    {
        Assert.False(LocalApiAuthenticator.IsAuthorized("abc123", "abc123"));
    }

    [Fact]
    public void IsAuthorized_WhenNoTokenIsConfigured_ReturnsFalse()
    {
        Assert.False(LocalApiAuthenticator.IsAuthorized("Bearer abc123", null));
    }

    [Fact]
    public void IsAuthorized_WithATokenThatIsAPrefixOfTheExpectedOne_ReturnsFalse()
    {
        Assert.False(LocalApiAuthenticator.IsAuthorized("Bearer abc", "abc123"));
    }
}
