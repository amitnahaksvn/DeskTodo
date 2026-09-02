using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class ProjectTemplateRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly ProjectTemplateRepository _sut;

    public ProjectTemplateRepositoryTests()
    {
        _sut = new ProjectTemplateRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    private static ProjectTemplate MakeTemplate(string name) => new()
    {
        Name = name,
        Description = "A standard release process",
        TaskItems =
        [
            new ProjectTemplateTaskItem { Title = "Requirements", Priority = TaskPriority.High, DayOffsetStart = 1, DurationDays = 1 },
            new ProjectTemplateTaskItem { Title = "Development", Priority = TaskPriority.Medium, DayOffsetStart = 2, DurationDays = 6 },
        ],
        MilestoneItems =
        [
            new ProjectTemplateMilestoneItem { Title = "Code Complete", DayOffset = 7 },
        ],
    };

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsTaskAndMilestoneItems()
    {
        var template = MakeTemplate("Software Release Kit");

        await _sut.AddAsync(template);
        var loaded = await _sut.GetByIdAsync(template.Id);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.TaskItems.Count);
        Assert.Equal("Development", loaded.TaskItems[1].Title);
        Assert.Equal(6, loaded.TaskItems[1].DurationDays);
        Assert.Single(loaded.MilestoneItems);
        Assert.Equal(7, loaded.MilestoneItems[0].DayOffset);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTemplatesOrderedByName()
    {
        await _sut.AddAsync(MakeTemplate("Zeta Kit"));
        await _sut.AddAsync(MakeTemplate("Alpha Kit"));

        var all = await _sut.GetAllAsync();

        Assert.Equal(["Alpha Kit", "Zeta Kit"], all.Select(t => t.Name));
    }

    [Fact]
    public async Task UpdateAsync_ReplacesTaskItems()
    {
        var template = MakeTemplate("Release Kit");
        await _sut.AddAsync(template);

        template.TaskItems = [new ProjectTemplateTaskItem { Title = "Only Task", DayOffsetStart = 1, DurationDays = 1 }];
        await _sut.UpdateAsync(template);

        var loaded = await _sut.GetByIdAsync(template.Id);
        Assert.Single(loaded!.TaskItems);
        Assert.Equal("Only Task", loaded.TaskItems[0].Title);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheTemplate()
    {
        var template = MakeTemplate("Release Kit");
        await _sut.AddAsync(template);

        await _sut.DeleteAsync(template.Id);

        Assert.Null(await _sut.GetByIdAsync(template.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenTemplateMissing_DoesNotThrow()
    {
        await _sut.DeleteAsync(Guid.NewGuid());
    }
}
