using DeskTodo.Application.Services;

namespace DeskTodo.Tests.Application;

public class RuleBasedMeetingActionExtractorTests
{
    private static readonly DateOnly Today = new(2026, 9, 2); // A Wednesday.

    private readonly RuleBasedMeetingActionExtractor _sut = new(new RuleBasedQuickAddParser());

    [Fact]
    public void Extract_WithAWillClauseAndAWeekdayDeadline_ProducesOwnerTitleAndDueDate()
    {
        var candidates = _sut.Extract("John will review the API by Friday.", Today);

        var candidate = Assert.Single(candidates);
        Assert.Equal("John", candidate.Owner);
        Assert.Equal("Review the API", candidate.Title);
        Assert.Equal("Friday", candidate.DeadlineText);
        Assert.Equal(new DateOnly(2026, 9, 4).ToDateTime(TimeOnly.MinValue), candidate.DueDate);
    }

    [Fact]
    public void Extract_WithANeedsToClauseAndNoDeadline_ProducesACandidateWithNoDueDate()
    {
        var candidates = _sut.Extract("Amit needs to prepare the release notes.", Today);

        var candidate = Assert.Single(candidates);
        Assert.Equal("Amit", candidate.Owner);
        Assert.Equal("Prepare the release notes", candidate.Title);
        Assert.Null(candidate.DeadlineText);
        Assert.Null(candidate.DueDate);
    }

    [Fact]
    public void Extract_WithAnUnresolvableDeadlinePhrase_StillCapturesTheRawPhraseText()
    {
        var candidates = _sut.Extract("Sarah will arrange testing next week.", Today);

        var candidate = Assert.Single(candidates);
        Assert.Equal("Sarah", candidate.Owner);
        Assert.Equal("Arrange testing", candidate.Title);
        Assert.Equal("next week", candidate.DeadlineText);
        Assert.Null(candidate.DueDate);
    }

    [Fact]
    public void Extract_WithMultipleLines_ProducesOneCandidatePerRecognizedLine()
    {
        var notes = """
            John will review the API by Friday.
            Amit needs to prepare the release notes.
            Sarah will arrange testing next week.
            """;

        var candidates = _sut.Extract(notes, Today);

        Assert.Equal(3, candidates.Count);
        Assert.Equal(["John", "Amit", "Sarah"], candidates.Select(c => c.Owner));
    }

    [Fact]
    public void Extract_IgnoresLinesWithNoRecognizedOwnerClause()
    {
        var notes = """
            Discussed the release timeline.
            John will review the API by Friday.
            Everyone agreed to proceed.
            """;

        var candidates = _sut.Extract(notes, Today);

        Assert.Single(candidates);
    }

    [Fact]
    public void Extract_WithAnExplicitDate_ParsesItAsTheDueDate()
    {
        var candidates = _sut.Extract("Priya should finish the deployment by 2026-09-10.", Today);

        var candidate = Assert.Single(candidates);
        Assert.Equal(new DateOnly(2026, 9, 10).ToDateTime(TimeOnly.MinValue), candidate.DueDate);
    }

    [Fact]
    public void Extract_WithBlankNotes_ReturnsNoCandidates()
    {
        Assert.Empty(_sut.Extract(string.Empty, Today));
        Assert.Empty(_sut.Extract("   ", Today));
    }

    [Fact]
    public void Extract_WithHasToClause_IsRecognized()
    {
        var candidates = _sut.Extract("Maria has to update the dashboard.", Today);

        var candidate = Assert.Single(candidates);
        Assert.Equal("Maria", candidate.Owner);
        Assert.Equal("Update the dashboard", candidate.Title);
    }
}
