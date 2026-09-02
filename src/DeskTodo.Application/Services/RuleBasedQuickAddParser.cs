using System.Globalization;
using System.Text.RegularExpressions;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IQuickAddParser"/>
/// <remarks>
/// The spec's own "Initial implementation" section calls for exactly this: deterministic
/// parsing (today/tomorrow/yesterday/weekdays, dates, times, duration, priority keywords,
/// project/tag syntax) before any AI layer. Each recognized token is stripped from the working
/// text as it's matched, so <see cref="TaskDraft.Title"/> ends up as whatever's left over.
/// </remarks>
public sealed partial class RuleBasedQuickAddParser : IQuickAddParser
{
    private static readonly string[] WeekdayNames = ["sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday"];

    public TaskDraft Parse(string input, DateOnly today)
    {
        var text = input;
        var tags = new List<string>();
        string? projectName = null;
        TaskPriority? priority = null;
        int? estimatedMinutes = null;
        DateOnly? date = null;
        TimeOnly? time = null;

        text = TagRegex().Replace(text, match =>
        {
            tags.Add(match.Groups[1].Value);
            return string.Empty;
        });

        var projectMatch = ProjectRegex().Match(text);
        if (projectMatch.Success)
        {
            projectName = projectMatch.Groups[1].Value;
            text = ProjectRegex().Replace(text, string.Empty, 1);
        }

        text = PriorityRegex().Replace(text, match =>
        {
            priority = match.Groups[1].Value.ToLowerInvariant() switch
            {
                "critical" => TaskPriority.Critical,
                "high" => TaskPriority.High,
                "medium" => TaskPriority.Medium,
                "low" => TaskPriority.Low,
                _ => priority,
            };
            return string.Empty;
        });

        var durationMatch = DurationRegex().Match(text);
        if (durationMatch.Success)
        {
            var amount = int.Parse(durationMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var unit = durationMatch.Groups[2].Value.ToLowerInvariant();
            estimatedMinutes = unit.StartsWith('h') ? amount * 60 : amount;
            text = DurationRegex().Replace(text, string.Empty, 1);
        }

        var timeMatch = TimeRegex().Match(text);
        if (timeMatch.Success)
        {
            time = ParseTime(timeMatch);
            text = TimeRegex().Replace(text, string.Empty, 1);
        }

        if (TodayRegex().IsMatch(text))
        {
            date = today;
            text = TodayRegex().Replace(text, string.Empty, 1);
        }
        else if (TomorrowRegex().IsMatch(text))
        {
            date = today.AddDays(1);
            text = TomorrowRegex().Replace(text, string.Empty, 1);
        }
        else if (YesterdayRegex().IsMatch(text))
        {
            date = today.AddDays(-1);
            text = YesterdayRegex().Replace(text, string.Empty, 1);
        }
        else
        {
            var weekdayMatch = WeekdayRegex().Match(text);
            if (weekdayMatch.Success)
            {
                date = NextWeekday(today, weekdayMatch.Groups[1].Value.ToLowerInvariant());
                text = WeekdayRegex().Replace(text, string.Empty, 1);
            }
            else
            {
                var explicitDateMatch = ExplicitDateRegex().Match(text);
                if (explicitDateMatch.Success && DateOnly.TryParse(explicitDateMatch.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    date = parsedDate;
                    text = ExplicitDateRegex().Replace(text, string.Empty, 1);
                }
            }
        }

        DateTime? dueDate = null;
        if (date is { } d)
        {
            dueDate = d.ToDateTime(time ?? TimeOnly.MinValue);
        }
        else if (time is { } t)
        {
            // A bare time with no date ("at 5pm") implies today.
            dueDate = today.ToDateTime(t);
        }

        var title = CollapseWhitespaceRegex().Replace(text, " ").Trim();

        return new TaskDraft(title, dueDate, priority, projectName, tags, estimatedMinutes);
    }

    private static TimeOnly ParseTime(Match match)
    {
        // Two alternatives: an am/pm-suffixed time ("4pm", "at 4:30pm", "9am" — "at " is
        // optional here since "tomorrow 5pm" is a real example straight from this feature's
        // own spec, with no "at"), or an explicit 24-hour "at H:MM" with no am/pm at all
        // (ambiguous without "at", so that prefix is required for this alternative).
        if (match.Groups["mer"].Success)
        {
            var hour = int.Parse(match.Groups["h1"].Value, CultureInfo.InvariantCulture);
            var minute = match.Groups["m1"].Success ? int.Parse(match.Groups["m1"].Value, CultureInfo.InvariantCulture) : 0;
            var meridiem = match.Groups["mer"].Value.ToLowerInvariant();

            if (meridiem == "pm" && hour < 12)
            {
                hour += 12;
            }
            else if (meridiem == "am" && hour == 12)
            {
                hour = 0;
            }

            return new TimeOnly(hour % 24, minute);
        }

        var hour24 = int.Parse(match.Groups["h2"].Value, CultureInfo.InvariantCulture);
        var minute24 = int.Parse(match.Groups["m2"].Value, CultureInfo.InvariantCulture);
        return new TimeOnly(hour24 % 24, minute24);
    }

    private static DateOnly NextWeekday(DateOnly today, string weekdayName)
    {
        var targetDayOfWeek = (DayOfWeek)Array.IndexOf(WeekdayNames, weekdayName);
        var daysUntil = ((int)targetDayOfWeek - (int)today.DayOfWeek + 7) % 7;
        daysUntil = daysUntil == 0 ? 7 : daysUntil; // "monday" said on a Monday means *next* Monday.
        return today.AddDays(daysUntil);
    }

    [GeneratedRegex(@"#(\w+)")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"@(\w+)")]
    private static partial Regex ProjectRegex();

    [GeneratedRegex(@"!(critical|high|medium|low)\b", RegexOptions.IgnoreCase)]
    private static partial Regex PriorityRegex();

    [GeneratedRegex(@"\bfor\s+(\d+)\s*(hours?|hrs?|h|minutes?|mins?|m)\b", RegexOptions.IgnoreCase)]
    private static partial Regex DurationRegex();

    /// <summary>
    /// Two alternatives, tried left to right: an am/pm-suffixed time (the "at" prefix is
    /// optional — "tomorrow 5pm" is this feature's own spec example, with no "at"), or an
    /// explicit 24-hour "at H:MM" with no am/pm — the "at" is required there since a bare
    /// "16:00" elsewhere in free text is too easy to mistake for something else.
    /// </summary>
    [GeneratedRegex(@"\b(?:at\s+)?(?<h1>\d{1,2})(?::(?<m1>\d{2}))?\s*(?<mer>am|pm)\b|\bat\s+(?<h2>\d{1,2}):(?<m2>\d{2})\b", RegexOptions.IgnoreCase)]
    private static partial Regex TimeRegex();

    [GeneratedRegex(@"\btoday\b", RegexOptions.IgnoreCase)]
    private static partial Regex TodayRegex();

    [GeneratedRegex(@"\btomorrow\b", RegexOptions.IgnoreCase)]
    private static partial Regex TomorrowRegex();

    [GeneratedRegex(@"\byesterday\b", RegexOptions.IgnoreCase)]
    private static partial Regex YesterdayRegex();

    [GeneratedRegex(@"\b(sunday|monday|tuesday|wednesday|thursday|friday|saturday)\b", RegexOptions.IgnoreCase)]
    private static partial Regex WeekdayRegex();

    [GeneratedRegex(@"\b\d{4}-\d{2}-\d{2}\b")]
    private static partial Regex ExplicitDateRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex CollapseWhitespaceRegex();
}
