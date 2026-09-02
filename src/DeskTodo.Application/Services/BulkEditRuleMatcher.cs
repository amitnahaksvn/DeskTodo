using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Application.Services;

/// <summary>Pure matching logic for Feature 88's Bulk Edit Rules — kept separate from <see cref="BulkEditRuleService"/> so the condition semantics can be unit tested without any persistence.</summary>
public static class BulkEditRuleMatcher
{
    public static bool Matches(TaskItem task, IReadOnlyList<BulkEditCondition> conditions) =>
        conditions.All(condition => MatchesCondition(task, condition));

    private static bool MatchesCondition(TaskItem task, BulkEditCondition condition) => condition.Field switch
    {
        BulkEditConditionField.Project => MatchesGuid(task.ProjectId, condition),
        BulkEditConditionField.Category => MatchesGuid(task.CategoryId, condition),
        BulkEditConditionField.Priority => MatchesPriority(task.Priority, condition),
        BulkEditConditionField.DueDate => MatchesDueDate(task.DueDate, condition),
        BulkEditConditionField.IsCompleted => MatchesBool(task.IsCompleted, condition),
        BulkEditConditionField.TitleContains => task.Title.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
        _ => false,
    };

    private static bool MatchesGuid(Guid? actual, BulkEditCondition condition)
    {
        if (!Guid.TryParse(condition.Value, out var expected))
        {
            return false;
        }

        var isEqual = actual == expected;
        return condition.Operator == BulkEditConditionOperator.NotEquals ? !isEqual : isEqual;
    }

    private static bool MatchesPriority(TaskPriority actual, BulkEditCondition condition)
    {
        if (!Enum.TryParse<TaskPriority>(condition.Value, ignoreCase: true, out var expected))
        {
            return false;
        }

        return condition.Operator switch
        {
            BulkEditConditionOperator.Equals => actual == expected,
            BulkEditConditionOperator.NotEquals => actual != expected,
            BulkEditConditionOperator.LessThan => actual < expected,
            BulkEditConditionOperator.GreaterThan => actual > expected,
            _ => false,
        };
    }

    private static bool MatchesDueDate(DateTime? actual, BulkEditCondition condition)
    {
        if (actual is not { } dueDate || !TryResolveDate(condition.Value, out var expected))
        {
            return false;
        }

        return condition.Operator switch
        {
            BulkEditConditionOperator.Equals => dueDate.Date == expected.Date,
            BulkEditConditionOperator.NotEquals => dueDate.Date != expected.Date,
            BulkEditConditionOperator.LessThan => dueDate.Date < expected.Date,
            BulkEditConditionOperator.GreaterThan => dueDate.Date > expected.Date,
            _ => false,
        };
    }

    /// <summary>"Today" (case-insensitive) resolves to the current date so a saved rule like "DueDate &lt; Today" stays relative every time it runs, rather than freezing to whatever date it was created on.</summary>
    private static bool TryResolveDate(string value, out DateTime date)
    {
        if (string.Equals(value, "Today", StringComparison.OrdinalIgnoreCase))
        {
            date = DateTime.Today;
            return true;
        }

        return DateTime.TryParse(value, out date);
    }

    private static bool MatchesBool(bool actual, BulkEditCondition condition)
    {
        if (!bool.TryParse(condition.Value, out var expected))
        {
            return false;
        }

        var isEqual = actual == expected;
        return condition.Operator == BulkEditConditionOperator.NotEquals ? !isEqual : isEqual;
    }
}
