namespace DeskTodo.Domain.Exceptions;

/// <summary>Thrown when an operation references a bulk edit rule ID that doesn't exist.</summary>
public sealed class BulkEditRuleNotFoundException(Guid ruleId)
    : Exception($"No bulk edit rule was found with id '{ruleId}'.")
{
    public Guid RuleId { get; } = ruleId;
}
