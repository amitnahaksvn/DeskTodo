namespace DeskTodo.Application.Services;

/// <summary>
/// A parsed HTTP request for Feature 97's Local REST API — deliberately not tied to
/// <c>System.Net.HttpListenerRequest</c> (that's <c>DeskTodo.Infrastructure</c>'s job to parse
/// into this), so <see cref="ILocalApiRequestHandler"/>'s routing logic can be unit tested
/// without ever binding a real socket.
/// </summary>
public sealed record LocalApiRequest(string Method, string Path, IReadOnlyDictionary<string, string> QueryParameters, string? Body);

/// <summary>The result of handling one <see cref="LocalApiRequest"/> — a status code plus an already-serialized JSON body (or null for a body-less response like a 204 or 401).</summary>
public sealed record LocalApiResponse(int StatusCode, string? BodyJson);
