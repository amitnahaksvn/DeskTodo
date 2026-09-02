using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;

namespace DeskTodo.Tests.Application;

public class DuplicateDetectionServiceTests
{
    private readonly DuplicateDetectionService _sut = new();

    [Fact]
    public void FindPossibleDuplicates_WithAnExactNormalizedTitleMatch_ScoresOne()
    {
        var existing = new TaskItem { PlanDate = new DateOnly(2026, 8, 25), Title = "Deploy Production API" };

        var results = _sut.FindPossibleDuplicates("deploy production api", new DateOnly(2026, 8, 25), null, [existing]);

        var match = Assert.Single(results);
        Assert.Equal(1.0, match.SimilarityScore);
    }

    [Fact]
    public void FindPossibleDuplicates_WithAnUnrelatedTitle_ReturnsNothing()
    {
        var existing = new TaskItem { PlanDate = new DateOnly(2026, 8, 25), Title = "Buy groceries" };

        var results = _sut.FindPossibleDuplicates("Deploy production API", new DateOnly(2026, 8, 25), null, [existing]);

        Assert.Empty(results);
    }

    [Fact]
    public void FindPossibleDuplicates_WithASimilarButNotIdenticalTitle_ReportsAPartialScore()
    {
        var existing = new TaskItem { PlanDate = new DateOnly(2026, 8, 25), Title = "Deploy production API to staging" };

        var results = _sut.FindPossibleDuplicates("Deploy production API", new DateOnly(2026, 8, 25), null, [existing]);

        var match = Assert.Single(results);
        Assert.True(match.SimilarityScore is > 0 and < 1.0);
    }

    [Fact]
    public void FindPossibleDuplicates_SameDayAndCategory_BoostsAnAlreadySimilarScore()
    {
        var categoryId = Guid.NewGuid();
        var planDate = new DateOnly(2026, 8, 25);
        var sameContext = new TaskItem { PlanDate = planDate, CategoryId = categoryId, Title = "Deploy production API to staging" };
        var differentContext = new TaskItem { PlanDate = planDate.AddDays(1), CategoryId = null, Title = "Deploy production API to staging" };

        var boosted = _sut.FindPossibleDuplicates("Deploy production API", planDate, categoryId, [sameContext]).Single();
        var notBoosted = _sut.FindPossibleDuplicates("Deploy production API", planDate, categoryId, [differentContext]).Single();

        Assert.True(boosted.SimilarityScore > notBoosted.SimilarityScore);
    }

    [Fact]
    public void FindPossibleDuplicates_WithABlankTitle_ReturnsNothing()
    {
        var existing = new TaskItem { PlanDate = new DateOnly(2026, 8, 25), Title = "Task" };

        var results = _sut.FindPossibleDuplicates("   ", new DateOnly(2026, 8, 25), null, [existing]);

        Assert.Empty(results);
    }

    [Fact]
    public void FindPossibleDuplicates_IgnoresPunctuationAndCaseWhenComparing()
    {
        var existing = new TaskItem { PlanDate = new DateOnly(2026, 8, 25), Title = "Deploy Production API!!" };

        var results = _sut.FindPossibleDuplicates("deploy production api", new DateOnly(2026, 8, 25), null, [existing]);

        var match = Assert.Single(results);
        Assert.Equal(1.0, match.SimilarityScore);
    }

    [Fact]
    public void FindPossibleDuplicates_ResultsAreOrderedByScoreDescending()
    {
        var planDate = new DateOnly(2026, 8, 25);
        var exact = new TaskItem { PlanDate = planDate, Title = "Deploy production API" };
        var partial = new TaskItem { PlanDate = planDate, Title = "Deploy production API to staging environment" };

        var results = _sut.FindPossibleDuplicates("Deploy production API", planDate, null, [partial, exact]);

        Assert.Equal(exact.Id, results[0].Task.Id);
    }
}
