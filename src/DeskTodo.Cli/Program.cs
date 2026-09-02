using System.Text.Json;
using DeskTodo.Cli;

var (options, positional) = ParseArgs(args);

if (positional.Count == 0 || options.ContainsKey("help") || positional[0] is "help" or "-h" or "--help")
{
    PrintUsage();
    return 0;
}

var host = options.GetValueOrDefault("host", "http://127.0.0.1");
var (discoveredPort, discoveredToken) = LocalSettingsLocator.TryReadApiSettings();

var portText = options.GetValueOrDefault("port") ?? discoveredPort?.ToString();
var token = options.GetValueOrDefault("token") ?? discoveredToken;

if (!int.TryParse(portText, out var port))
{
    Console.Error.WriteLine("Couldn't determine the Local REST API's port. Pass --port, or enable it in DeskTodo's Settings → Local REST API.");
    return 1;
}

if (string.IsNullOrEmpty(token))
{
    Console.Error.WriteLine("Couldn't determine the Local REST API's token. Pass --token, or enable it in DeskTodo's Settings → Local REST API.");
    return 1;
}

using var httpClient = ApiClient.CreateHttpClient(host, port, token);
var client = new ApiClient(httpClient);

var resource = positional[0];
var action = positional.Count > 1 ? positional[1] : string.Empty;
var rest = positional.Skip(2).ToList();

return (resource, action) switch
{
    ("task", "add") => await TaskAddAsync(client, rest, options),
    ("task", "list") => await TaskListAsync(client, options),
    ("task", "complete") => await TaskCompleteAsync(client, rest),
    ("task", "search") => await TaskSearchAsync(client, rest),
    ("project", "list") => await ProjectListAsync(client),
    ("project", "show") => await ProjectShowAsync(client, rest),
    _ => Unknown(resource, action),
};

static (Dictionary<string, string> Options, List<string> Positional) ParseArgs(string[] args)
{
    var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var positional = new List<string>();

    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (arg.StartsWith("--", StringComparison.Ordinal))
        {
            var key = arg[2..];
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                options[key] = args[++i];
            }
            else
            {
                options[key] = "true";
            }
        }
        else
        {
            positional.Add(arg);
        }
    }

    return (options, positional);
}

static void PrintUsage()
{
    Console.WriteLine("""
        desktodo — a terminal client for DeskTodo's Local REST API (Feature 97).

        Usage:
          desktodo task add "<title>" [--priority Low|Medium|High|Critical] [--due <date>] [--project <id>]
          desktodo task list [--date <yyyy-MM-dd>] [--overdue]
          desktodo task complete <id>
          desktodo task search "<query>"
          desktodo project list
          desktodo project show <id>

        Global options:
          --host <url>    Default: http://127.0.0.1
          --port <n>      Default: auto-discovered from DeskTodo's settings.json
          --token <token> Default: auto-discovered from DeskTodo's settings.json

        Requires DeskTodo to be running with Settings → Local REST API enabled.
        """);
}

static int Unknown(string resource, string action)
{
    Console.Error.WriteLine($"Unknown command: {resource} {action}".TrimEnd());
    PrintUsage();
    return 1;
}

static async Task<int> TaskAddAsync(ApiClient client, List<string> rest, Dictionary<string, string> options)
{
    if (rest.Count == 0)
    {
        Console.Error.WriteLine("Usage: desktodo task add \"<title>\"");
        return 1;
    }

    var body = new Dictionary<string, object?>
    {
        ["title"] = rest[0],
        ["priority"] = options.GetValueOrDefault("priority"),
        ["dueDate"] = options.GetValueOrDefault("due"),
        ["categoryId"] = options.GetValueOrDefault("project"),
    };

    var (success, _, result, error) = await client.SendAsync(HttpMethod.Post, "api/v1/tasks", body);
    if (!success)
    {
        Console.Error.WriteLine($"Failed to add task: {error}");
        return 1;
    }

    PrintTask(result!.Value);
    return 0;
}

