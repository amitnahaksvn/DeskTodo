using DeskTodo.Domain.Entities;

namespace DeskTodo.Tests.Domain;

public class GoalTests
{
    private static readonly DateOnly Today = new(2026, 8, 15);

    private static Goal CreateGoal(params DateOnly[] completedDates)
    {
        var goal = new Goal { Name = "Meditate" };
        foreach (var date in completedDates)
        {
            goal.Completions.Add(new GoalCompletion { GoalId = goal.Id, CompletedDate = date });
        }

        return goal;
    }

    [Fact]
    public void GetCurrentStreak_WithNoCompletions_IsZero()
    {
        var goal = CreateGoal();

        Assert.Equal(0, goal.GetCurrentStreak(Today));
    }

    [Fact]
    public void GetCurrentStreak_CompletedToday_CountsToday()
    {
        var goal = CreateGoal(Today);

        Assert.Equal(1, goal.GetCurrentStreak(Today));
    }

    [Fact]
    public void GetCurrentStreak_NotCompletedTodayButCompletedYesterday_StillCounts()
    {
        // Not yet marked done today, but yesterday's streak isn't broken until midnight
        // passes without a completion — the same "give it until end of day" leniency a
        // real habit streak needs.
        var goal = CreateGoal(Today.AddDays(-1));

        Assert.Equal(1, goal.GetCurrentStreak(Today));
    }

    [Fact]
    public void GetCurrentStreak_MissingYesterdayAndToday_IsZero()
    {
        var goal = CreateGoal(Today.AddDays(-2));

        Assert.Equal(0, goal.GetCurrentStreak(Today));
    }

    [Fact]
    public void GetCurrentStreak_CountsConsecutiveDaysOnly()
    {
        // Today, yesterday, day before — three in a row — then a gap, then one more
        // isolated day further back. The gap should stop the count at 3, not continue
        // through to the isolated day.
        var goal = CreateGoal(
            Today,
            Today.AddDays(-1),
            Today.AddDays(-2),
            Today.AddDays(-4),
            Today.AddDays(-5));

        Assert.Equal(3, goal.GetCurrentStreak(Today));
    }

    [Fact]
    public void GetCurrentStreak_OrderOfCompletionsDoesNotMatter()
    {
        var goal = CreateGoal(Today.AddDays(-2), Today, Today.AddDays(-1));

        Assert.Equal(3, goal.GetCurrentStreak(Today));
    }

    [Fact]
    public void GetCurrentStreak_FutureCompletionsDoNotExtendTheStreak()
    {
        // Shouldn't happen in practice (the app only ever logs today's date), but the
        // computation should still behave sanely if it did.
        var goal = CreateGoal(Today, Today.AddDays(1));

        Assert.Equal(1, goal.GetCurrentStreak(Today));
    }
}
