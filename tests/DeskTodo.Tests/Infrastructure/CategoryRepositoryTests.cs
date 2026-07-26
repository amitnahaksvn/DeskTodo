using DeskTodo.Domain.Entities;
using DeskTodo.Infrastructure.Data.Configurations;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class CategoryRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly CategoryRepository _sut;

    public CategoryRepositoryTests()
    {
        _sut = new CategoryRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task GetAllAsync_ReturnsTheSevenSeededBuiltInCategories()
    {
        var categories = await _sut.GetAllAsync();

        Assert.Equal(7, categories.Count);
        Assert.All(categories, c => Assert.True(c.IsBuiltIn));
        Assert.Contains(categories, c => c.Name == "Personal");
        Assert.Contains(categories, c => c.Name == "Fitness");
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsTheCustomCategory()
    {
        var category = new Category { Name = "Side Project", ColorHex = "#000000" };

        await _sut.AddAsync(category);
        var fetched = await _sut.GetByIdAsync(category.Id);

        Assert.NotNull(fetched);
        Assert.Equal("Side Project", fetched.Name);
        Assert.False(fetched.IsBuiltIn);
    }

    [Fact]
    public async Task DeleteAsync_RemovesACustomCategory()
    {
        var category = new Category { Name = "Side Project", ColorHex = "#000000" };
        await _sut.AddAsync(category);

        await _sut.DeleteAsync(category.Id);

        Assert.Null(await _sut.GetByIdAsync(category.Id));
    }

    [Fact]
    public async Task DeleteAsync_IgnoresBuiltInCategories()
    {
        await _sut.DeleteAsync(CategoryConfiguration.PersonalId);

        Assert.NotNull(await _sut.GetByIdAsync(CategoryConfiguration.PersonalId));
    }
}
