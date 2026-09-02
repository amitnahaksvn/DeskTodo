namespace DeskTodo.Application.Settings;

/// <summary>One named request saved from the API Explorer (Feature 100, Roadmap-39-100.md) — "Save request" from that feature's own spec.</summary>
public sealed class ApiExplorerSavedRequest
{
    public required string Name { get; set; }

    public required string Method { get; set; }

    public required string Path { get; set; }

    public string QueryText { get; set; } = string.Empty;

    public string HeadersText { get; set; } = string.Empty;

    public string BodyText { get; set; } = string.Empty;
}
