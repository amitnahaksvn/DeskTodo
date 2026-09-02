using System.Text.Json;
using System.Text.RegularExpressions;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="ILocalApiRequestHandler"/>
/// <remarks>
/// Scoped to Tasks and Projects — the two resources this feature's own "Example endpoints" list
/// leads with. The "Additional APIs" (/search, /views, /goals, /milestones, /tags, /events) are
/// deliberately not built this pass; see Roadmap-39-100.md's Feature 97 entry.
/// </remarks>
public sealed partial class LocalApiRequestHandler(ITaskService taskService, IProjectService projectService) : ILocalApiRequestHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<LocalApiResponse> HandleAsync(LocalApiRequest request, CancellationToken cancellationToken = default)
    {
        var method = request.Method.ToUpperInvariant();

        if (request.Path == "/api/v1/tasks" && method == "GET")
        {
            return await GetTasksAsync(request, cancellationToken);
        }

        if (request.Path == "/api/v1/tasks" && method == "POST")
        {
            return await CreateTaskAsync(request, cancellationToken);
        }

        var completeMatch = TaskCompleteIdRegex().Match(request.Path);
        if (completeMatch.Success && method == "POST" && Guid.TryParse(completeMatch.Groups["id"].Value, out var completeId))
        {
            return await CompleteTaskAsync(completeId, cancellationToken);
        }

        var taskIdMatch = TaskIdRegex().Match(request.Path);
        if (taskIdMatch.Success && Guid.TryParse(taskIdMatch.Groups["id"].Value, out var taskId))
        {
            return method switch
            {
                "GET" => await GetTaskAsync(taskId, cancellationToken),
                "PUT" => await UpdateTaskAsync(taskId, request, cancellationToken),
                "DELETE" => await DeleteTaskAsync(taskId, cancellationToken),
                _ => NotFound(),
            };
        }

        if (request.Path == "/api/v1/projects" && method == "GET")
        {
            var projects = await projectService.GetProjectsAsync(cancellationToken);
            return Json(200, projects.Select(ToDto).ToList());
        }

        if (request.Path == "/api/v1/projects" && method == "POST")
        {
            return await CreateProjectAsync(request, cancellationToken);
        }

        return NotFound();
    }

    private async Task<LocalApiResponse> GetTasksAsync(LocalApiRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<TaskItem> tasks;
        if (request.QueryParameters.TryGetValue("date", out var dateText) && DateOnly.TryParse(dateText, out var date))
        {
            tasks = await taskService.GetTasksForDateAsync(date, cancellationToken);
        }
        else
        {
            tasks = await taskService.GetAllTasksAsync(cancellationToken);
        }

        return Json(200, tasks.Select(ToDto).ToList());
    }

    private async Task<LocalApiResponse> GetTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await taskService.GetTaskAsync(taskId, cancellationToken);
        return task is null ? NotFound() : Json(200, ToDto(task));
    }

    private async Task<LocalApiResponse> CreateTaskAsync(LocalApiRequest request, CancellationToken cancellationToken)
    {
        var dto = Deserialize<TaskCreateRequestDto>(request.Body);
        if (dto is null || string.IsNullOrWhiteSpace(dto.Title))
        {
            return Json(400, new ErrorResponseDto("A non-empty 'title' is required."));
        }

        var priority = ParsePriority(dto.Priority);
        var planDate = dto.PlanDate ?? DateOnly.FromDateTime(DateTime.Now);
        var task = await taskService.CreateTaskAsync(planDate, dto.Title.Trim(), dto.Description, priority, dto.CategoryId, dto.DueDate, cancellationToken: cancellationToken);
        return Json(201, ToDto(task));
    }

    private async Task<LocalApiResponse> UpdateTaskAsync(Guid taskId, LocalApiRequest request, CancellationToken cancellationToken)
    {
        var task = await taskService.GetTaskAsync(taskId, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        var dto = Deserialize<TaskUpdateRequestDto>(request.Body);
        if (dto is null)
        {
            return Json(400, new ErrorResponseDto("A JSON body is required."));
        }

        if (!string.IsNullOrWhiteSpace(dto.Title))
        {
            task.Title = dto.Title.Trim();
        }

        if (dto.Description is not null)
        {
            task.Description = dto.Description;
        }

        if (dto.Priority is not null)
        {
            task.Priority = ParsePriority(dto.Priority);
        }

        if (dto.DueDate is not null)
        {
            task.DueDate = dto.DueDate;
        }

        if (dto.CategoryId is not null)
        {
            task.CategoryId = dto.CategoryId;
        }

        await taskService.UpdateTaskAsync(task, cancellationToken);
        return Json(200, ToDto(task));
    }

    /// <summary>Backs the CLI's <c>task complete &lt;id&gt;</c> (Feature 99) — not in this feature's own "Example endpoints" list, but a plain PUT can't express "mark done" without the client re-sending every other field, and the CLI's own spec example needs exactly this action.</summary>
    private async Task<LocalApiResponse> CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await taskService.GetTaskAsync(taskId, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        await taskService.CompleteTaskAsync(taskId, cancellationToken);
        var updated = await taskService.GetTaskAsync(taskId, cancellationToken);
        return Json(200, ToDto(updated!));
    }

    private async Task<LocalApiResponse> DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await taskService.GetTaskAsync(taskId, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        await taskService.DeleteTaskAsync(taskId, cancellationToken);
        return new LocalApiResponse(204, null);
    }

    private async Task<LocalApiResponse> CreateProjectAsync(LocalApiRequest request, CancellationToken cancellationToken)
    {
        var dto = Deserialize<ProjectCreateRequestDto>(request.Body);
        if (dto is null || string.IsNullOrWhiteSpace(dto.Name))
        {
            return Json(400, new ErrorResponseDto("A non-empty 'name' is required."));
        }

        var project = await projectService.CreateProjectAsync(dto.Name.Trim(), dto.Description, dto.ColorHex ?? "#6366F1", cancellationToken);
        return Json(201, ToDto(project));
    }

    private static TaskPriority ParsePriority(string? priority) =>
        Enum.TryParse<TaskPriority>(priority, ignoreCase: true, out var parsed) ? parsed : TaskPriority.Medium;

    private static TaskResponseDto ToDto(TaskItem task) => new(
        task.Id, task.Title, task.Description, task.Priority.ToString(), task.PlanDate, task.DueDate, task.IsCompleted, task.CategoryId, task.ProjectId);

    private static ProjectResponseDto ToDto(Project project) => new(project.Id, project.Name, project.Description, project.ColorHex);

    private static LocalApiResponse NotFound() => Json(404, new ErrorResponseDto("Not found"));

    private static LocalApiResponse Json(int statusCode, object body) => new(statusCode, JsonSerializer.Serialize(body, JsonOptions));

    private static T? Deserialize<T>(string? body) where T : class
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [GeneratedRegex(@"^/api/v1/tasks/(?<id>[0-9a-fA-F-]{36})$")]
    private static partial Regex TaskIdRegex();

    [GeneratedRegex(@"^/api/v1/tasks/(?<id>[0-9a-fA-F-]{36})/complete$")]
    private static partial Regex TaskCompleteIdRegex();

    private sealed record TaskResponseDto(Guid Id, string Title, string? Description, string Priority, DateOnly PlanDate, DateTime? DueDate, bool IsCompleted, Guid? CategoryId, Guid? ProjectId);

    private sealed record ProjectResponseDto(Guid Id, string Name, string? Description, string ColorHex);

    private sealed record TaskCreateRequestDto(string Title, string? Description, string? Priority, DateOnly? PlanDate, DateTime? DueDate, Guid? CategoryId);

    private sealed record TaskUpdateRequestDto(string? Title, string? Description, string? Priority, DateTime? DueDate, Guid? CategoryId);

    private sealed record ProjectCreateRequestDto(string Name, string? Description, string? ColorHex);

    private sealed record ErrorResponseDto(string Error);
}
