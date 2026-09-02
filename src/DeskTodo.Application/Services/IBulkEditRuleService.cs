using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>Roadmap-39-100.md Feature 88 — rule/condition-based multi-task operations, distinct from Phase 28's manual-selection Batch Actions.</summary>
public interface IBulkEditRuleService
{
    Task<IReadOnlyList<BulkEditRule>> GetRulesAsync(CancellationToken cancellationToken = default);

    Task<BulkEditRule> CreateRuleAsync(
        string name,
        IReadOnlyList<BulkEditCondition> conditions,
        IReadOnlyList<BulkEditAction> actions,
        CancellationToken cancellationToken = default);

    Task DeleteRuleAsync(Guid ruleId, CancellationToken cancellationToken = default);

    /// <summary>Every non-deleted task that matches <paramref name="conditions"/>, without applying any action — backs the "37 tasks will be modified" preview this feature's spec requires before Apply.</summary>
    Task<IReadOnlyList<TaskItem>> PreviewAsync(IReadOnlyList<BulkEditCondition> conditions, CancellationToken cancellationToken = default);

    /// <summary>Applies <paramref name="actions"/> to every task matching <paramref name="conditions"/>. Returns how many tasks were affected.</summary>
    Task<int> ApplyAsync(IReadOnlyList<BulkEditCondition> conditions, IReadOnlyList<BulkEditAction> actions, CancellationToken cancellationToken = default);

    /// <summary>Loads a saved rule and applies it — see <see cref="ApplyAsync"/>.</summary>
    Task<int> ApplyRuleAsync(Guid ruleId, CancellationToken cancellationToken = default);
}
