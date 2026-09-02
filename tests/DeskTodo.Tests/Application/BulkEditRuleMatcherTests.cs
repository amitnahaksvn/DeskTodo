using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Tests.Application;

public class BulkEditRuleMatcherTests
{
    private static TaskItem MakeTask(TaskPriority priority = TaskPriority.Medium, Guid? projectId = null, Guid? categoryId = null, DateTime? dueDate = null, string title = "Task") =>
        new() { PlanDate = DateOnly.FromDateTime(DateTime.Today), Title = title, Priority = priority, ProjectId = projectId, CategoryId = categoryId, DueDate = dueDate };

    [Fact]
    public void Matches_WithNoConditions_AlwaysMatches()
    {
        Assert.True(BulkEditRuleMatcher.Matches(MakeTask(), []));
    }

    [Fact]
    public void Matches_RequiresEveryConditionToMatch()
    {
        var task = MakeTask(priority: TaskPriority.High, title: "Ship the release");
        var conditions = new[]
        {
            new BulkEditCondition { Field = BulkEditConditionField.Priority, Operator = BulkEditConditionOperator.Equals, Value = "High" },
            new BulkEditCondition { Field = BulkEditConditionField.TitleContains, Value = "release" },
        };

        Assert.True(BulkEditRuleMatcher.Matches(task, conditions));

        var mismatched = conditions.Append(new BulkEditCondition { Field = BulkEditConditionField.IsCompleted, Value = "true" }).ToList();
        Assert.False(BulkEditRuleMatcher.Matches(task, mismatched));
    }

    [Fact]
    public void Matches_ProjectEquals_ComparesProjectId()
    {
        var projectId = Guid.NewGuid();
        var task = MakeTask(projectId: projectId);
        var condition = new BulkEditCondition { Field = BulkEditConditionField.Project, Operator = BulkEditConditionOperator.Equals, Value = projectId.ToString() };

        Assert.True(BulkEditRuleMatcher.Matches(task, [condition]));

        var otherCondition = condition with { Value = Guid.NewGuid().ToString() };
        Assert.False(BulkEditRuleMatcher.Matches(task, [otherCondition]));
    }

    [Fact]
    public void Matches_ProjectNotEquals_InvertsTheComparison()
    {
        var task = MakeTask(projectId: Guid.NewGuid());
        var condition = new BulkEditCondition { Field = BulkEditConditionField.Project, Operator = BulkEditConditionOperator.NotEquals, Value = Guid.NewGuid().ToString() };

        Assert.True(BulkEditRuleMatcher.Matches(task, [condition]));
    }

    [Theory]
    [InlineData(BulkEditConditionOperator.LessThan, TaskPriority.Medium, TaskPriority.High, true)]
    [InlineData(BulkEditConditionOperator.GreaterThan, TaskPriority.High, TaskPriority.Medium, true)]
    [InlineData(BulkEditConditionOperator.Equals, TaskPriority.High, TaskPriority.High, true)]
    [InlineData(BulkEditConditionOperator.Equals, TaskPriority.High, TaskPriority.Low, false)]
    public void Matches_PriorityComparisons(BulkEditConditionOperator op, TaskPriority actual, TaskPriority conditionValue, bool expected)
    {
        var task = MakeTask(priority: actual);
        var condition = new BulkEditCondition { Field = BulkEditConditionField.Priority, Operator = op, Value = conditionValue.ToString() };

        Assert.Equal(expected, BulkEditRuleMatcher.Matches(task, [condition]));
    }

    [Fact]
    public void Matches_DueDateLessThanToday_UsesTheCurrentDate()
    {
        var overdue = MakeTask(dueDate: DateTime.Today.AddDays(-3));
        var future = MakeTask(dueDate: DateTime.Today.AddDays(3));
        var condition = new BulkEditCondition { Field = BulkEditConditionField.DueDate, Operator = BulkEditConditionOperator.LessThan, Value = "Today" };

        Assert.True(BulkEditRuleMatcher.Matches(overdue, [condition]));
        Assert.False(BulkEditRuleMatcher.Matches(future, [condition]));
    }

    [Fact]
    public void Matches_DueDateCondition_WhenTaskHasNoDueDate_NeverMatches()
    {
        var task = MakeTask(dueDate: null);
        var condition = new BulkEditCondition { Field = BulkEditConditionField.DueDate, Operator = BulkEditConditionOperator.NotEquals, Value = "Today" };

        Assert.False(BulkEditRuleMatcher.Matches(task, [condition]));
    }

    [Fact]
    public void Matches_TitleContains_IsCaseInsensitive()
    {
        var task = MakeTask(title: "Prepare Quarterly Report");
        var condition = new BulkEditCondition { Field = BulkEditConditionField.TitleContains, Value = "quarterly" };

        Assert.True(BulkEditRuleMatcher.Matches(task, [condition]));
    }

    [Fact]
    public void Matches_WithAnUnparsablePriorityValue_DoesNotMatch()
    {
        var task = MakeTask(priority: TaskPriority.High);
        var condition = new BulkEditCondition { Field = BulkEditConditionField.Priority, Value = "NotAPriority" };

        Assert.False(BulkEditRuleMatcher.Matches(task, [condition]));
    }
}
