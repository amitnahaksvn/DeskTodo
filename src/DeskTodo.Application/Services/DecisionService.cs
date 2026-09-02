using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IDecisionService"/>
public sealed class DecisionService(IDecisionRepository decisionRepository) : IDecisionService
{
    public Task<IReadOnlyList<Decision>> GetDecisionsAsync(CancellationToken cancellationToken = default) =>
        decisionRepository.GetAllAsync(cancellationToken);

    public async Task<Decision> RecordDecisionAsync(string title, string? context, string decisionText, string? alternatives, string? reason, Guid? projectId, CancellationToken cancellationToken = default)
    {
        var decision = new Decision
        {
            Title = title.Trim(),
            Context = string.IsNullOrWhiteSpace(context) ? null : context.Trim(),
            DecisionText = decisionText.Trim(),
            Alternatives = string.IsNullOrWhiteSpace(alternatives) ? null : alternatives.Trim(),
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            ProjectId = projectId,
        };
        await decisionRepository.AddAsync(decision, cancellationToken);
        return decision;
    }

    public Task DeleteDecisionAsync(Guid decisionId, CancellationToken cancellationToken = default) =>
        decisionRepository.DeleteAsync(decisionId, cancellationToken);
}
