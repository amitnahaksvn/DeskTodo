namespace DeskTodo.App.ViewModels;

/// <summary>A read-only row shown in the Bulk Edit Rules window's saved-rule list.</summary>
public sealed record BulkEditRuleRow(Guid Id, string Name, int ConditionCount, int ActionCount, bool HasDestructiveAction);
