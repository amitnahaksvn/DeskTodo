using DeskTodo.Application.Options;
using DeskTodo.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DeskTodo.Tests.Infrastructure;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInfrastructure_WithoutConfiguredRootDirectory_ResolvesOsDefaultPath()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<AppStorageOptions>>().Value;

        Assert.False(string.IsNullOrWhiteSpace(options.RootDirectory));
        Assert.True(Directory.Exists(options.RootDirectory));
    }

    [Fact]
    public void AddInfrastructure_WithConfiguredRootDirectory_UsesConfiguredValue()
    {
        var expectedRoot = Path.Combine(Path.GetTempPath(), "DeskTodoTests", Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{AppStorageOptions.SectionName}:RootDirectory"] = expectedRoot,
            })
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<AppStorageOptions>>().Value;

        Assert.Equal(expectedRoot, options.RootDirectory);
        Assert.True(Directory.Exists(expectedRoot));

        Directory.Delete(expectedRoot, recursive: true);
    }
}
