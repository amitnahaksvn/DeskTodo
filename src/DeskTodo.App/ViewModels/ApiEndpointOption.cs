namespace DeskTodo.App.ViewModels;

/// <summary>One entry in the API Explorer's endpoint list (Feature 100, Roadmap-39-100.md) — the Local REST API's (Feature 97) actual implemented routes.</summary>
public sealed record ApiEndpointOption(string Method, string PathTemplate, string Description)
{
    public string DisplayText => $"{Method} {PathTemplate}";

    public static readonly IReadOnlyList<ApiEndpointOption> All =
    [
        new("GET", "/api/v1/tasks", "List tasks (optional ?date=yyyy-MM-dd)"),
        new("GET", "/api/v1/tasks/{id}", "Get one task"),
        new("POST", "/api/v1/tasks", "Create a task"),
        new("PUT", "/api/v1/tasks/{id}", "Update a task (only provided fields change)"),
        new("DELETE", "/api/v1/tasks/{id}", "Soft-delete a task"),
        new("POST", "/api/v1/tasks/{id}/complete", "Mark a task complete"),
        new("GET", "/api/v1/projects", "List projects"),
        new("POST", "/api/v1/projects", "Create a project"),
    ];
}
