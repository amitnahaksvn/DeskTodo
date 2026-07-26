# DeskTodo

A native productivity desktop widget for Windows and macOS: today's date and
today's task list, always visible on the desktop — no separate window to
open. Think Sticky Notes crossed with Apple Reminders / Microsoft To Do /
TickTick, but as a lightweight always-on desktop widget rather than a
full application window.

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the layering, the
dependency graph, and the design decisions behind the current codebase.
See [IMPLEMENTATION.md](IMPLEMENTATION.md) for the phase-by-phase progress
checklist — kept up to date as each phase completes.

## Status

Solution scaffold, DI/logging/config infrastructure, the Domain task model,
EF Core/SQLite persistence, and the widget UI are in place and verified
end-to-end — today's date, live task list, complete/reopen, add/rename
(inline)/duplicate/pin/archive/delete, drag-to-reorder, a full-field task
editor (description/priority/category/due date/estimated time/notes), and
the progress bar. Day navigation, search, settings, notifications, and
import/export are not implemented yet.

## Tech stack

- C# / .NET 10
- Avalonia UI (MVVM, `CommunityToolkit.Mvvm`)
- SQLite via Entity Framework Core
- Serilog (logging)
- Microsoft.Extensions.DependencyInjection / Options (DI, configuration)
- System.Text.Json (settings persistence)
- xUnit + Moq (testing)

## Solution layout

```
DeskTodo.sln
Directory.Build.props        # shared MSBuild properties (Nullable, LangVersion, doc-gen, …)
Directory.Packages.props     # Central Package Management — every NuGet version pinned once
global.json                  # pins the .NET SDK version
src/
  DeskTodo.Domain/            # entities, value objects, enums — no dependencies
  DeskTodo.Application/       # use cases, service/repository abstractions, DTOs — depends on Domain
  DeskTodo.Infrastructure/    # EF Core + SQLite, Serilog, JSON settings persistence
  DeskTodo.Platform.Windows/  # Windows-specific interop behind Application's abstractions
  DeskTodo.Platform.Mac/      # macOS-specific interop behind Application's abstractions
  DeskTodo.App/               # Avalonia MVVM UI + composition root (DI, tray, views/viewmodels)
tests/
  DeskTodo.Tests/             # xUnit — mirrors Domain/Application/Infrastructure/ViewModels
```

## Building

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) (pinned
via `global.json` to `10.0.200`, roll-forward to latest feature band).

```bash
dotnet restore
dotnet build DeskTodo.sln
```

## Running

```bash
dotnet run --project src/DeskTodo.App
```

## Testing

```bash
dotnet test DeskTodo.sln
```

UI tests render real windows through Avalonia's headless platform (no
display needed). To also save a PNG of the rendered widget for manual
inspection:

```bash
DESKTODO_SCREENSHOT_DIR=/tmp/desktodo-screenshots dotnet test DeskTodo.sln --filter "FullyQualifiedName~WidgetWindowRenderTests"
```

## Database migrations

The EF Core CLI is pinned as a local tool (`.config/dotnet-tools.json`), not
assumed to be globally installed:

```bash
dotnet tool restore
dotnet ef migrations add <Name> \
  --project src/DeskTodo.Infrastructure/DeskTodo.Infrastructure.csproj \
  --startup-project src/DeskTodo.App/DeskTodo.App.csproj \
  --output-dir Data/Migrations
```

Migrations are applied automatically on startup — no manual `dotnet ef
database update` step is needed to run the app.

## Packaging

Windows (MSIX) and macOS (DMG) packaging pipelines are a later-stage
deliverable; this section will be filled in with `dotnet publish` /
packaging tool invocations once the app itself is feature-complete enough
to package.