static async Task<int> TaskListAsync(ApiClient client, Dictionary<string, string> options)
{
    var path = "api/v1/tasks";
    if (options.TryGetValue("date", out var date))
    {
        path += $"?date={Uri.EscapeDataString(date)}";
    }

    var (success, _, result, error) = await client.SendAsync(HttpMethod.Get, path);
    if (!success)
    {
        Console.Error.WriteLine($"Failed to list tasks: {error}");
        return 1;
    }

    IEnumerable<JsonElement> tasks = result!.Value.EnumerateArray();
    if (options.ContainsKey("overdue"))
    {
        var now = DateTime.UtcNow;
        tasks = tasks.Where(t =>
            !t.GetProperty("isCompleted").GetBoolean() &&
            t.TryGetProperty("dueDate", out var due) && due.ValueKind == JsonValueKind.String &&
            DateTime.TryParse(due.GetString(), out var parsedDue) && parsedDue < now);
    }

    var any = false;
    foreach (var task in tasks)
    {
        PrintTask(task);
        any = true;
    }

    if (!any)
    {
        Console.WriteLine("No tasks.");
    }

    return 0;
}

static async Task<int> TaskCompleteAsync(ApiClient client, List<string> rest)
{
    if (rest.Count == 0 || !Guid.TryParse(rest[0], out _))
    {
        Console.Error.WriteLine("Usage: desktodo task complete <id>");
        return 1;
    }

    var (success, _, result, error) = await client.SendAsync(HttpMethod.Post, $"api/v1/tasks/{rest[0]}/complete");
    if (!success)
    {
        Console.Error.WriteLine($"Failed to complete task: {error}");
        return 1;
    }

    PrintTask(result!.Value);
    return 0;
}

static async Task<int> TaskSearchAsync(ApiClient client, List<string> rest)
{
    if (rest.Count == 0)
    {
        Console.Error.WriteLine("Usage: desktodo task search \"<query>\"");
        return 1;
    }

    // No /search endpoint on the API (Feature 97's own deliberate scope cut) — fetch and filter
    // client-side instead of expanding that surface just for this.
    var (success, _, result, error) = await client.SendAsync(HttpMethod.Get, "api/v1/tasks");
    if (!success)
    {
        Console.Error.WriteLine($"Failed to search tasks: {error}");
        return 1;
    }

    var query = rest[0];
    var any = false;
    foreach (var task in result!.Value.EnumerateArray())
    {
        var title = task.GetProperty("title").GetString() ?? string.Empty;
        var description = task.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() ?? string.Empty : string.Empty;
        if (title.Contains(query, StringComparison.OrdinalIgnoreCase) || description.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            PrintTask(task);
            any = true;
        }
    }

    if (!any)
    {
        Console.WriteLine("No matching tasks.");
    }

    return 0;
}

static async Task<int> ProjectListAsync(ApiClient client)
{
    var (success, _, result, error) = await client.SendAsync(HttpMethod.Get, "api/v1/projects");
    if (!success)
    {
        Console.Error.WriteLine($"Failed to list projects: {error}");
        return 1;
    }

    var any = false;
    foreach (var project in result!.Value.EnumerateArray())
    {
        PrintProject(project);
        any = true;
    }

    if (!any)
    {
        Console.WriteLine("No projects.");
    }

    return 0;
}

static async Task<int> ProjectShowAsync(ApiClient client, List<string> rest)
{
    if (rest.Count == 0 || !Guid.TryParse(rest[0], out var projectId))
    {
        Console.Error.WriteLine("Usage: desktodo project show <id>");
        return 1;
    }

    // No GET /api/v1/projects/{id} endpoint — fetch the list and find it, rather than adding a
    // single-purpose endpoint just for this.
    var (success, _, result, error) = await client.SendAsync(HttpMethod.Get, "api/v1/projects");
    if (!success)
    {
        Console.Error.WriteLine($"Failed to fetch project: {error}");
        return 1;
    }

    foreach (var project in result!.Value.EnumerateArray())
    {
        if (project.GetProperty("id").GetGuid() == projectId)
        {
            PrintProject(project);
            return 0;
        }
    }

    Console.Error.WriteLine($"No project found with id {projectId}.");
    return 1;
}

static void PrintTask(JsonElement task)
{
    var id = task.GetProperty("id").GetGuid();
    var title = task.GetProperty("title").GetString();
    var priority = task.GetProperty("priority").GetString();
    var completed = task.GetProperty("isCompleted").GetBoolean();
    var status = completed ? "[x]" : "[ ]";
    Console.WriteLine($"{status} {id}  {title}  ({priority})");
}

static void PrintProject(JsonElement project)
{
    var id = project.GetProperty("id").GetGuid();
    var name = project.GetProperty("name").GetString();
    Console.WriteLine($"{id}  {name}");
}
