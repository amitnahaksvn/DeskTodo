using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class RecurringProjectScheduleRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly RecurringProjectScheduleRepository _sut;
    private readonly ProjectTemplateRepository _templateRepository;

    public RecurringProjectScheduleRepositoryTests()
    {
        _sut = new RecurringProjectScheduleRepository(_fixture.ContextFactory);
        _templateRepository = new ProjectTemplateRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    private async Task<ProjectTemplate> SeedTemplateAsync()
    {
        var template = new ProjectTemplate { Name = "Monthly Reporting Kit" };
        await _templateRepository.AddAsync(template);
        return template;
    }

    private static RecurringProjectSchedule MakeSchedule(Guid templateId, DateOnly nextOccurrence, bool isActive = true) => new()
    {
        Name = "Monthly Reporting",
        ProjectTemplateId = templateId,
        ColorHex = "#4A90D9",
        Frequency = ProjectRecurrenceFrequency.Monthly,
        StartDate = nextOccurrence,
        NextOccurrenceDate = nextOccurrence,
        IsActive = isActive,
        GeneratedProjectIds = [Guid.NewGuid()],
    };

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsGeneratedProjectIds()
    {
        var template = await SeedTemplateAsync();
        var schedule = MakeSchedule(template.Id, DateOnly.FromDateTime(DateTime.Today));

        await _sut.AddAsync(schedule);
        var loaded = await _sut.GetByIdAsync(schedule.Id);

        Assert.NotNull(loaded);
        Assert.Equal(schedule.GeneratedProjectIds, loaded!.GeneratedProjectIds);
    }

    [Fact]
    public async Task GetDueAsync_OnlyReturnsActiveSchedulesDueOnOrBeforeTheGivenDate()
    {
        var template = await SeedTemplateAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var due = MakeSchedule(template.Id, today.AddDays(-1));
        var notYetDue = MakeSchedule(template.Id, today.AddDays(10));
        var inactive = MakeSchedule(template.Id, today.AddDays(-1), isActive: false);

        await _sut.AddAsync(due);
        await _sut.AddAsync(notYetDue);
        await _sut.AddAsync(inactive);

        var results = await _sut.GetDueAsync(today);

        var resultId = Assert.Single(results).Id;
        Assert.Equal(due.Id, resultId);
    }

    [Fact]
    public async Task UpdateAsync_PersistsAdvancedNextOccurrenceDate()
    {
        var template = await SeedTemplateAsync();
        var schedule = MakeSchedule(template.Id, DateOnly.FromDateTime(DateTime.Today));
        await _sut.AddAsync(schedule);

        schedule.NextOccurrenceDate = schedule.NextOccurrenceDate.AddMonths(1);
        await _sut.UpdateAsync(schedule);

        var loaded = await _sut.GetByIdAsync(schedule.Id);
        Assert.Equal(schedule.NextOccurrenceDate, loaded!.NextOccurrenceDate);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheSchedule()
    {
        var template = await SeedTemplateAsync();
        var schedule = MakeSchedule(template.Id, DateOnly.FromDateTime(DateTime.Today));
        await _sut.AddAsync(schedule);

        await _sut.DeleteAsync(schedule.Id);

        Assert.Null(await _sut.GetByIdAsync(schedule.Id));
    }
}
