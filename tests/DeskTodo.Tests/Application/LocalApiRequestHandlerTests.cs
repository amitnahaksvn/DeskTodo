using System.Text.Json;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Moq;

namespace DeskTodo.Tests.Application;

public class LocalApiRequestHandlerTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly Mock<IProjectService> _projectService = new();
    private readonly LocalApiRequestHandler _sut;

    public LocalApiRequestHandlerTests()
    {
        _sut = new LocalApiRequestHandler(_taskService.Object, _projectService.Object);
    }

    private static LocalApiRequest MakeRequest(string method, string path, string? body = null, IReadOnlyDictionary<string, string>? query = null) =>
        new(method, path, query ?? new Dictionary<string, string>(), body);

    private static JsonElement ParseBody(LocalApiResponse response) => JsonSerializer.Deserialize<JsonElement>(response.BodyJson!);

    [Fact]
    public async Task HandleAsync_GetTasks_WithNoDateQuery_ReturnsAllTasks()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 9, 2), Title = "Ship it" };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        var response = await _sut.HandleAsync(MakeRequest("GET", "/api/v1/tasks"));

        Assert.Equal(200, response.StatusCode);
        var body = ParseBody(response);
        Assert.Equal(1, body.GetArrayLength());
        Assert.Equal("Ship it", body[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task HandleAsync_GetTasks_WithADateQuery_UsesGetTasksForDate()
    {
        var date = new DateOnly(2026, 9, 2);
        _taskService.Setup(s => s.GetTasksForDateAsync(date, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var response = await _sut.HandleAsync(MakeRequest("GET", "/api/v1/tasks", query: new Dictionary<string, string> { ["date"] = "2026-09-02" }));

        Assert.Equal(200, response.StatusCode);
        _taskService.Verify(s => s.GetTasksForDateAsync(date, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_GetTaskById_WhenFound_Returns200()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 9, 2), Title = "Ship it" };
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        var response = await _sut.HandleAsync(MakeRequest("GET", $"/api/v1/tasks/{task.Id}"));

        Assert.Equal(200, response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_GetTaskById_WhenMissing_Returns404()
    {
        var id = Guid.NewGuid();
        _taskService.Setup(s => s.GetTaskAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        var response = await _sut.HandleAsync(MakeRequest("GET", $"/api/v1/tasks/{id}"));

        Assert.Equal(404, response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_GetTaskById_WithAMalformedId_Returns404()
    {
        var response = await _sut.HandleAsync(MakeRequest("GET", "/api/v1/tasks/not-a-guid"));

        Assert.Equal(404, response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_PostTasks_WithAValidBody_Creates_AndReturns201()
    {
        _taskService.Setup(s => s.CreateTaskAsync(
                It.IsAny<DateOnly>(), "Ship it", null, TaskPriority.High, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskItem { PlanDate = new DateOnly(2026, 9, 2), Title = "Ship it", Priority = TaskPriority.High });

        var response = await _sut.HandleAsync(MakeRequest("POST", "/api/v1/tasks", """{"title":"Ship it","priority":"High"}"""));

        Assert.Equal(201, response.StatusCode);
        var body = ParseBody(response);
        Assert.Equal("Ship it", body.GetProperty("title").GetString());
        Assert.Equal("High", body.GetProperty("priority").GetString());
    }

    [Fact]
    public async Task HandleAsync_PostTasks_WithNoTitle_Returns400()
    {
        var response = await _sut.HandleAsync(MakeRequest("POST", "/api/v1/tasks", """{"description":"no title"}"""));

        Assert.Equal(400, response.StatusCode);
        _taskService.Verify(s => s.CreateTaskAsync(
            It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TaskPriority>(),
            It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_PostTasks_WithMalformedJson_Returns400()
    {
        var response = await _sut.HandleAsync(MakeRequest("POST", "/api/v1/tasks", "not json"));

        Assert.Equal(400, response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_PutTask_WhenFound_UpdatesOnlyProvidedFields()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 9, 2), Title = "Old title", Description = "Keep me", Priority = TaskPriority.Low };
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        var response = await _sut.HandleAsync(MakeRequest("PUT", $"/api/v1/tasks/{task.Id}", """{"title":"New title"}"""));

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("New title", task.Title);
        Assert.Equal("Keep me", task.Description);
        Assert.Equal(TaskPriority.Low, task.Priority);
        _taskService.Verify(s => s.UpdateTaskAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PutTask_WhenMissing_Returns404()
    {
        var id = Guid.NewGuid();
        _taskService.Setup(s => s.GetTaskAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        var response = await _sut.HandleAsync(MakeRequest("PUT", $"/api/v1/tasks/{id}", """{"title":"New title"}"""));

        Assert.Equal(404, response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_DeleteTask_WhenFound_Returns204_AndDeletes()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 9, 2), Title = "Ship it" };
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        var response = await _sut.HandleAsync(MakeRequest("DELETE", $"/api/v1/tasks/{task.Id}"));

        Assert.Equal(204, response.StatusCode);
        Assert.Null(response.BodyJson);
        _taskService.Verify(s => s.DeleteTaskAsync(task.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DeleteTask_WhenMissing_Returns404()
    {
        var id = Guid.NewGuid();
        _taskService.Setup(s => s.GetTaskAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        var response = await _sut.HandleAsync(MakeRequest("DELETE", $"/api/v1/tasks/{id}"));

        Assert.Equal(404, response.StatusCode);
        _taskService.Verify(s => s.DeleteTaskAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_GetProjects_ReturnsThem()
    {
        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Project { Name = "Website", ColorHex = "#6366F1" }]);

        var response = await _sut.HandleAsync(MakeRequest("GET", "/api/v1/projects"));

        Assert.Equal(200, response.StatusCode);
        var body = ParseBody(response);
        Assert.Equal("Website", body[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task HandleAsync_PostProjects_WithAValidBody_Creates_AndReturns201()
    {
        _projectService.Setup(s => s.CreateProjectAsync("Website", null, "#6366F1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Name = "Website", ColorHex = "#6366F1" });

        var response = await _sut.HandleAsync(MakeRequest("POST", "/api/v1/projects", """{"name":"Website","colorHex":"#6366F1"}"""));

        Assert.Equal(201, response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_PostProjects_WithNoName_Returns400()
    {
        var response = await _sut.HandleAsync(MakeRequest("POST", "/api/v1/projects", "{}"));

        Assert.Equal(400, response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WithAnUnknownRoute_Returns404()
    {
        var response = await _sut.HandleAsync(MakeRequest("GET", "/api/v1/goals"));

        Assert.Equal(404, response.StatusCode);
    }
}
