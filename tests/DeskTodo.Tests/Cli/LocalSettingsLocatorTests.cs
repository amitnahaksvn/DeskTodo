using DeskTodo.Cli;

namespace DeskTodo.Tests.Cli;

public class LocalSettingsLocatorTests : IDisposable
{
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), $"desktodo-cli-tests-{Guid.NewGuid()}.json");

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    [Fact]
    public void TryReadApiSettingsFrom_WithAMissingFile_ReturnsNulls()
    {
        var (port, token) = LocalSettingsLocator.TryReadApiSettingsFrom(_tempFile);

        Assert.Null(port);
        Assert.Null(token);
    }

    [Fact]
    public void TryReadApiSettingsFrom_WithAValidSettingsFile_ReadsPortAndToken()
    {
        File.WriteAllText(_tempFile, """{"LocalApiEnabled":true,"LocalApiPort":47291,"LocalApiToken":"abc123"}""");

        var (port, token) = LocalSettingsLocator.TryReadApiSettingsFrom(_tempFile);

        Assert.Equal(47291, port);
        Assert.Equal("abc123", token);
    }

    [Fact]
    public void TryReadApiSettingsFrom_WithNoLocalApiFields_ReturnsNulls()
    {
        File.WriteAllText(_tempFile, """{"Theme":"Dark"}""");

        var (port, token) = LocalSettingsLocator.TryReadApiSettingsFrom(_tempFile);

        Assert.Null(port);
        Assert.Null(token);
    }

    [Fact]
    public void TryReadApiSettingsFrom_WithMalformedJson_ReturnsNulls_AndDoesNotThrow()
    {
        File.WriteAllText(_tempFile, "not json");

        var (port, token) = LocalSettingsLocator.TryReadApiSettingsFrom(_tempFile);

        Assert.Null(port);
        Assert.Null(token);
    }
}
