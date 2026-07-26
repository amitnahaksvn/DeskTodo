# DeskTodo — Implementation Plan

Tracks phase-by-phase progress on DeskTodo. Updated as each phase completes
or changes scope. For *why* things are built the way they are, see
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — this file is the checklist,
that one is the reasoning.

**Legend:** ✅ Done · 🚧 Partial · ⬜ Not started

**Last updated:** 2026-07-27 (Phase 9 completed)

> **Note on numbering:** this list mirrors the tracked work items one-to-one,
> with one deliberate exception — the DesktopSheet→DeskTodo pivot (renaming
> the scaffold, dropping Excel-specific pieces, adding EF Core/SQLite) isn't
> listed as its own phase here, since it was a one-time repo-setup event, not
> a phase of *DeskTodo* itself. It's covered in docs/ARCHITECTURE.md instead.

| # | Phase | Status |
|---|-------|--------|
| 1 | [Solution scaffold](#1-solution-scaffold) | ✅ |
| 2 | [Domain model](#2-domain-model) | ✅ |
| 3 | [Application layer: repository abstractions + TaskService](#3-application-layer-repository-abstractions--taskservice) | ✅ |
| 4 | [Infrastructure: EF Core DbContext, SQLite, migrations, repositories](#4-infrastructure-ef-core-dbcontext-sqlite-migrations-repositories) | ✅ |
| 5 | [DI wiring + auto-migrate on startup](#5-di-wiring--auto-migrate-on-startup) | ✅ |
| 6 | [Persistence-layer tests](#6-persistence-layer-tests) | ✅ |
| 7 | [Widget UI](#7-widget-ui) | ✅ |
| 8 | [Task CRUD](#8-task-crud) | ✅ |
| 9 | [Drag-to-reorder gesture + full-field task editor](#9-drag-to-reorder-gesture--full-field-task-editor) | ✅ |
| 10 | [Daily planner & calendar navigation](#10-daily-planner--calendar-navigation) | ⬜ |
| 11 | [Search / filter / sort / multi-select](#11-search--filter--sort--multi-select) | ⬜ |
| 12 | [Settings](#12-settings) | ⬜ |
| 13 | [Notifications](#13-notifications) | ⬜ |
| 14 | [Import / export](#14-import--export) | ⬜ |
| 15 | [Platform-specific integration](#15-platform-specific-integration) | ⬜ |
| 16 | [Packaging (MSIX / DMG)](#16-packaging-msix--dmg) | ⬜ |

---

## 1. Solution scaffold ✅

Clean Architecture layering, Avalonia + MVVM (`CommunityToolkit.Mvvm`),
Microsoft.Extensions.Hosting generic host, Serilog logging (console +
rolling file), Central Package Management, `Directory.Build.props`,
`global.json`.

- `DeskTodo.sln`, `src/DeskTodo.{Domain,Application,Infrastructure,Platform.Windows,Platform.Mac,App}`, `tests/DeskTodo.Tests`
- `src/DeskTodo.App/Program.cs`, `App.axaml.cs`

## 2. Domain model ✅

`TaskItem` (all spec fields: title, description, completed/completed-at,
priority, category, estimated/actual time, created/modified, due date,
notes, color, pinned/archived/deleted), `Category` (with built-in vs.
custom), `TaskPriority` enum. No single `TaskStatus` enum — completed/
pinned/archived/deleted are independent flags; overdue is computed.

- `src/DeskTodo.Domain/Entities/{TaskItem,Category}.cs`
- `src/DeskTodo.Domain/Enums/TaskPriority.cs`
- `src/DeskTodo.Domain/Exceptions/TaskNotFoundException.cs`

## 3. Application layer: repository abstractions + TaskService ✅

`ITaskRepository`/`ICategoryRepository` (per-aggregate, not a generic
`IRepository<T>`) and `ITaskService`/`TaskService` — the use-case layer
handling day-order assignment, duplication, renaming, and the
complete/pin/archive/delete state toggles.

- `src/DeskTodo.Application/Abstractions/{ITaskRepository,ICategoryRepository}.cs`
- `src/DeskTodo.Application/Services/{ITaskService,TaskService}.cs`

## 4. Infrastructure: EF Core DbContext, SQLite, migrations, repositories ✅

`DeskTodoDbContext`, Fluent configurations with seeded default categories
(keyed by fixed GUIDs so seeds don't churn migrations), `TaskRepository`/
`CategoryRepository` built on `IDbContextFactory` (each method a
self-contained unit of work — see docs/ARCHITECTURE.md for why a shared
long-lived `DbContext` isn't used), initial migration checked in,
`dotnet-ef` pinned as a local tool (`.config/dotnet-tools.json`).

- `src/DeskTodo.Infrastructure/Data/` (DbContext, Configurations, Migrations)
- `src/DeskTodo.Infrastructure/Repositories/{Task,Category}Repository.cs`

## 5. DI wiring + auto-migrate on startup ✅

`AddInfrastructure()` registers the DbContext factory, repositories and
`TaskService`; `AddDeskTodoApp()` registers ViewModels. Migrations apply
automatically on startup (`DatabaseInitializer.MigrateDeskTodoDatabaseAsync`,
called from `Program.cs`) — no manual `dotnet ef database update` step for
end users.

- `src/DeskTodo.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`
- `src/DeskTodo.App/DependencyInjection/ServiceCollectionExtensions.cs`
- `src/DeskTodo.Infrastructure/Data/DatabaseInitializer.cs`

## 6. Persistence-layer tests ✅

Domain entity behavior, `TaskService` use-case logic (Moq), and repository
tests against a *real* in-memory SQLite database (not a fake) — including
the options-binding/default-path-resolution tests from the scaffold phase.

- `tests/DeskTodo.Tests/Domain/TaskItemTests.cs`
- `tests/DeskTodo.Tests/Application/TaskServiceTests.cs`
- `tests/DeskTodo.Tests/Infrastructure/{Task,Category}RepositoryTests.cs`, `ServiceCollectionExtensionsTests.cs`

## 7. Widget UI ✅

Borderless, rounded, draggable always-visible window: day-of-week/date
header, live task list bound via `ITaskService`, completion progress bar.
Verified with real rendered screenshots via Avalonia's headless platform
(no physical display available in this dev environment — see
docs/ARCHITECTURE.md's "Headless visual verification" section).

- `src/DeskTodo.App/Views/WidgetWindow.axaml(.cs)`
- `src/DeskTodo.App/ViewModels/{Widget,TaskItem}ViewModel.cs`
- `tests/DeskTodo.Tests/Views/` (headless render tests)

## 8. Task CRUD ✅

- [x] Create (inline "Add a task…" row)
- [x] Rename (double-click title, inline edit, Enter/Escape)
- [x] Complete / undo completion (checkbox, strikethrough)
- [x] Duplicate (context menu)
- [x] Pin / unpin (context menu, glyph indicator)
- [x] Archive (context menu)
- [x] Delete — soft delete, recoverable (context menu)
- [x] Regression + headless UI tests for all of the above

Deliberately out of scope for this phase, carried forward to Phase 9: drag
gesture, full-field editing, edit auto-focus.

## 9. Drag-to-reorder gesture + full-field task editor ✅

- [x] Drag-to-reorder gesture — dedicated drag-handle glyph per row, Avalonia `DragDrop` API, `WidgetViewModel.ReorderAsync`
- [x] Full-field task editor — new `TaskEditWindow`/`TaskEditViewModel` (description, priority, category, due date, estimated minutes, notes), opened from the row context menu's "Edit" (double-click title still does the quick inline rename)
- [x] Auto-focus the inline-edit textbox when edit mode begins

- `src/DeskTodo.App/Views/TaskEditWindow.axaml(.cs)`, `WidgetWindow.axaml(.cs)` (drag handlers, edit-request handler)
- `src/DeskTodo.App/ViewModels/TaskEditViewModel.cs`, `CategoryOption.cs`
- `tests/DeskTodo.Tests/Views/TaskEditWindowRenderTests.cs`, `WidgetViewModelTests.cs`

Caught two real bugs by actually running/screenshotting this, not just
building it — see docs/ARCHITECTURE.md's "Phase 9" section: a headless-test
threading race exposed by `xunit.runner.json`'s `parallelizeTestCollections`
fix, and a DatePicker/NumericUpDown layout collision only visible in the
rendered screenshot.

## 10. Daily planner & calendar navigation ⬜

Previous/today/next day navigation, calendar date picker, per-day task
lists (already supported at the data layer via `TaskItem.PlanDate` — no
separate day/plan entity needed).

## 11. Search / filter / sort / multi-select ⬜

Search by title/category/date/notes/priority/status. Filter and sort the
task list. Multi-select for bulk complete/delete. Copy/paste, undo/redo at
the command level (beyond the per-field undo completion already done).

## 12. Settings ⬜

Theme (light/dark/auto), accent color, widget transparency/opacity, font
size, widget size, auto-start, notifications, database location, backup
frequency, keyboard shortcuts, language, date/time format, week start day.
Persisted as JSON (`AppStorageOptions.SettingsFileName` already reserved).

## 13. Notifications ⬜

Optional reminders, daily summary, morning reminder, evening review,
missed-task alerts.

## 14. Import / export ⬜

CSV, Excel, JSON, Markdown (PDF explicitly future/optional per spec).

## 15. Platform-specific integration ⬜

Auto-start/login-item registration, native notification integration,
desktop-level widget window placement (`DeskTodo.Platform.Windows` /
`DeskTodo.Platform.Mac` projects are scaffolded but not yet implemented).

## 16. Packaging (MSIX / DMG) ⬜

Windows MSIX and macOS DMG packaging pipelines, `dotnet publish` configuration,
code signing, installer/update flow.
