using DeskTodo.Application.Services;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Tests.Application;

public class RuleBasedQuickAddParserTests
{
    private readonly RuleBasedQuickAddParser _sut = new();
    private static readonly DateOnly Today = new(2026, 8, 19); // a Wednesday

    [Fact]
    public void Parse_WithPlainText_ReturnsItUnchangedAsTitle_WithNoOtherFields()
    {
        var draft = _sut.Parse("Buy milk", Today);

        Assert.Equal("Buy milk", draft.Title);
        Assert.Null(draft.DueDate);
        Assert.Null(draft.Priority);
        Assert.Null(draft.ProjectName);
        Assert.Empty(draft.Tags);
        Assert.Null(draft.EstimatedMinutes);
    }

    [Fact]
    public void Parse_RecognizesToday()
    {
        var draft = _sut.Parse("Call Sam today", Today);

        Assert.Equal(Today.ToDateTime(TimeOnly.MinValue), draft.DueDate);
        Assert.Equal("Call Sam", draft.Title);
    }

    [Fact]
    public void Parse_RecognizesTomorrow()
    {
        var draft = _sut.Parse("Call Sam tomorrow", Today);

        Assert.Equal(Today.AddDays(1).ToDateTime(TimeOnly.MinValue), draft.DueDate);
    }

    [Fact]
    public void Parse_RecognizesYesterday()
    {
        var draft = _sut.Parse("Follow up yesterday", Today);

        Assert.Equal(Today.AddDays(-1).ToDateTime(TimeOnly.MinValue), draft.DueDate);
    }

    [Theory]
    [InlineData("at 4pm", 16, 0)]
    [InlineData("at 4:30pm", 16, 30)]
    [InlineData("at 9am", 9, 0)]
    [InlineData("at 16:00", 16, 0)]
    public void Parse_RecognizesTimes(string phrase, int expectedHour, int expectedMinute)
    {
        var draft = _sut.Parse($"Call Rahul {phrase}", Today);

        Assert.NotNull(draft.DueDate);
        Assert.Equal(expectedHour, draft.DueDate!.Value.Hour);
        Assert.Equal(expectedMinute, draft.DueDate!.Value.Minute);
        // A bare time with no date implies today.
        Assert.Equal(Today, DateOnly.FromDateTime(draft.DueDate!.Value));
    }

    [Fact]
    public void Parse_CombinesTomorrowAndATime()
    {
        var draft = _sut.Parse("Call Rahul tomorrow at 4pm", Today);

        Assert.Equal(Today.AddDays(1), DateOnly.FromDateTime(draft.DueDate!.Value));
        Assert.Equal(16, draft.DueDate!.Value.Hour);
        Assert.Equal("Call Rahul", draft.Title);
    }

    [Fact]
    public void Parse_RecognizesTheExampleFromTheSpec()
    {
        var draft = _sut.Parse("Prepare release notes tomorrow 5pm #release @ProjectA", Today);

        Assert.Equal("Prepare release notes", draft.Title);
        Assert.Equal(Today.AddDays(1), DateOnly.FromDateTime(draft.DueDate!.Value));
        Assert.Equal(17, draft.DueDate!.Value.Hour);
        Assert.Equal(["release"], draft.Tags);
        Assert.Equal("ProjectA", draft.ProjectName);
    }

    [Theory]
    [InlineData("monday")]
    [InlineData("friday")]
    public void Parse_RecognizesAWeekdayName_AsTheNextOccurrenceOfThatDay(string weekday)
    {
        var draft = _sut.Parse($"Team sync {weekday}", Today);

        Assert.NotNull(draft.DueDate);
        Assert.True(DateOnly.FromDateTime(draft.DueDate!.Value) > Today);
        Assert.Equal(weekday, Enum.GetName(DateOnly.FromDateTime(draft.DueDate!.Value).DayOfWeek)?.ToLowerInvariant());
    }

    [Theory]
    [InlineData("!critical", TaskPriority.Critical)]
    [InlineData("!high", TaskPriority.High)]
    [InlineData("!medium", TaskPriority.Medium)]
    [InlineData("!low", TaskPriority.Low)]
    public void Parse_RecognizesPriorityKeywords(string token, TaskPriority expected)
    {
        var draft = _sut.Parse($"Fix the bug {token}", Today);

        Assert.Equal(expected, draft.Priority);
        Assert.Equal("Fix the bug", draft.Title);
    }

    [Theory]
    [InlineData("for 30 minutes", 30)]
    [InlineData("for 30 mins", 30)]
    [InlineData("for 1 hour", 60)]
    [InlineData("for 2 hours", 120)]
    public void Parse_RecognizesDuration(string phrase, int expectedMinutes)
    {
        var draft = _sut.Parse($"Deep work {phrase}", Today);

        Assert.Equal(expectedMinutes, draft.EstimatedMinutes);
    }

    [Fact]
    public void Parse_RecognizesMultipleTags()
    {
        var draft = _sut.Parse("Ship it #urgent #backend", Today);

        Assert.Equal(["urgent", "backend"], draft.Tags);
        Assert.Equal("Ship it", draft.Title);
    }

    [Fact]
    public void Parse_RecognizesAnExplicitIsoDate()
    {
        var draft = _sut.Parse("Renew passport 2026-12-01", Today);

        Assert.Equal(new DateOnly(2026, 12, 1), DateOnly.FromDateTime(draft.DueDate!.Value));
    }
}
