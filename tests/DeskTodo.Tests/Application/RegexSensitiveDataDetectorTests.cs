using DeskTodo.Application.Services;

namespace DeskTodo.Tests.Application;

public class RegexSensitiveDataDetectorTests
{
    private readonly RegexSensitiveDataDetector _sut = new();

    [Fact]
    public void Detect_WithAnAwsAccessKey_FindsIt()
    {
        var matches = _sut.Detect("Key is AKIAIOSFODNN7EXAMPLE for the deploy user");

        Assert.Contains(matches, m => m.PatternName == "AWS Access Key" && m.MatchedText == "AKIAIOSFODNN7EXAMPLE");
    }

    [Fact]
    public void Detect_WithAGoogleApiKey_FindsIt()
    {
        var matches = _sut.Detect("AIzaSyD-9tSrke72PouQMnMX-a7eZSW0jkFMBWY is the maps key");

        Assert.Contains(matches, m => m.PatternName == "Google API Key");
    }

    [Fact]
    public void Detect_WithAGitHubToken_FindsIt()
    {
        var matches = _sut.Detect("token: ghp_1234567890abcdefghijklmnopqrstuvwxyzAB");

        Assert.Contains(matches, m => m.PatternName == "GitHub Token");
    }

    [Fact]
    public void Detect_WithASlackToken_FindsIt()
    {
        // Built from fragments (rather than one literal) so this fake, structurally-shaped
        // test fixture doesn't itself trip GitHub's push-protection secret scanner.
        var fakeSlackToken = string.Concat("xoxb", "-", "0000000000", "-", "0000000000000000");

        var matches = _sut.Detect(fakeSlackToken);

        Assert.Contains(matches, m => m.PatternName == "Slack Token");
    }

    [Fact]
    public void Detect_WithAStripeLiveKey_FindsIt()
    {
        var fakeStripeKey = string.Concat("sk", "_live_", "000000000000000000000000");

        var matches = _sut.Detect(fakeStripeKey);

        Assert.Contains(matches, m => m.PatternName == "Stripe Live Key");
    }

    [Fact]
    public void Detect_WithAJwt_FindsIt()
    {
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        var matches = _sut.Detect($"Auth header: {jwt}");

        Assert.Contains(matches, m => m.PatternName == "JWT" && m.MatchedText == jwt);
    }

    [Fact]
    public void Detect_WithAPemPrivateKeyBlock_FindsIt()
    {
        var matches = _sut.Detect("-----BEGIN RSA PRIVATE KEY-----\nMIIEow...\n-----END RSA PRIVATE KEY-----");

        Assert.Contains(matches, m => m.PatternName == "Private Key Block");
    }

    [Theory]
    [InlineData("password: hunter22")]
    [InlineData("Password=SuperSecret1")]
    [InlineData("api_key: abc123xyz")]
    [InlineData("secret=my-app-secret")]
    public void Detect_WithAPasswordOrSecretKeywordAndValue_FindsIt(string text)
    {
        var matches = _sut.Detect(text);

        Assert.Contains(matches, m => m.PatternName == "Password/Secret");
    }

    [Fact]
    public void Detect_WithOrdinaryTaskText_FindsNothing()
    {
        var matches = _sut.Detect("Buy groceries and call the plumber about the leaking pipe by Friday.");

        Assert.Empty(matches);
    }

    [Fact]
    public void Detect_WithEmptyText_FindsNothing()
    {
        Assert.Empty(_sut.Detect(string.Empty));
    }

    [Fact]
    public void Detect_ReportsTheCorrectIndexAndLength_SoTheMatchCanBeSplicedOut()
    {
        const string text = "prefix AKIAIOSFODNN7EXAMPLE suffix";

        var match = Assert.Single(_sut.Detect(text));

        Assert.Equal(text.Substring(match.Index, match.Length), match.MatchedText);
    }
}
