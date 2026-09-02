namespace DeskTodo.Application.Services;

/// <summary>Routes one already-authenticated <see cref="LocalApiRequest"/> to the matching use case and builds its <see cref="LocalApiResponse"/> — Feature 97 (Roadmap-39-100.md).</summary>
public interface ILocalApiRequestHandler
{
    Task<LocalApiResponse> HandleAsync(LocalApiRequest request, CancellationToken cancellationToken = default);
}
