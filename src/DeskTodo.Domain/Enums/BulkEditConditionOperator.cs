namespace DeskTodo.Domain.Enums;

/// <summary>How a <see cref="Entities.BulkEditCondition"/> compares a task's field against its value. Not every operator applies to every <see cref="BulkEditConditionField"/> — see <c>BulkEditRuleMatcher</c>.</summary>
public enum BulkEditConditionOperator
{
    Equals = 0,
    NotEquals = 1,
    LessThan = 2,
    GreaterThan = 3,
}
