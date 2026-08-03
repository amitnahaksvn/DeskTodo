namespace DeskTodo.Tests.ViewModels;

/// <summary>Shared across this folder's Phase 21 planner-view tests (Calendar/Week/Year/Matrix) — mirrors <c>WidgetViewModelTests</c>'s private one, pulled out since multiple test classes here need it. Fixes <see cref="LocalTimeZone"/> to UTC so a test's chosen date/time literal can't land on a different calendar date depending on the machine's local timezone.</summary>
internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
