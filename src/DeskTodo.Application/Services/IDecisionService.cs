using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>Feature 57's Decision Log use cases.</summary>
public interface IDecisionService
{
    Task<IReadOnlyList<Decision>> GetDecisionsAsync(CancellationToken cancellationToken = default);

    Task<Decision> RecordDecisionAsync(string title, string? context, string decisionText, string? alternatives, string? reason, Guid? projectId, CancellationToken cancellationToken = default);

    Task DeleteDecisionAsync(Guid decisionId, CancellationToken cancellationToken = default);
}
