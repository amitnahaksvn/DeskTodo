using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Tests.Domain;

public class DistractionTests
{
    [Fact]
    public void End_SetsEndedAtAndComputesDurationInMinutes()
    {
        var start = new DateTime(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc);
        var distraction = new Distraction { StartedAt = start, Category = DistractionCategory.Website };

        distraction.End(start.AddMinutes(12));

        Assert.Equal(start.AddMinutes(12), distraction.EndedAt);
        Assert.Equal(12, distraction.DurationMinutes);
    }

    [Fact]
    public void End_RoundsToTheNearestMinute()
    {
        var start = new DateTime(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc);
        var distraction = new Distraction { StartedAt = start, Category = DistractionCategory.Website };

        distraction.End(start.AddSeconds(40));

        Assert.Equal(1, distraction.DurationMinutes);
    }

    [Fact]
    public void End_WithZeroElapsedTime_StillCountsAsAtLeastOneMinute()
    {
        var start = new DateTime(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc);
        var distraction = new Distraction { StartedAt = start, Category = DistractionCategory.Website };

        distraction.End(start);

        Assert.Equal(1, distraction.DurationMinutes);
    }
}
