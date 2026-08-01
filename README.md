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

All 16 planned phases are complete. The widget: today's date, live task
list, complete/reopen, add/rename (inline)/duplicate/pin/archive/delete,
drag-to-reorder, a full-field task editor (description/priority/category/
due date/estimated time/notes), the progress bar, previous/today/next/
calendar day navigation, search/status-filter/category-filter/sort with
multi-select bulk complete/delete, a Settings window (accent color, widget
background opacity, remembered window position/size, notifications
toggle, start-at-login toggle), native notifications (overdue-task alerts,
a once-daily summary), and CSV/JSON/Markdown/Excel import/export.

macOS-specific pieces (notifications, auto-start, DMG packaging) are built
and verified live in a real macOS environment. The equivalent
Windows-specific pieces are authored to the same standard but **not
runtime-verified** — this project has been developed on macOS only, with no
Windows machine available to test against. Each Windows-only file says so
explicitly in its own doc comment. Theme (light/dark) and desktop-level
window placement (sitting behind desktop icons) are deliberately out of
scope — see docs/ARCHITECTURE.md's "Phase 12" and "Phase 15" sections for
why.

## Tech stack

- C# / .NET 10
- Avalonia UI (MVVM, `CommunityToolkit.Mvvm`)
- SQLite via Entity Framework Core
- Serilog (logging)
- Microsoft.Extensions.DependencyInjection / Options (DI, configuration)
- System.Text.Json (settings persistence)
- ClosedXML (Excel export)
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
scripts/                      # package-macos.sh, package-windows.ps1
packaging/
  windows/                    # AppxManifest.xml + logo assets for the MSIX build
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

Or use the wrapper script (macOS/Linux: `scripts/run.sh`, Windows:
`scripts/run.ps1`) — same command, just without needing to remember the
project path. Either passes extra arguments straight through to the app.

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

**macOS** — builds a self-contained `.app` bundle and packs it into a
`.dmg`. Verified end-to-end in this repo: publish → bundle → `hdiutil` →
mount → launch the packaged binary.

```bash
./scripts/package-macos.sh            # defaults to the host's own arch (osx-arm64 / osx-x64)
```

Output: `artifacts/macos/DeskTodo-<version>-<rid>.dmg`. Not code-signed or
notarized — that needs a real Apple Developer ID certificate. Sign +
notarize (`codesign`, `xcrun notarytool`) before distributing outside your
own machine, or Gatekeeper will require a right-click-Open the first time.

**Windows** — packs a self-contained `win-x64` publish into an unsigned
`.msix` via `makeappx.exe`. **Authored but not run** — this repo has been
developed on macOS only, with no Windows SDK to test against. Needs real
DeskTodo logo assets in `packaging/windows/Assets/` first (see that
folder's `README.md`) and a "Developer PowerShell" prompt with the Windows
SDK tools on `PATH`:

```powershell
.\scripts\package-windows.ps1
```

Output: `artifacts\windows\DeskTodo-<version>-win-x64.msix`, unsigned —
`signtool.exe sign` with a real certificate before Windows will install it.
