# DeskTodo — Implementation Plan

Tracks phase-by-phase progress on DeskTodo. Updated as each phase completes
or changes scope. For *why* things are built the way they are, see
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — this file is the checklist,
that one is the reasoning.

**Legend:** ✅ Done · 🚧 Partial · ⬜ Not started

**Last updated:** 2026-08-12 (Phases 1–16 done; Phases 17–25 fully done — including their originally-deferred items; Phases 26–37 still pending)

> **Note on numbering:** phases 1–16 mirror the tracked work items
> one-to-one, with one deliberate exception — the DesktopSheet→DeskTodo
> pivot (renaming the scaffold, dropping Excel-specific pieces, adding EF
> Core/SQLite) isn't listed as its own phase here, since it was a one-time
> repo-setup event, not a phase of *DeskTodo* itself. It's covered in
> docs/ARCHITECTURE.md instead.
>
> Phases 17+ are new: every item from `Later.Implementation.md` (the
> product-wishlist file, ~300 entries across Core Task Management, Planning,
> Grid View, Desktop Features, Productivity, Analytics, Organization,
> Reminders, Appearance, Power User, AI, Cloud, Team, Integrations,
> Import/Export, Security, and more) that isn't already built has been
> triaged into these phases, grouped by what would actually get built
> together rather than kept as ~300 loose checkboxes. Items already shipped
> in Phases 1–16 are marked done in `Later.Implementation.md` directly and
> don't reappear here.

## Phases 1–16 (done)

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
| 10 | [Daily planner & calendar navigation](#10-daily-planner--calendar-navigation) | ✅ |
| 11 | [Search / filter / sort / multi-select](#11-search--filter--sort--multi-select) | ✅ |
| 12 | [Settings](#12-settings) | ✅ |
| 13 | [Notifications](#13-notifications) | ✅ |
| 14 | [Import / export](#14-import--export) | ✅ |
| 15 | [Platform-specific integration](#15-platform-specific-integration) | ✅ |
| 16 | [Packaging (MSIX / DMG)](#16-packaging-msix--dmg) | 🚧 |

## Phases 17–25 (done)

Fully built, including every item their own "Deferred:"/scope notes
originally named — see each phase's own section below for the full detail.

| # | Phase | Source category (Later.Implementation.md) | Status |
|---|-------|---------------------------------------------|--------|
| 17 | [Subtasks, checklists, templates & rich content](#17-subtasks-checklists-templates--rich-content) | Core Task Management | ✅ |
| 18 | [Tags, labels & grouping](#18-tags-labels--grouping) | Core Task Management | ✅ |
| 19 | [Recurring tasks, dependencies & auto-reschedule](#19-recurring-tasks-dependencies--auto-reschedule) | Core Task Management, "Later" notes | ✅ |
| 20 | [Excel-style grid view](#20-excel-style-grid-view) | Spreadsheet / Grid View | ✅ |
| 21 | [Calendar, weekly/monthly/year views & alternate layouts](#21-calendar-weeklymonthlyyear-views--alternate-layouts) | Planning | ✅ |
| 22 | [System tray, global shortcuts & quick add](#22-system-tray-global-shortcuts--quick-add) | Desktop Features | ✅ |
| 23 | [Productivity tools: timers, focus & habits](#23-productivity-tools-timers-focus--habits) | Productivity | ✅ |
| 24 | [Analytics & reporting](#24-analytics--reporting) | Analytics | ✅ |
| 25 | [Organization: projects, workspaces & lists](#25-organization-projects-workspaces--lists) | Organization | ✅ |

## Extended Roadmap — Phase 26+

| # | Phase | Source category (Later.Implementation.md) | Status |
|---|-------|---------------------------------------------|--------|
| 26 | [Reminder enhancements](#26-reminder-enhancements) | Reminders | ⬜ |
| 27 | [Theming & appearance](#27-theming--appearance) | Appearance, "Later" notes | ⬜ |
| 28 | [Power user tools](#28-power-user-tools) | Power User Features | ⬜ |
| 29 | [Security & data protection](#29-security--data-protection) | Security, Import/Export | ⬜ |
| 30 | [Auto-update system](#30-auto-update-system) | "Later" notes | ⬜ |
| 31 | [Cloud sync & multi-device](#31-cloud-sync--multi-device) | Cloud Features | ⬜ |
| 32 | [Team collaboration & sharing](#32-team-collaboration--sharing) | Team Features, "Later" notes | ⬜ |
| 33 | [Third-party integrations](#33-third-party-integrations) | Integrations | ⬜ |
| 34 | [AI features](#34-ai-features) | AI Features | ⬜ |
| 35 | [Unique capture features](#35-unique-capture-features) | Unique Features | ⬜ |
| 36 | [Developer Mode dashboards](#36-developer-mode-dashboards) | Developer Mode | ⬜ |
| 37 | [Companion apps & extensions](#37-companion-apps--extensions) | Future Ideas | ⬜ |
| 38 | [have a list of task which can add to day, days, week or month on 1 click] allow user to create multiple task groups and on click it adds to ther to do list
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
- [x] Delete — soft delete (context menu), gated behind a confirmation
      dialog (see follow-up below); the underlying row/DB state is a soft
      delete, but there's no user-facing recovery UI, so the dialog is
      honest that it "can't be undone from here"
- [x] Regression + headless UI tests for all of the above

Deliberately out of scope for this phase, carried forward to Phase 9: drag
gesture, full-field editing, edit auto-focus.

**Follow-up (2026-08-02): delete confirmation + a new Task Type field.**

- Delete confirmation — every delete path (per-row context menu, the
  widget's bulk-select "Delete", and the grid's "Delete Selected") now
  routes through a new shared `ConfirmDialogWindow` (title/message/confirm-
  button text all caller-supplied, not delete-specific) before invoking the
  existing `DeleteCommand`/`BulkDeleteCommand`/`DeleteSelectedCommand` —
  those commands are unchanged, only what triggers them is gated. Avalonia
  has no built-in MessageBox the way WPF does, so this is hand-rolled.
- Task Type — a new `TaskItem.Type` field (`TaskType`: Task/Event/Reminder/
  Note/Meeting), distinct from Priority (urgency) and Category (project/
  context grouping) — answers "what kind of activity is this." Editable via
  a new "Type" picker in the full-field editor (paired with Priority);
  shown as a small icon on the widget row (📅/⏰/📝/👥), hidden entirely for
  the plain, default `Task` type to keep ordinary rows visually clean.

- `src/DeskTodo.Domain/Enums/TaskType.cs`, `src/DeskTodo.Domain/Entities/TaskItem.cs` (`Type`)
- `src/DeskTodo.App/Views/{ConfirmDialogWindow.axaml,ConfirmDialogWindow.axaml.cs}` (new)
- `src/DeskTodo.App/Views/WidgetWindow.axaml(.cs)` (`OnDeleteTaskClick`, `OnBulkDeleteClick`,
  type icon row indicator), `Views/GridWindow.axaml(.cs)` (`OnDeleteSelectedClick`)
- `src/DeskTodo.App/ViewModels/TaskEditViewModel.cs` (`Type`, `TaskTypes`),
  `Views/TaskEditWindow.axaml` ("Type" picker)
- `src/DeskTodo.App/ViewModels/TaskItemViewModel.cs` (`Type`, `TypeIcon`, `HasNonDefaultType`)
- Migration: `20260802162824_AddTaskType`
- Tests: `TaskItemViewModelTests`, `TaskEditViewModelTests`, `ConfirmDialogWindowRenderTests`

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

## 10. Daily planner & calendar navigation ✅

- [x] Previous day / Next day / Today navigation buttons
- [x] Calendar date picker to jump to any date (`CalendarDatePicker`)
- [x] Midnight auto-advance only follows along if the widget was actually
      showing "today" — navigating to a past/future day to plan ahead or
      review history isn't yanked back when the real-world day rolls over
- [x] Context-aware empty state ("No tasks for today" vs. "No tasks for this day")

Per-day task lists needed no data-layer changes — already supported via
`TaskItem.PlanDate` (see docs/ARCHITECTURE.md's "no separate day entity"
design decision from the persistence phase).

- `src/DeskTodo.App/ViewModels/WidgetViewModel.cs` (`GoToPreviousDayCommand`,
  `GoToNextDayCommand`, `GoToTodayCommand`, `SelectedDate`, `IsToday`)
- `src/DeskTodo.App/Views/WidgetWindow.axaml` (navigation bar)
- `tests/DeskTodo.Tests/ViewModels/WidgetViewModelTests.cs`

**Update (2026-07-31):** the `TimeProvider` migration this gap called for
has since landed (see Phase 12's follow-up below) — `WidgetViewModel` now
takes an injected `TimeProvider` and the rollover logic has direct
regression tests using a fake one.

## 11. Search / filter / sort / multi-select ✅

- [x] Search box (title/notes/description substring match), toggled via a header icon button
- [x] Status filter: All / Active / Completed
- [x] Category filter, populated live from `ICategoryRepository`
- [x] Sort: Manual (preserves drag-order `DayOrder`) / Priority / Due date / Title
- [x] Multi-select mode (header toggle): per-row checkboxes replace the drag handle,
      Select All / Complete / Delete bulk actions
- [x] Regression + headless UI tests for all of the above

All client-side over the already-loaded day's task list (`Tasks`), computed
into a separate `VisibleTasks` collection the row list actually binds to —
no extra DB round-trips per keystroke/filter change. Progress bar/counts
still reflect the *whole* day (`Tasks`), not the filtered view. Search by
date and command-level copy/paste/undo-redo were considered and deliberately
left out of scope — "search by date" doesn't fit a single-day widget view,
and general undo/redo doesn't fit this phase's scope.

- `src/DeskTodo.App/ViewModels/{TaskStatusFilter,TaskSortOption,CategoryFilterOption}.cs`
- `src/DeskTodo.App/ViewModels/WidgetViewModel.cs` (`VisibleTasks`, `Categories`,
  search/filter/sort properties, `RefreshVisibleTasks`, `RefreshCategoriesAsync`,
  `ToggleSelectMode`, `SelectAllVisible`, `ClearSelection`, `BulkCompleteAsync`, `BulkDeleteAsync`)
- `src/DeskTodo.App/ViewModels/TaskItemViewModel.cs` (`IsSelected`, `IsSelectModeActive`)
- `src/DeskTodo.App/Views/WidgetWindow.axaml` (search/filter row, bulk-action bar, row-level checkbox/drag-handle swap)
- `tests/DeskTodo.Tests/ViewModels/WidgetViewModelTests.cs`, `tests/DeskTodo.Tests/Views/WidgetWindowRenderTests.cs`

Caught a real bug only visible via headless rendering, not the ViewModel
tests: `RefreshCategoriesAsync` rebuilding `Categories` via `Clear()`-then-
`Add()` momentarily removed the currently-selected item from the bound
collection, desyncing the category `ComboBox`'s two-way `SelectedItem`
binding permanently (`SelectedIndex` stuck at `-1`, blank closed box, even
once the list was correctly repopulated). Fixed by updating the collection
in place (add missing / remove stale / rename changed) instead of ever
clearing it outright — see docs/ARCHITECTURE.md's "Phase 11" section.

## 12. Settings ✅

- [x] Accent color (6 presets, overrides Avalonia's `SystemAccentColor` resource — live-applies to the running widget, e.g. checkbox/progress-bar fill)
- [x] Widget background opacity/transparency (40–100% slider; text stays fully opaque, only the card behind it fades)
- [x] Remembered window position & size across restarts, with a "reset to default" action
- [x] Settings persisted as JSON (`AppStorageOptions.SettingsFileName`, via a new `ISettingsService`)
- [x] Settings window, opened via a new header gear icon
- [x] Regression + headless UI tests for all of the above

Scoped down from the phase's original wishlist (theme light/dark/auto,
font/widget size, auto-start, notifications, database location, backup
frequency, keyboard shortcuts, language, date/time format, week start day)
to settings the widget can concretely apply *today* — the rest each need a
system that doesn't exist yet (a themed-resource pass for light/dark, Phase
15's platform integration for auto-start, Phase 13 for notifications, Phase
14 for backups, a shortcut system, i18n), so adding UI for them now would
be dead controls. Revisit each once its dependency lands — see
docs/ARCHITECTURE.md's "Phase 12" section for the full reasoning.

- `src/DeskTodo.Application/Settings/AppSettings.cs`, `Abstractions/ISettingsService.cs`
- `src/DeskTodo.Infrastructure/Storage/SettingsService.cs` (JSON file, `System.Text.Json`)
- `src/DeskTodo.App/ViewModels/SettingsViewModel.cs`, `Views/SettingsWindow.axaml(.cs)`
- `src/DeskTodo.App/ViewModels/WidgetViewModel.cs` (`AccentColorHex`, `WidgetOpacity`,
  `WidgetBackgroundHex`, `WindowLeft/Top/Width/Height`, `LoadSettingsAsync`, `SaveWindowBoundsAsync`, `OpenSettingsCommand`)
- `src/DeskTodo.App/App.axaml.cs` (`ApplyAccentColor`, restores window bounds at startup before first paint)
- `src/DeskTodo.App/Views/WidgetWindow.axaml(.cs)` (gear icon, `OnClosing` persists bounds, Settings dialog hand-off)
- `tests/DeskTodo.Tests/Infrastructure/SettingsServiceTests.cs`, `ViewModels/SettingsViewModelTests.cs`, `ViewModels/WidgetViewModelTests.cs`

**Follow-up (2026-07-31): both gaps above closed.**

- Window-bounds-on-close: the assumption that this needed a real display was
  wrong — `Window.Close()` runs Avalonia's own `OnClosing`/`OnClosed` C#
  lifecycle synchronously even under the headless platform (confirmed
  empirically first, not assumed). `WidgetWindow_OnClosing_PersistsCurrentWindowBounds`
  now exercises the real close path end-to-end; verified it actually catches
  a regression by temporarily breaking the wiring and watching it fail
  before restoring the fix.
- Midnight rollover (the Phase 10 test gap): `WidgetViewModel` now takes an
  injected `TimeProvider` (`services.AddSingleton(TimeProvider.System)`;
  `Today()` replaces every direct `DateTime.Now` call), and
  `OnDayRolloverTick` is `internal` (`InternalsVisibleTo` in
  `src/DeskTodo.App/AssemblyInfo.cs`) so tests can invoke it directly against
  a fake `TimeProvider` instead of waiting on the real 30-second timer or the
  wall clock. Three new regression tests cover: advances when following
  today, stays put when viewing a different day (verified this one catches
  the original bug by breaking the `wasFollowingToday` check and watching it
  fail), and no-ops within the same day.

See docs/ARCHITECTURE.md's "Phase 12" section for the full writeup.

## 13. Notifications ✅

- [x] `INotificationService` abstraction, native macOS implementation (shells out to `osascript -e 'display notification'`) and Windows implementation (shells out to a PowerShell `NotifyIcon` balloon-tip script)
- [x] Overdue-task alerts — fires once per task the first time its due time passes while incomplete
- [x] Once-daily "you have N tasks today" summary, only while actually viewing today
- [x] "Enable notifications" toggle in Settings
- [x] Regression tests, including real (non-mocked) macOS notification calls and an AppleScript-injection test

Scoped down from the original wishlist (morning reminder, evening review at
specific times) — those would need a real background scheduler capable of
firing while the widget isn't focused, which is out of scope for a 30-second
poll timer; the daily summary serves the "morning reminder" need in
practice, since it fires whenever the widget first loads today's list.

- `src/DeskTodo.Application/Abstractions/INotificationService.cs`, `Services/NullNotificationService.cs`
- `src/DeskTodo.Platform.Mac/MacNotificationService.cs`, `src/DeskTodo.Platform.Windows/WindowsNotificationService.cs`
- `src/DeskTodo.App/ViewModels/WidgetViewModel.cs` (`CheckForOverdueTaskNotificationsAsync`, `MaybeSendDailySummaryAsync`, `NotificationsEnabled`)
- `src/DeskTodo.App/DependencyInjection/PlatformServiceCollectionExtensions.cs` (OS-conditional registration)
- `tests/DeskTodo.Tests/Infrastructure/MacNotificationServiceTests.cs`, `ViewModels/WidgetViewModelTests.cs`

**Verified live**: real `osascript` notification calls succeed in this dev
environment (confirmed before writing any code, not assumed) and the app
starts/stops cleanly with the new DI wiring. The Windows implementation is
authored but **not runtime-verified** — this sandbox has no Windows machine
— see docs/ARCHITECTURE.md's "Phase 13" section.

## 14. Import / export ✅

- [x] Export all tasks to CSV, JSON, Markdown, or Excel (`.xlsx`, via ClosedXML)
- [x] Import tasks from CSV or JSON, matching categories by name
- [x] "Import / Export tasks…" dialog, opened from Settings, using Avalonia's native file-picker `StorageProvider`
- [x] Malformed rows are skipped (logged), not fatal to the whole import
- [x] Regression tests, including a hand-rolled RFC 4180 CSV parser round-tripping commas/quotes/newlines, and a real ClosedXML read-back of the exported workbook

Markdown and Excel are export-only — Markdown's checklist format is
lossy/ambiguous to parse back unambiguously, and Excel adds cell-type/header
parsing complexity for a format most users export to for viewing, not
round-tripping.

- `src/DeskTodo.Application/Abstractions/{ITaskExportService,ITaskImportService}.cs`, `DTOs/TaskExportRecord.cs`
- `src/DeskTodo.Infrastructure/ImportExport/{TaskExportService,TaskImportService}.cs`
- `src/DeskTodo.App/ViewModels/ImportExportViewModel.cs`, `Views/ImportExportWindow.axaml(.cs)`
- `tests/DeskTodo.Tests/Infrastructure/TaskExportImportServiceTests.cs`, `ViewModels/ImportExportViewModelTests.cs`

**Known verification gap**: the native file-picker dialogs
(`StorageProvider.SaveFilePickerAsync`/`OpenFilePickerAsync`) can't be
exercised by Avalonia's headless test platform — there's no fake
`IStorageProvider` to inject a picked file into. The underlying
`ImportExportViewModel` methods (given a real `Stream`) and the export/import
*services* (given a real file) are both fully tested; only the thin
code-behind that connects a native dialog's result to those methods is
unverified.

## 15. Platform-specific integration ✅

- [x] Auto-start/login-item registration: `IAutoStartService`, macOS (writes/removes a `~/Library/LaunchAgents` plist — real, tested against a scratch path) and Windows (writes/removes an `HKCU\...\Run` registry value)
- [x] Native notification integration — see Phase 13 (same `DeskTodo.Platform.Windows`/`DeskTodo.Platform.Mac` projects)
- [x] "Start at login" toggle in Settings, seeded from `IAutoStartService.IsEnabled` (the real OS state), not a persisted flag
- ⬜ Desktop-level widget window placement (sitting behind icons, above wallpaper) — **deliberately not implemented**

The desktop-level placement item needs raw native interop with no way to
verify it here: macOS via Objective-C runtime calls to set `NSWindow.level`,
Windows via reparenting to the desktop's hidden `WorkerW` window through
Win32 messaging. Both need a live compositor/display session to visually
confirm at all, which this sandbox doesn't have, and a wrong selector or
window-level constant could crash the window rather than just fail quietly.
Discussed directly and confirmed with the user rather than assumed — see
docs/ARCHITECTURE.md's "Phase 15" section.

- `src/DeskTodo.Application/Abstractions/IAutoStartService.cs`, `Services/NullAutoStartService.cs`
- `src/DeskTodo.Platform.Mac/MacAutoStartService.cs`, `src/DeskTodo.Platform.Windows/WindowsAutoStartService.cs`
- `tests/DeskTodo.Tests/Infrastructure/MacAutoStartServiceTests.cs`

**Verified live**: real plist read/write/delete against a scratch path in
this dev environment. The Windows registry implementation is authored but
**not runtime-verified**.

## 16. Packaging (MSIX / DMG) 🚧

- [x] macOS: `scripts/package-macos.sh` — self-contained `dotnet publish`, assembles a real `.app` bundle with `Info.plist`, packs a `.dmg` via `hdiutil`
- [x] **Actually run and verified end-to-end**: produced a genuine 57 MB signed-free `.dmg`, mounted it, launched the packaged `arm64` Mach-O binary straight off the mounted image, confirmed a clean startup log
- [x] Windows: `packaging/windows/AppxManifest.xml` + `scripts/package-windows.ps1` — authored, not run
- ⬜ Code signing / notarization (macOS) and package signing (Windows) — needs real certificates this environment doesn't have; documented as a manual step for whoever ships a real release

**Known verification gap**: the Windows MSIX path (`makeappx.exe`) has never
actually been run — this sandbox has no Windows SDK. The script checks for
required logo assets upfront and fails fast with a clear message rather than
letting `makeappx` fail on an invalid manifest; those logo PNGs don't exist
yet either (only the Avalonia template placeholder icon is in the repo — see
`packaging/windows/Assets/README.md`).

---

# Extended Roadmap (Phase 17+)

Phases 17–25 are now fully built, including every item their own original
"Deferred:"/scope-note paragraphs named — see each phase's own section
below for exactly what shipped and the reasoning behind it. Phases 26–37
below remain **planning only — no code has been written for any of them.**
Each of those phases lists what it covers, why it's grouped that way, the
concrete deliverables (traced back to `Later.Implementation.md`), and the
architectural approach in prose — new entities, services, or UI surfaces —
without pre-committing to exact class names or file layouts that should be
worked out at implementation time. Phases are ordered roughly by
dependency and by how naturally they build on what already exists, not by
priority — that's a product call for whoever picks this up next.

## 17. Subtasks, checklists, templates & rich content ✅

- [x] Checklists — an ordered, per-task list of check-off-able items
      (`ChecklistItem`: text, checked, order), added/toggled/removed inline
      in the full-field editor, each persisted immediately
- [x] Task Templates — save a task's current shape (title, description,
      priority, category, estimated minutes, notes, checklist lines) as a
      named `TaskTemplate`; "New from template" (a picker in the widget's
      add-task row) seeds a new task — including its checklist — from one;
      seeded with 7 starter templates (one per built-in Category — Morning
      routine, Sprint planning prep, Study session, Workout, Weekly grocery
      run, Pay monthly bills, Family game night) so the picker isn't empty
      on a brand-new install
- [x] Subtasks — a single-level parent/child relationship
      (`TaskItem.ParentTaskId`/`Subtasks`, deliberately not a general tree —
      a subtask having its own subtasks isn't offered at the UI layer); a
      "Parent task" picker and an inline "Add a subtask" list live in the
      full-field editor, and the widget row indents itself and shows a
      subtask-count badge
- [x] Rich Text Notes — a hand-rolled minimal Markdown preview
      (`**bold**`/`*italic*`/`- bullet` lines) toggled via a "Preview"/"Edit"
      button on the Notes field — see the scope note below for why this is
      hand-rolled rather than a third-party renderer
- [x] Attachments — files copied into app storage (a 20 MB cap) and recorded
      as `Attachment` rows; attach/open/remove in the full-field editor,
      "Open" launching the OS default handler via Avalonia's `ILauncher`

**Scope note on Rich Text Notes:** Avalonia has no bundled Markdown
renderer, and pulling in a third-party one just for `**bold**`/`*italic*`/
bullet lines wasn't worth the added dependency — especially since
`TextBlock.Inlines` can't be data-bound to a converter's output the normal
way (it's a mutable collection property assigned once, not driven by a
value converter binding). `TaskEditWindow`'s code-behind rebuilds the
preview's `Inlines` by hand instead, via a small scanner — genuinely
functional for the common case, not a general Markdown parser (no nested
emphasis, no links/headers/code blocks).

- `src/DeskTodo.Domain/Entities/{ChecklistItem,TaskTemplate,Attachment}.cs`
- `src/DeskTodo.Domain/Entities/TaskItem.cs` (`ParentTaskId`/`ParentTask`/`Subtasks`)
- `src/DeskTodo.Infrastructure/Data/Configurations/{ChecklistItemConfiguration,TaskTemplateConfiguration,AttachmentConfiguration}.cs`
- `src/DeskTodo.Application/Abstractions/{IChecklistRepository,ITaskTemplateRepository,IAttachmentRepository}.cs`,
  `src/DeskTodo.Infrastructure/Repositories/{ChecklistRepository,TaskTemplateRepository,AttachmentRepository}.cs`
- `src/DeskTodo.Application/Services/{IChecklistService,ChecklistService,ITaskTemplateService,TaskTemplateService,IAttachmentService,AttachmentService}.cs`
- `src/DeskTodo.App/ViewModels/{ChecklistItemRowViewModel,SubtaskRowViewModel,AttachmentRowViewModel,TaskOption}.cs`,
  `TaskEditViewModel.cs` (checklist/subtask/attachment add-remove, `SelectedParentTask`, `IsNotesPreview`/`ToggleNotesPreviewCommand`)
- `src/DeskTodo.App/ViewModels/WidgetViewModel.cs` (`Templates`, `SelectedTemplateToApply`)
- `src/DeskTodo.App/ViewModels/TaskItemViewModel.cs` (`IsSubtask`, `SubtaskCount`)
- `src/DeskTodo.App/Converters/{BoolToNotesToggleLabelConverter,IsSubtaskToRowMarginConverter,IntGreaterThanZeroConverter}.cs`
- `src/DeskTodo.App/Views/TaskEditWindow.axaml`/`.axaml.cs` (checklist/subtask/attachment sections,
  Notes preview toggle, OS file picker, Markdown-lite renderer), `WidgetWindow.axaml` ("From template…" picker,
  subtask indent/count badge)
- Migration: `20260731190657_AddChecklistsTemplatesTagsRecurrence`,
  `20260801193307_AddSubtasksAttachmentsDependencies` (both shared with Phases 18–19),
  `20260802155355_SeedDefaultTaskTemplates`
- Tests: `ChecklistRepositoryTests`, `TaskTemplateRepositoryTests`, `AttachmentRepositoryTests`,
  `ChecklistServiceTests`, `TaskTemplateServiceTests`, `AttachmentServiceTests`,
  `TaskEditViewModelTests`, `TaskItemViewModelTests`, `WidgetViewModelTests`

## 18. Tags, labels & grouping ✅

- [x] Tags — free-form, multi-valued, user-created (`Tag`, many-to-many with
      `TaskItem`); add/remove chips in the full-field editor, get-or-create
      by name (case-insensitive) so re-typing an existing tag reuses it
      rather than duplicating
- [x] Tag filter — a search-bar dropdown alongside the existing status/category
      filters, mirroring `CategoryFilterOption`'s shape
- [x] Group By — shipped as a `TaskSortOption.Category` sort mode (clusters
      same-category rows together, uncategorized last) rather than a
      separate grouped-list UI with header rows — see scope note below
- [x] Favorite Tasks — a second boolean flag distinct from Pin
      (`TaskItem.IsFavorite`/`MarkFavorite`/`UnmarkFavorite`), toggled from
      the row context menu exactly like Pin, shown as a ⭐ row indicator
- [x] Task Color — a per-task `ColorHex` override (8-swatch palette + "none"
      in the full-field editor); the widget row's priority dot now shows it
      when set, falling back to the priority color otherwise
- [x] Recently Viewed — the last 5 tasks opened in the full-field editor,
      most-recent-first, shown as a clickable chip row in the search bar;
      session-only (not persisted — see the scope note below)

**Scope note on Group By:** implemented as an additional `TaskSortOption`
(sorting by category name visually clusters same-category rows) instead of
a header-row grouped list. This delivers the *visual* grouping outcome
without `RefreshVisibleTasks` needing to switch from a flat
`ObservableCollection<TaskItemViewModel>` to a heterogeneous
header/item collection — a meaningfully bigger change to the row list's
shape that every consumer (drag-to-reorder, bulk-select, `HasNoTasks`, ...)
would have had to account for. Grouping by priority/tag/due-date (only
category was requested strongly enough to prioritize) and true header rows
are still open if a future pass wants them.

**Scope note on Recently Viewed:** deliberately session-only, resetting on
restart — the same reasoning as `WidgetViewModel`'s existing
`_notifiedOverdueTaskIds`: re-showing an empty list once after a restart
isn't harmful, and persisting five task IDs would need its own storage for
very little benefit.

- `src/DeskTodo.Domain/Entities/Tag.cs`, `src/DeskTodo.Infrastructure/Data/Configurations/TagConfiguration.cs`
  (implicit many-to-many via EF Core skip navigations — no explicit join-entity class)
- `src/DeskTodo.Application/Abstractions/ITagRepository.cs`, `src/DeskTodo.Infrastructure/Repositories/TagRepository.cs`
- `src/DeskTodo.Application/Services/{ITagService,TagService}.cs`
- `src/DeskTodo.Domain/Entities/TaskItem.cs` (`IsFavorite`, `MarkFavorite`/`UnmarkFavorite`, `ColorHex` already existed, now wired to UI)
- `src/DeskTodo.App/ViewModels/TagChip.cs`, `TaskEditViewModel.cs` (tag add/remove, color swatches)
- `src/DeskTodo.App/ViewModels/{TagFilterOption,TaskSortOption,RecentTaskOption}.cs`, `WidgetViewModel.cs`
  (`Tags`, `SelectedTagFilter`, `RefreshTagsAsync`, `TaskSortOption.Category` handling, `RecentlyViewed`, `RequestTaskEdit`)
- `src/DeskTodo.App/ViewModels/TaskItemViewModel.cs` (`IsFavorite`, `DisplayColorHex`, `ToggleFavoriteCommand`)
- `src/DeskTodo.App/Views/WidgetWindow.axaml` (tag filter dropdown, ⭐ indicator, Favorite context-menu item,
  `DisplayColorHex` binding, Recently Viewed chip row)
- Migration: `20260731190657_AddChecklistsTemplatesTagsRecurrence` (shared with Phase 17, 19)
- Tests: `TagRepositoryTests`, `TagServiceTests`, `TaskEditViewModelTests`, `TaskItemViewModelTests`, `WidgetViewModelTests`

## 19. Recurring tasks, dependencies & auto-reschedule ✅

- [x] Recurring Tasks — `RecurrenceFrequency` (None/Daily/Weekly/Monthly) +
      `RecurrenceInterval` + optional `RecurrenceEndDate` on `TaskItem`,
      editable in the full-field editor; `TaskItem.GetNextOccurrencePlanDate()`
      is a pure computation, and `TaskService.CompleteTaskAsync` creates the
      next occurrence (copying title/description/priority/category/estimate/
      notes/color/recurrence settings — not checklist/tags) when completing a
      recurring task, unless the next date would fall after the end date
- [x] Auto-reschedule overdue tasks — a Settings toggle
      (`AppSettings.AutoRescheduleOverdueTasks`, **defaults to off** — moving
      a task's `PlanDate` is a real data change, not a display preference, so
      it stays opt-in). When on, `WidgetViewModel` bumps every incomplete,
      non-archived task from a past day onto today the first time today's
      list loads each calendar day (mirrors `MaybeSendDailySummaryAsync`'s
      once-per-day guard), appended to the end of the day's list
- [x] Task Dependencies — a `TaskDependency` join entity
      (`BlockingTaskId`/`BlockedTaskId`, both `Restrict`-deleted FKs to
      `TaskItem`, a unique composite index against duplicate assignment);
      `TaskItem.IsBlocked` is computed from `BlockedByDependencies` (true
      while any blocker is still incomplete); `TaskService.CompleteTaskAsync`
      throws `TaskBlockedException` for a blocked task instead of silently
      completing it; the full-field editor gets a "Blocked by" section (chip
      list + add picker) and the widget row shows a 🔒 indicator — see the
      scope note below for what cycle-prevention deliberately doesn't cover

**Approach note:** recurrence's "create the next occurrence" logic lives in
`TaskService.CompleteTaskAsync` rather than a separate background poller —
completion is the only event that can produce a next occurrence, so there's
no need for `WidgetViewModel`'s 30-second timer to also carry this check.
Task Dependencies uses a plain join entity rather than tags' many-to-many
skip navigation, because "blocks"/"is blocked by" is directionally
asymmetric — unlike a tag assignment, which side of the pair is which
matters.

**Scope note on Task Dependencies' cycle prevention:**
`TaskDependencyService.AddBlockerAsync` refuses self-blocking and a direct
two-task cycle (A blocks B, then B blocks A) via two `ExistsAsync` checks,
but does **not** walk the full dependency graph to catch a deeper
transitive cycle (A blocks B, B blocks C, C blocks A). A user would need to
deliberately construct that shape for it to matter, so it's documented here
as a known, narrow limitation rather than left silently unhandled.

- `src/DeskTodo.Domain/Enums/RecurrenceFrequency.cs`
- `src/DeskTodo.Domain/Entities/TaskItem.cs` (`RecurrenceFrequency`/`RecurrenceInterval`/`RecurrenceEndDate`, `GetNextOccurrencePlanDate`,
  `BlockedByDependencies`/`BlockingDependencies`, `IsBlocked`)
- `src/DeskTodo.Domain/Entities/TaskDependency.cs`, `src/DeskTodo.Domain/Exceptions/TaskBlockedException.cs`
- `src/DeskTodo.Infrastructure/Data/Configurations/TaskDependencyConfiguration.cs`
- `src/DeskTodo.Application/Abstractions/ITaskDependencyRepository.cs`, `src/DeskTodo.Infrastructure/Repositories/TaskDependencyRepository.cs`
- `src/DeskTodo.Application/Services/{ITaskDependencyService,TaskDependencyService}.cs`
- `src/DeskTodo.Application/Services/TaskService.cs` (`CompleteTaskAsync`'s next-occurrence creation + blocked-guard, `RescheduleOverdueTasksAsync`)
- `src/DeskTodo.Application/Abstractions/ITaskRepository.cs`, `src/DeskTodo.Infrastructure/Repositories/TaskRepository.cs` (`GetIncompleteBeforeDateAsync`, blocker `Include`s)
- `src/DeskTodo.Application/Settings/AppSettings.cs` (`AutoRescheduleOverdueTasks`), `SettingsViewModel.cs`, `Views/SettingsWindow.axaml`
- `src/DeskTodo.App/ViewModels/{TaskEditViewModel,BlockerChip,TaskOption}.cs` (recurrence fields, Blocked-by section),
  `Views/TaskEditWindow.axaml` ("Repeat" section, Blocked-by chips + add picker)
- `src/DeskTodo.App/ViewModels/WidgetViewModel.cs` (`MaybeRescheduleOverdueTasksAsync`)
- `src/DeskTodo.App/ViewModels/TaskItemViewModel.cs` (`IsBlocked`), `Views/WidgetWindow.axaml` (🔒 indicator)
- `src/DeskTodo.App/Converters/RecurrenceFrequencyToBoolConverter.cs`
- Migration: `20260731190657_AddChecklistsTemplatesTagsRecurrence` (shared with Phases 17–18),
  `20260801193307_AddSubtasksAttachmentsDependencies` (shared with Phase 17)
- Tests: `TaskServiceTests` (recurrence + reschedule + blocked-completion), `TaskRepositoryTests`,
  `TaskDependencyRepositoryTests`, `TaskDependencyServiceTests`, `SettingsViewModelTests`,
  `SettingsServiceTests`, `WidgetViewModelTests`, `TaskEditViewModelTests`, `TaskItemViewModelTests`

## 20. Excel-style grid view ✅

- [x] An Excel-like grid as an alternate view of the task list — every
      non-archived task across every day, opened from a new header icon
      button on the widget (`GridWindow`, a separate dialog window; doesn't
      replace the compact widget)
- [x] Inline cell editing — Title/Notes (text), Date/Due (date pickers),
      Priority/Category (dropdowns), Done (checkbox); each cell edit
      persists immediately (no separate Save button, matching every other
      immediate-persistence surface in this app)
- [x] Multi-row selection + bulk update — a per-row selection checkbox
      column (`DataGrid.SelectedItems` isn't two-way bindable in Avalonia's
      `DataGrid`, so this mirrors the widget's own Phase 11 bulk-select
      pattern instead) + a "Delete Selected" bulk action
- [x] Resizable, reorderable, sortable columns — `DataGrid`'s own built-in
      `CanUserResizeColumns`/`CanUserReorderColumns`/`CanUserSortColumns`,
      not custom-built
- [x] Copy/paste to and from real Excel (TSV clipboard interop) —
      `GridViewModel.BuildClipboardText()`/`PasteFromClipboardAsync` build
      and parse tab-separated text (Excel's own copy format), wired to
      Copy/Paste buttons in `GridWindow`; copies the selection or every row
      if nothing's selected — see the scope note below for the `IClipboard`
      API discovery this needed
- [x] Hideable + freezable columns — a "Columns" flyout (Category/Due/Notes/
      Status/Progress can be hidden; Title/Date/Priority/Done stay
      mandatory) plus a "Freeze checkbox + Title columns" toggle, both
      persisted via `AppSettings.HiddenGridColumns`/`GridColumnsFrozen` and
      restored the next time the grid opens
- [x] Saved column-layout "views" — a "Views" flyout: save the grid's
      current hidden-column set under a name (`AppSettings.GridSavedViews`),
      apply or delete a saved view later; column widths/order still aren't
      captured, only visibility — see the scope note below
- [x] Dedicated Progress/Status columns — `TaskGridRowViewModel.StatusDisplay`
      (Done/Overdue/Due Today/Upcoming/No due date, derived from
      `IsCompleted`/`DueDate`) and `ProgressDisplay` (checked/total checklist
      items, or "—" with none), both read-only and derived, not raw
      persisted fields

**Scope note:** `Avalonia.Controls.DataGrid` (the official first-party
package, version-matched to the rest of the pinned Avalonia 12.1.0 stack)
was added rather than building a custom grid or reaching for a third-party
package — it already provides resize/reorder/sort, editable columns, and a
checkbox column out of the box. Column-type/API choices (`DataGridTemplateColumn`
for date/dropdown cells, `DataGridCollectionView`-driven sort via
`SortMemberPath`, no `DataGridComboBoxColumn` — it doesn't exist in this
package) were confirmed against the actual compiled assembly via reflection
before use, not assumed from general DataGrid familiarity.

**Scope note on the clipboard API:** `IClipboard` in the pinned Avalonia
`12.1.0` does **not** expose `SetTextAsync`/`GetTextAsync` directly, despite
that being the shape older Avalonia/WPF familiarity would suggest —
reflecting over the real compiled interface first (before writing
`GridWindow.axaml.cs`'s copy/paste handlers) found it's built around a
newer `IAsyncDataTransfer`/`SetDataAsync`/`TryGetDataAsync` API instead. The
classic convenience methods still exist, just as extension methods —
`Avalonia.Input.Platform.ClipboardExtensions.SetTextAsync(IClipboard,
string)` and `TryGetTextAsync(IClipboard)` (note: `TryGetTextAsync`, not
`GetTextAsync`) — found via a further reflection pass and used from the
start, the same "verify the real compiled API before depending on it"
discipline as the `DataGridComboBoxColumn`/`SelectedItems` findings above.

**Scope note on hidden columns and freeze:** the "Columns" flyout's
`CheckBox`es (including the freeze toggle) aren't realized as controls
until the flyout is actually opened, so syncing their checked state to the
persisted setting happens in a `Flyout.Opened` handler — separately from
`GridWindow.OnOpened`, which applies the actual `DataGridColumn.IsVisible`
values and `DataGrid.FrozenColumnCount` unconditionally, since
`DataGrid.Columns`/the `DataGrid` itself exist immediately regardless of
whether the flyout has ever been shown. Column visibility is tracked by a
fixed column *index* (a static `ColumnVisibilityMap`), not by name, since
`DataGridColumn` has no stable name/key the way an `x:Name`d `Control`
would. Freeze stays a binary "checkbox + Title columns, or nothing" toggle
rather than an arbitrary user-chosen freeze point — the two-column default
is the only shape that's actually useful here (Title is the one column you
always want visible while scrolling right), so a more general "freeze N
columns" control wasn't worth the added UI.

**Scope note on saved views:** a named `GridSavedView` (name + hidden-column
set) is deliberately a thin snapshot of the *same* shape as the single
"current" layout (`AppSettings.HiddenGridColumns`) — column width/order/
sort/freeze-state aren't captured, matching the scope the single-layout
version already settled on. `GridViewModel.SaveCurrentViewAsync` overwrites
an existing view of the same name (case-insensitive) rather than erroring
or silently creating a duplicate, since "save over my existing view" is the
more common intent than "let me have two views with the same name."
`ApplyViewAsync` works by copying the saved view's hidden-column list into
`HiddenGridColumns` — the *same* setting the "Columns" flyout already
edits — so applying a view and manually toggling a column afterward compose
naturally instead of being two disconnected mechanisms.

- `Directory.Packages.props`, `src/DeskTodo.App/DeskTodo.App.csproj` (`Avalonia.Controls.DataGrid`)
- `src/DeskTodo.App/App.axaml` (`StyleInclude` for the DataGrid's Fluent theme)
- `src/DeskTodo.App/ViewModels/{GridViewModel,TaskGridRowViewModel}.cs`
  (`BuildClipboardText`, `EscapeCell`, `PasteFromClipboardAsync`, `GetHiddenColumnsAsync`,
  `SetColumnHiddenAsync`, `GetColumnsFrozenAsync`/`SetColumnsFrozenAsync`, `GetSavedViewsAsync`,
  `SaveCurrentViewAsync`/`DeleteSavedViewAsync`/`ApplyViewAsync`, `StatusDisplay`/`ProgressDisplay`)
- `src/DeskTodo.App/Views/{GridWindow.axaml,GridWindow.axaml.cs}` ("Views" + "Columns" flyouts,
  Copy/Paste buttons, `ColumnVisibilityMap`, `OnColumnsFlyoutOpened`/`OnFreezeColumnsChanged`,
  `OnViewsFlyoutOpened`/`OnApplyViewClick`/`OnDeleteViewClick`/`OnSaveViewClick`, `OnCopyClick`/`OnPasteClick`)
- `src/DeskTodo.App/ViewModels/WidgetViewModel.cs` (`GridViewRequested`, `OpenGridViewCommand`),
  `Views/WidgetWindow.axaml` (grid header icon), `Views/WidgetWindow.axaml.cs` (`OnGridViewRequested`)
- `src/DeskTodo.Application/Settings/{AppSettings,GridSavedView}.cs` (`HiddenGridColumns`, `GridColumnsFrozen`, `GridSavedViews`)
- No new migration — reads/writes the same `TaskItem`/`ChecklistItem` fields already modeled,
  plus JSON-persisted `AppSettings` fields (no schema migration needed for those)
- Tests: `GridViewModelTests` (incl. clipboard build/parse, hidden-column/freeze/saved-view persistence),
  `TaskGridRowViewModelTests`, `SettingsServiceTests` (round-trips the new settings fields),
  `GridWindowRenderTests` (headless, incl. a screenshot-verified render)

## 21. Calendar, weekly/monthly/year views & alternate layouts ✅

The widget only ever shows one day at a time (by design — Phase 10's daily
planner). This phase is about *viewing more than one day at once*, in
several different shapes.

- [x] Calendar View — a real month-grid calendar (`CalendarWindow`, a fixed
      7x6 cell grid, always 42 cells so switching months never reflows the
      window's height), with a completion/task-count indicator per day;
      also serves as the Monthly Planner deliverable below (a month grid
      already shows a full month's shape — a separate near-duplicate screen
      wasn't worth building)
- [x] Weekly Planner — a "Week" tab in a new `PlannerWindow` (seven day
      cells, same cell type the Calendar view uses)
- [x] Monthly Planner — see Calendar View above
- [x] Year Planner — a "Year" tab: 12 month tiles, each a task-count summary
      rather than a mini-calendar (see scope note below)
- [x] Agenda View — an "Agenda" tab: incomplete tasks across the next 14
      days (including anything overdue), grouped by date with a friendly
      label ("Today"/"Tomorrow"/day name)
- [x] Timeline View — a "Timeline" tab: every incomplete task with a due
      date, in chronological order (see scope note below for why this is a
      plain list, not a proportionally-drawn axis)
- [x] Kanban Board — a "Kanban" tab: To Do / Done columns, reusing
      `TaskItem.IsCompleted` (see scope note below for why, not a new
      status concept)
- [x] Eisenhower Matrix — a "Matrix" tab: a 2x2 grid derived purely from
      Priority (High/Critical = important) and due-date proximity (due
      within 2 days, or overdue = urgent) — no new persistence
- [x] Goal Planner — a "Goals" tab: personal, ongoing habit-style targets
      (new `Goal`/`GoalCompletion` entities), tracked by a computed daily
      streak (`Goal.GetCurrentStreak`) — "Mark done today" logs one
      completion row per calendar day
- [x] Milestones, Sprint Planner, Roadmap View — a "Milestones" tab: a new
      `Milestone` entity with an optional target date that tasks can link
      to (`TaskItem.MilestoneId`, a "Milestone" picker in the full-field
      editor), showing each milestone's linked-task progress; this one
      chronologically-ordered list serves Sprint Planner *and* Roadmap View
      both — see the scope note below for why

**Approach:** Calendar/Week/Year/Agenda/Timeline/Kanban/Matrix are all
read-only *views* over the same `TaskItem` data `ITaskService.GetAllTasksAsync`
already provides (Phase 14/20) — no new persistence for any of them.
Goal/Milestone are the exception — the phase's own original scope note
flagged these four wishlist items ("Goal Planner, Milestones, Sprint
Planner, Roadmap View") as needing a genuinely new persisted concept before
they could be built at all, and they're the only pieces of this phase that
actually needed one: two new entities (`Goal` + its `GoalCompletion` log,
and `Milestone`) plus a nullable FK from `TaskItem`. Week/Year/Agenda/
Timeline/Kanban/Matrix/Goals/Milestones live as tabs in one `PlannerWindow`
rather than eight separate windows/header icons (Calendar stays its own
window/icon, since it was built first and a month grid's interaction shape
— click a day, jump there — is different enough from the others' list/card
shapes to earn its own screen). Clicking a day or task in a date-bearing
tab navigates the widget to it and closes the window; Goals/Milestones rows
don't (see docs/ARCHITECTURE.md's "Phase 21" section for why).

**Scope note on the Year view:** 12 simultaneous 7x6 mini-calendars would
be either illegibly tiny or need a much taller window than this app's other
dialogs use, so each month is a summary tile ("N/M done") instead — a
year-level view answering "how busy was this month," with day-level detail
left to the Month/Week tabs.

**Scope note on the Timeline view:** a plain chronological list (due date +
title per row), not tasks positioned along a proportionally-scaled drawn
axis. A true scaled timeline needs custom `DrawingContext`/`Canvas` layout,
axis-scaling, and same-day overlap handling — a materially bigger UI
engineering effort for a view whose actual job ("what's due, in order") a
plain list already delivers. Still open if a future pass specifically wants
the visual version.

**Scope note on the Kanban board:** two columns (To Do/Done) reusing
`TaskItem.IsCompleted` directly, not a new persisted "status" (the original
wishlist shape was a three-plus-column board — To Do/In Progress/Done —
but nothing in the domain model distinguishes "not started" from "in
progress," and inventing that distinction is a separate scoping decision,
not something to bolt on silently here). Moving a card between columns is a
button click ("Move to Done"/"Move to To Do"), not drag-and-drop — a real
drag gesture (Avalonia's `DragDrop` API, already used for the widget's own
row reordering) is a reasonable follow-up, deliberately not built here to
keep this pass's scope to the underlying To Do/Done model itself.

**Scope note on Goal vs. Milestone:** two deliberately separate entities,
not one concept split by a type flag — a personal habit-style goal (no end
date, tracked by a daily streak) and a project-management milestone (a
fixed deliverable with a target date that tasks link to) answer genuinely
different questions, decided directly with the user rather than guessed:
"add personal habit style goal and project style milestone" — both, as two
concepts. `Sprint Planner` and `Roadmap View` were folded into the same
Milestones tab rather than built as separate screens, since a
chronologically-ordered milestone list already reads as "what's coming up
next" (Sprint Planner) at the top and as the full timeline (Roadmap View)
scrolling down — see docs/ARCHITECTURE.md's "Phase 21" section for the full
reasoning, including why the streak is computed from a completion log
rather than a cached counter, why Milestone deletion unlinks tasks instead
of deleting them, and why Goals/Milestones don't navigate the widget the
way every other tab's rows do.

- `src/DeskTodo.Domain/Entities/{Goal,GoalCompletion,Milestone}.cs`,
  `src/DeskTodo.Domain/Entities/TaskItem.cs` (`MilestoneId`/`Milestone`),
  `src/DeskTodo.Domain/Exceptions/{GoalNotFoundException,MilestoneNotFoundException}.cs`
- `src/DeskTodo.Infrastructure/Data/Configurations/{GoalConfiguration,GoalCompletionConfiguration,MilestoneConfiguration}.cs`,
  `TaskItemConfiguration.cs` (Milestone FK, `SetNull`)
- `src/DeskTodo.Application/Abstractions/{IGoalRepository,IMilestoneRepository}.cs`,
  `src/DeskTodo.Infrastructure/Repositories/{GoalRepository,MilestoneRepository}.cs`
- `src/DeskTodo.Application/Services/{IGoalService,GoalService,IMilestoneService,MilestoneService}.cs`
- `src/DeskTodo.App/ViewModels/{CalendarViewModel,CalendarDayViewModel}.cs`,
  `Views/{CalendarWindow.axaml,CalendarWindow.axaml.cs}`
- `src/DeskTodo.App/ViewModels/{PlannerViewModel,WeekViewModel,YearViewModel,YearMonthSummaryViewModel,
  AgendaViewModel,AgendaGroupViewModel,TimelineViewModel,KanbanViewModel,KanbanCardViewModel,
  MatrixViewModel,MatrixQuadrantViewModel,PlannerTaskRowViewModel,PriorityColors,
  GoalsViewModel,GoalRowViewModel,MilestonesViewModel,MilestoneRowViewModel,MilestoneOption}.cs`,
  `Views/{PlannerWindow.axaml,PlannerWindow.axaml.cs}`
- `src/DeskTodo.App/Converters/{BoolToTodayBackgroundConverter,BoolToCurrentMonthOpacityConverter}.cs`
- `src/DeskTodo.App/ViewModels/TaskEditViewModel.cs` (`MilestoneOptions`/`SelectedMilestone`),
  `Views/TaskEditWindow.axaml` ("Milestone" picker)
- `src/DeskTodo.App/ViewModels/WidgetViewModel.cs` (`CalendarViewRequested`/`OpenCalendarViewCommand`,
  `PlannerViewRequested`/`OpenPlannerViewCommand`), `Views/WidgetWindow.axaml` (📅/📋 header icons),
  `Views/WidgetWindow.axaml.cs` (`OnCalendarViewRequested`, `OnPlannerViewRequested`)
- Migration: `20260803195217_AddGoalsAndMilestones`
- Tests: `GoalTests` (streak computation), `GoalRepositoryTests`, `MilestoneRepositoryTests`,
  `GoalServiceTests`, `MilestoneServiceTests`, `GoalsViewModelTests`, `MilestonesViewModelTests`,
  `CalendarViewModelTests`, `WeekViewModelTests`, `YearViewModelTests`, `AgendaViewModelTests`,
  `TimelineViewModelTests`, `KanbanViewModelTests`, `MatrixViewModelTests`, `PlannerViewModelTests`,
  `TaskEditViewModelTests`, `WidgetViewModelTests`, `CalendarWindowRenderTests`,
  `PlannerWindowRenderTests` (headless, incl. a screenshot-verified render of every tab)

## 22. System tray, global shortcuts & quick add ✅

The widget previously only existed as its own window — there was no tray
icon, no way to summon it via a keyboard shortcut, and no lightweight "just
add a task from anywhere" flow. This phase is about *ambient* access to
the app, and all seven originally-listed deliverables are fully built.

**Delivered:**
- System Tray icon (Windows) / macOS Menu Bar item, via Avalonia's
  cross-platform `TrayIcon` API — a context menu (Show/Hide Widget, Quick
  Add…, Settings…, Quit), live-verified rendering correctly in the real
  macOS menu bar (confirmed both visually and via `System Events`
  querying the live `NSStatusItem`)
- Minimize to Tray — the widget's own close button hides it instead of
  exiting; only the tray's "Quit" item calls `TryShutdown`
- Global Shortcut (Cmd/Ctrl+Shift+N, opens Quick Add) — `IGlobalHotkeyService`
  abstraction, macOS via Carbon's `RegisterEventHotKey`, Windows via
  User32's `RegisterHotKey`. Live-verified end-to-end on macOS: registered
  the real hotkey, ran an actual Carbon event loop, and confirmed
  `Pressed` fired after simulating the exact key combo via `osascript`
- Quick Add Window — `QuickAddViewModel` (title + priority + category,
  deliberately not the full editor) + `QuickAddWindow`, live-verified:
  opens with focus and populated dropdowns, Enter creates the task and
  refreshes the widget
- Mini Widget — an `IsMiniWidgetMode` Settings-persisted toggle that
  collapses the widget to just its header + today's progress bar (reusing
  the existing `ProgressSummaryText`/`ProgressPercentage`, not a new
  concept), with `WidgetWindow` shrinking to a fixed compact size and
  restoring on toggle-off. Live-verified via both a full toggle-off round
  trip and the accessibility-reported window geometry (340×148 in mini
  mode vs. 340×560 default)
- Multi Monitor Support — a "Monitor" picker in Settings backed by
  `Screens.All`, persisting a `DisplayName`+`Bounds` identity string
  (`MonitorIdentity`) since Avalonia's `Screen` exposes no native unique
  ID. Live-verified: the picker correctly listed this dev machine's two
  real connected monitors ("Built-in Retina Display (1512×982) —
  Primary" and "SAMSUNG (1920×1080)") with accurate resolutions

**Scope note on the Global Shortcut's Windows implementation:** authored
but not runtime-verified, matching this whole project's established
Windows-code precedent (no Windows machine in this dev environment) — see
`WindowsGlobalHotkeyService`'s own doc comment for the specific risk that
caveat covers (an unverified P/Invoke signature throwing on a background
thread, defensively caught to avoid crashing the whole process).

**A genuine bug caught during live verification:** the original
`MacGlobalHotkeyService` implementation called Carbon's
`NewEventHandlerUPP` to wrap the event handler delegate before passing it
to `InstallEventHandler` — standard-looking Carbon usage modeled on
real-world hotkey libraries. Live-testing in a bare console host (not
just the full Avalonia app) surfaced an `EntryPointNotFoundException`:
`NewEventHandlerUPP` isn't actually exported from the modern 64-bit
Carbon.framework shared library at all — it only ever existed as a
32-bit compatibility trampoline, and Apple's own headers `#define` it as
a no-op identity macro in 64-bit builds. The fix was to pass the raw
marshaled function pointer directly to `InstallEventHandler`, skipping
the call entirely — exactly the kind of assumption this project's
"verify against the real compiled thing, don't trust general API
familiarity" discipline exists to catch.

- `src/DeskTodo.Application/Abstractions/IGlobalHotkeyService.cs`,
  `src/DeskTodo.Application/Services/NullGlobalHotkeyService.cs`
- `src/DeskTodo.Platform.Mac/MacGlobalHotkeyService.cs` (Carbon P/Invoke),
  `src/DeskTodo.Platform.Windows/WindowsGlobalHotkeyService.cs` (User32
  P/Invoke, dedicated Win32 message-loop thread)
- `src/DeskTodo.App/DependencyInjection/PlatformServiceCollectionExtensions.cs`
  (per-OS `IGlobalHotkeyService` registration), `ServiceCollectionExtensions.cs`
  (`QuickAddViewModel` registration)
- `src/DeskTodo.App/ViewModels/QuickAddViewModel.cs`,
  `Views/{QuickAddWindow.axaml,QuickAddWindow.axaml.cs}`
- `src/DeskTodo.App/App.axaml.cs` (`IsQuitting`, `SetupTrayIcon`,
  `ToggleWidgetVisibility`, `OpenQuickAdd`, `SetupGlobalHotkey`, monitor
  resolution at startup)
- `src/DeskTodo.App/Views/WidgetWindow.axaml.cs` (`OnClosing` minimize-to-tray,
  `ApplyMiniWidgetModeSize`, `RepositionOnMonitor`),
  `Views/WidgetWindow.axaml` (mini-toggle header button, mini-mode
  `IsVisible` bindings on the day-nav/search/add-task/task-list rows)
- `src/DeskTodo.App/ViewModels/WidgetViewModel.cs` (`IsMiniWidgetMode`,
  `ToggleMiniWidgetModeCommand`, `PreferredMonitorId`),
  `Converters/MiniWidgetModeToGlyphConverter.cs`
- `src/DeskTodo.App/{MonitorIdentity.cs,ViewModels/MonitorOption.cs}`,
  `ViewModels/SettingsViewModel.cs` (`Monitors`/`SelectedMonitor`/`SetAvailableMonitors`),
  `Views/SettingsWindow.axaml` ("Monitor" picker)
- `src/DeskTodo.Application/Settings/AppSettings.cs`
  (`IsMiniWidgetMode`, `PreferredMonitorId`)
- Tests: `QuickAddViewModelTests`, `MacGlobalHotkeyServiceTests` (exercised
  for real against the live Carbon APIs, same precedent as
  `MacAutoStartServiceTests`), `WidgetViewModelTests` (mini widget toggle,
  settings round-trip), `SettingsViewModelTests` (monitor selection,
  persistence, fallback when a saved monitor is no longer connected)

## 23. Productivity tools: timers, focus & habits ✅

DeskTodo previously tracked only *what* to do and *whether* it's done, not
time spent doing it or wellness/focus routines around it. Four of the five
originally-listed deliverables are now fully built; the fifth (Productivity
Score) was always scoped to Phase 24 by this section's own original text,
since it overlaps entirely with Analytics — not a new deferral.

**Delivered:**
- Pomodoro Timer, Stopwatch, Focus Timer, Focus Mode, Deep Work Session —
  one shared timer engine (`FocusTimerViewModel`, a DI singleton) with
  three real mechanisms (`FocusSessionType`: Pomodoro's alternating work/
  break cycle, an open-ended Stopwatch, and a CountdownTimer). "Focus
  Timer"/"Focus Mode"/"Deep Work Session" are all the CountdownTimer
  mechanism at different preset lengths (25/50/90-minute quick buttons,
  or any custom duration), not three separate concepts — see
  `FocusSessionType`'s own doc comment. A widget header icon opens
  `FocusTimerWindow`; a header indicator bound to the same singleton
  instance shows "⏱ MM:SS" whenever a session is running, whether or not
  the timer window itself is open. Live-verified end-to-end: started a
  real countdown, watched it tick down for real, confirmed the widget's
  indicator updates from the same shared instance, stopped it, and
  confirmed a genuine `FocusSessions` row landed in the database
- Break Reminder, Water Reminder, Stretch Reminder — three independent
  toggle+interval settings, each checked on the widget's existing
  30-second poll timer (the same one Phase 13's overdue-task check
  already uses) and delivered via the existing `INotificationService`.
  Off by default. Live-verified via a controllable fake clock in tests
  (immediate-after-enable doesn't fire; fires once the interval elapses;
  doesn't re-fire early) and the Settings UI rendering/hiding correctly
- Time Tracking, Actual Time — a new `FocusSession` log entity (one row
  per completed/stopped session, written once at stop-time, not ticked)
  drives `TaskItem.ActualMinutes` — a field that existed since early
  phases but had no UI or writer until now. Completing a session linked
  to a task adds its duration onto that task's `ActualMinutes` via a new
  `ITaskService.AddActualMinutesAsync`. The full-field editor shows
  "Actual minutes" (read-only — it accumulates from real sessions, not
  hand-typed) next to a "Start Timer" button that preselects the task in
  the shared Focus Timer window. Live-verified: opened a task's editor,
  clicked Start Timer, confirmed the task appeared preselected in the
  (already-shared, already-singleton) Focus Timer window

**Scope note on Habit Tracker / Daily-Weekly-Monthly Goals:** already
satisfied by Phase 21's `Goal`/`GoalCompletion` entities — a recurring,
streak-tracked commitment distinct from a one-off task is exactly what a
habit tracker is. No new entity was built for this; `Goal`'s own doc
comment already called it "a personal, ongoing habit-style target." The
one named gap: `Goal`'s streak logic assumes a daily cadence (consecutive
calendar days), so it doesn't yet model a "3×/week" style habit — an
honest, named limitation for a future pass, not something silently
declared done.

**Approach, as built:** `FocusTimerViewModel.OnTick` is `internal` (via
this project's established `InternalsVisibleTo` pattern, same as
`WidgetViewModel.OnDayRolloverTick`) so tests drive the timer
deterministically by calling it directly instead of waiting on the real
1-second `DispatcherTimer`. Sessions under a minute aren't logged — an
accidental Start-then-Stop shouldn't leave a stray zero-minute row. A
Pomodoro's break phase is never logged as time worked, only completed (or
partially-completed, past one minute) work phases are.

- `src/DeskTodo.Domain/Entities/FocusSession.cs`,
  `src/DeskTodo.Domain/Enums/FocusSessionType.cs`
- `src/DeskTodo.Infrastructure/Data/Configurations/FocusSessionConfiguration.cs`
  (SetNull FK to `Tasks`, mirroring Milestone's Phase 21 reasoning),
  `Repositories/FocusSessionRepository.cs`
- `src/DeskTodo.Application/Abstractions/IFocusSessionRepository.cs`,
  `Services/{IFocusSessionService,FocusSessionService}.cs`
- `src/DeskTodo.Application/Services/ITaskService.cs`/`TaskService.cs`
  (`AddActualMinutesAsync`)
- `src/DeskTodo.Application/Settings/AppSettings.cs`
  (`PomodoroWorkMinutes`/`PomodoroBreakMinutes`,
  `{Break,Water,Stretch}ReminderEnabled`/`IntervalMinutes`)
- `src/DeskTodo.App/ViewModels/FocusTimerViewModel.cs` (DI singleton — see
  `ServiceCollectionExtensions.cs`), `Views/{FocusTimerWindow.axaml,FocusTimerWindow.axaml.cs}`
  (`ShowOrActivate` — one shared window across every entry point)
- `src/DeskTodo.App/ViewModels/WidgetViewModel.cs`
  (`FocusTimerRequested`/`OpenFocusTimerCommand`,
  `OnWellnessReminderCheckTick`/`CheckWellnessRemindersAsync`),
  `Views/WidgetWindow.axaml` (⏱ header icon, `FocusTimerIndicator` bound
  to the singleton `FocusTimerViewModel`, not this window's own ViewModel),
  `Views/WidgetWindow.axaml.cs` (`OnFocusTimerRequested`)
- `src/DeskTodo.App/ViewModels/TaskEditViewModel.cs` (`ActualMinutes`,
  `TaskId`, `StartTimerRequested`/`StartTimerCommand`),
  `Views/TaskEditWindow.axaml` (Actual minutes + Start Timer row),
  `Views/TaskEditWindow.axaml.cs` (`OnStartTimerRequested`)
- `src/DeskTodo.App/ViewModels/SettingsViewModel.cs` (Pomodoro + reminder
  properties), `Views/SettingsWindow.axaml` (now scrollable; Focus Timer
  and Wellness reminders sections)
- Migration: `20260811192129_AddFocusSessions`
- Tests: `FocusSessionRepositoryTests`, `FocusSessionServiceTests`,
  `FocusTimerViewModelTests` (the timer state machine, driven via
  `OnTick` directly), `TaskServiceTests` (`AddActualMinutesAsync`),
  `WidgetViewModelTests` (wellness reminder timing via a controllable
  fake clock), `SettingsViewModelTests`, `TaskEditViewModelTests`

## 24. Analytics & reporting ✅

Previously, zero analytics existed beyond the widget's own today's-progress
bar (Phase 7). This phase looks *backward* across days/weeks/months — all
six originally-listed deliverables are now built, with "Time Per Project"
delivered as Time Per Category (Phase 25's "Project" concept doesn't exist
yet — the same substitution Phase 23 made for "Habit Tracker," not a new
scoping decision).

**Delivered:**
- Dashboard — a new `AnalyticsWindow`, opened from a widget header icon,
  combining every metric below in one scrollable read-only view
- Weekly/Monthly Progress, Completion Rate — computed from `TaskItem.PlanDate`
  falling in the current week (Sunday-start, matching `WeekViewModel`'s
  convention)/month, and all-time, respectively
- Streak Counter — consecutive days (walking back from today) with at
  least one task completed, the identical algorithm to Phase 21's
  `Goal.GetCurrentStreak`, just over `TaskItem.CompletedAt` instead of a
  `GoalCompletion` log
- Focus Time, Category Analytics, Time Per Category — aggregated from
  Phase 23's `FocusSession` log and `Category`, including a "No Category"
  bucket
- Heat Map — a 12-week grid of color-intensity squares (GitHub-contribution-graph
  style) showing completion density per day
- Weekly Report, Monthly Report — a generated Markdown summary (completed
  vs. still-open tasks, focus time logged) with Copy-to-clipboard and
  Save-as-.md actions, reusing the same `StorageProvider.SaveFilePickerAsync`
  pattern Phase 14's export already established

**A genuine domain-model constraint surfaced while testing:** `TaskItem.CompletedAt`
has a private setter and is only ever stamped with the real `DateTime.UtcNow`
via `TaskItem.Complete()` — a deliberate invariant (you can't complete a
task in the past) — so multi-day streak/heat-map test scenarios needed a
small, clearly-scoped reflection helper to backdate it for test data only;
the production code path never does this.

**Live-verified:** opened the real Dashboard against the actual database —
confirmed all 6 summary tiles, the heat map's color-coded cells, and the
full category breakdown (6 real categories with correct counts/colors/
percentages) render correctly from live data. Report generation itself
(exact Markdown content for a given period) is covered by
`AnalyticsServiceTests`' content assertions rather than a second live
click-through — the interactive session was interrupted by the OS screen
lock partway through, and no attempt was made to work around that.

- `src/DeskTodo.Application/DTOs/{AnalyticsSummary,DailyCompletionCount,CategoryAnalytics}.cs`
- `src/DeskTodo.Application/Services/{IAnalyticsService,AnalyticsService}.cs`
- `src/DeskTodo.Application/Services/IFocusSessionService.cs`/`FocusSessionService.cs`
  (`GetAllSessionsAsync` — keeps `IFocusSessionRepository` behind the
  service layer, matching this codebase's "services depend on services"
  layering)
- `src/DeskTodo.App/ViewModels/{AnalyticsViewModel,AnalyticsTile}.cs`,
  `Views/{AnalyticsWindow.axaml,AnalyticsWindow.axaml.cs}`
- `src/DeskTodo.App/Converters/{CompletionCountToHeatColorConverter,StringNotEmptyConverter,IntEqualsZeroConverter}.cs`
- `src/DeskTodo.App/ViewModels/WidgetViewModel.cs` (`AnalyticsRequested`/`OpenAnalyticsCommand`),
  `Views/WidgetWindow.axaml` (📊 header icon), `Views/WidgetWindow.axaml.cs`
  (`OnAnalyticsRequested`)
- Tests: `AnalyticsServiceTests`, `AnalyticsViewModelTests`

## 25. Organization: projects, workspaces & lists ✅

The wishlist's nine items (Projects, Workspaces, Lists, Folders, Sections,
Smart Lists, Saved Searches, Favorites, Bookmarks) are really three or four
distinct ideas wearing nine names. This phase builds the ones that add
genuine new value at this app's current scale, and explicitly defers the
ones that would require a much larger structural change for little payoff
today — each substitution/deferral is documented below rather than silently
dropped.

**Delivered:**
- **Projects** — a new `Project` entity (name, color, description, archived
  flag): an ongoing, color-coded task container, distinct from `Category`
  (lighter, often built-in) and Phase 21's `Milestone` (a fixed deliverable
  with a target date, not an ongoing bucket). `TaskItem.ProjectId` is a
  plain nullable FK, linked the same way `CategoryId`/`MilestoneId` already
  are — no dedicated link/unlink method. A "Projects" tab was added
  alongside Goals/Milestones in the existing Planner window (add/archive/
  delete, colors cycling through a fixed palette) rather than a new header
  icon/window, matching how Milestones was integrated in Phase 21.
- **Lists** — satisfied by Projects: a Project's linked-tasks collection
  *is* a list. Not a second, separate concept.
- **Favorites** — `TaskItem.IsFavorite` already existed (an earlier phase)
  but had no cross-day "view them all" UI; this phase adds that missing
  piece via the grid's new Favorites Smart List.
- **Bookmarks** — read the same way: `TaskItem.IsPinned` already existed;
  this phase adds the cross-day "Pinned" Smart List that was missing.
- **Smart Lists** — a `GridSmartFilter` enum (Favorites/Pinned/Overdue/Due
  Today/High Priority/No Project) added to the grid view (Phase 20), which
  is the natural home since — unlike the day-scoped widget — it already
  spans every day.
- **Saved Searches** — rather than a second, parallel "saved search" concept
  next to the grid's existing "saved column views" (Phase 20), `GridSavedView`
  was extended with search text/category/project/status/smart-filter fields:
  one named preset now captures both what's *shown* (columns) and what's
  *filtered* (the search bar), since a user thinks of both as "what the
  grid currently looks like."
- The grid also gained its first filter bar at all (search text + status +
  category + project dropdowns) — it previously showed every non-archived
  task with no filtering whatsoever.
- The widget's existing day-scoped filter bar (Phase 11) gained a matching
  Project filter dropdown, alongside the existing Category/Tag ones.
- The full-field task editor (Phase 17) gained a "Project" picker, the same
  shape as its existing "Milestone" picker.

**Deferred (documented, not silently dropped):**
- **Workspaces** — fully separate task-space silos (their own settings,
  tray behavior, DB scoping) are a much larger structural change than this
  single-user, single-database desktop widget currently supports. No
  partial version would be honest, so it's deferred rather than faked.
- **Folders** — nested hierarchy of Projects. The only precedent for
  hierarchy anywhere in this domain is `TaskItem.ParentTaskId`'s single-level
  parent/child, explicitly documented as deliberately *not* a general tree.
  Deferred until flat Projects alone prove insufficient.
- **Sections** — sub-grouping headers within a project's task list. Real UI
  complexity (grouped/collapsible rows, cross-section drag-reorder) for
  comparatively low value at this stage; deferred.

**Live-verified:** created a real "Website Redesign" project through the
Planner window's new Projects tab against the actual database — confirmed
it persisted correctly (`sqlite3` query against `Projects`), rendered with
its color swatch and "No linked tasks" state, and cleaned up afterward.
The widget/grid filter dropdowns and the task editor's Project picker were
not separately click-verified live in this pass (covered instead by
`WidgetViewModelTests`, `GridViewModelTests`, and `TaskEditViewModelTests`'
dedicated Project-filter/Smart-List/Saved-Search test coverage) — session
budget ran low partway through live verification, so remaining surface
area relied on the automated suite rather than further manual clicking.

- `src/DeskTodo.Domain/Entities/Project.cs`, `TaskItem.cs` (`ProjectId`/`Project`)
- `src/DeskTodo.Domain/Exceptions/ProjectNotFoundException.cs`
- `src/DeskTodo.Infrastructure/Data/Configurations/{ProjectConfiguration,TaskItemConfiguration}.cs`,
  migration `20260811205335_AddProjects`
- `src/DeskTodo.Application/Abstractions/IProjectRepository.cs`,
  `src/DeskTodo.Infrastructure/Repositories/ProjectRepository.cs`
- `src/DeskTodo.Application/Services/{IProjectService,ProjectService}.cs`
- `src/DeskTodo.App/ViewModels/{ProjectsViewModel,ProjectRowViewModel,ProjectOption,ProjectFilterOption}.cs`,
  `PlannerViewModel.cs` (Projects tab), `Views/PlannerWindow.axaml` (Projects `TabItem`)
- `src/DeskTodo.App/Converters/BoolToArchivedOpacityConverter.cs`
- `src/DeskTodo.App/ViewModels/TaskEditViewModel.cs`/`Views/TaskEditWindow.axaml`
  (Project picker), `WidgetViewModel.cs`/`Views/WidgetWindow.axaml` (Project filter)
- `src/DeskTodo.App/ViewModels/{GridSmartFilter,TaskGridRowViewModel,GridViewModel}.cs`,
  `Views/GridWindow.axaml` (filter bar), `Application/Settings/GridSavedView.cs`
  (Saved Search fields)
- Tests: `ProjectRepositoryTests`, `ProjectServiceTests`, `ProjectsViewModelTests`,
  plus additions to `WidgetViewModelTests`, `TaskEditViewModelTests`, `GridViewModelTests`

## 26. Reminder enhancements ⬜

Phase 13 built the notification *pipeline* (overdue alerts, daily summary).
This phase extends what triggers a reminder and what happens after it
fires, without touching the underlying `INotificationService` abstraction.

**Deliverables:**
- Recurring Reminder — depends on Phase 19's recurring tasks existing first
- Snooze — dismiss a notification for N minutes/hours and be re-reminded
- Reminder History — a log of past notifications, viewable somewhere (e.g.
  a small history panel)
- Sound Notification — a custom notification sound rather than relying on
  the OS default

**Approach:** Snooze needs the notification itself to carry an action (OS
notification APIs generally support action buttons — `osascript`'s basic
`display notification` used by `MacNotificationService`, Phase 13, does
*not* support actions, so a snooze button would likely require switching
macOS to the richer `UNUserNotificationCenter` API, itself gated on having
a proper app bundle identity — see Phase 16's packaging work as a
prerequisite) or, more simply, a "remind me again in..." option surfaced
in-app rather than in the OS notification itself. Reminder History is a
new small append-only log (could be a lightweight new table, or even just
structured log entries queried back out — doesn't need the durability
guarantees a new EF Core entity implies). Custom sound needs
platform-specific audio playback, which neither `MacNotificationService`
nor `WindowsNotificationService` currently does.

## 27. Theming & appearance ⬜

Phase 12 explicitly deferred full theming ("a themed-resource pass" was
called out as future work when accent color/opacity shipped) — this phase
is that deferred work, now formalized, plus the "later" note's "nicer,
Bootstrap-like UI" polish request.

**Deliverables:**
- Light Theme, Dark Theme, System Theme (follow OS) — a real switchable
  theme, not just the current hardcoded light appearance
- Custom Font Size, Compact Mode, Zoom — density/scale controls
- Animations — transitions for state changes (task complete, list
  reordering, window open/close) that don't currently exist
- Responsive Layout — the widget behaving well across its full resizable
  range, not just its default size
- General UI polish pass ("nice UI like bootstrap" from the "later" notes)
  — consistent spacing/typography/elevation across every window, not a
  specific checkbox so much as a design-and-execute pass across all
  existing Views

**Approach:** This is the single most invasive item across the whole
Extended Roadmap from a *files touched* perspective, not a *new concepts*
one: every hardcoded hex color across `WidgetWindow.axaml`,
`TaskEditWindow.axaml`, `SettingsWindow.axaml`, and `ImportExportWindow.axaml`
needs to become a themed `DynamicResource` reference instead, with
light/dark resource dictionaries defined in `App.axaml` (Avalonia's
`FluentTheme` already supports a `RequestedThemeVariant`/dark mode — the
work is in the app's *own* controls following it, not the framework). Font
size/compact mode/zoom are naturally one underlying "UI scale" concept
applied via a `LayoutTransformControl` or resource-based font-size
scaling — this was considered and explicitly deferred during Phase 12 for
being higher-risk to get right without visual verification; the same
caution applies here, so plan to budget real time for visual QA on
whatever display is available. Animations are mostly Avalonia
`Transitions`/`Animation` XAML additions to existing controls, low
architectural risk, more of a time cost than a design-risk cost.

## 28. Power user tools ⬜

Features for users who want to drive the whole app without touching the
mouse, or who want more forgiving editing (undo/redo).

**Deliverables:**
- Command Palette — a searchable list of every app action (open settings,
  add task, export, jump to a date...), summoned via a shortcut
- Keyboard Shortcuts — app-wide bindings beyond the current per-field
  Enter/Escape (e.g. Ctrl/Cmd+N for new task, Ctrl/Cmd+F for search)
- Undo / Redo — a command-level undo stack (distinct from the existing
  per-field "undo completion" toggle) covering delete/edit/reorder/bulk
  actions
- Clipboard History — a small history of recently copied text, surfaced
  for quick paste into a new task
- Activity Log — a chronological log of actions taken in the app (overlaps
  with Phase 26's Reminder History and Phase 24's analytics — likely one
  shared underlying event log powering all three views)
- Task Templates — listed again here from the Power User section of the
  wishlist; same feature as Phase 17's Task Templates, not a second one

**Approach:** Undo/Redo is the architecturally significant item here — it
implies every mutating `TaskService`/`WidgetViewModel` operation pushes an
invertible command onto a stack, a real pattern shift from the current
"call the service, reload" model used throughout Phases 8–14. This is
worth scoping carefully (e.g. "undo the last single action" vs. a full
multi-level undo stack) before committing to it, since it touches nearly
every existing command. Command Palette and app-wide Keyboard Shortcuts
are more additive: a new overlay window/control listing available
commands (which could literally enumerate the `[RelayCommand]`s already
defined across the ViewModels) plus a `KeyBindings`-style
input-to-command mapping registered at the `Window` level. An Activity
Log as a single shared event-sourcing-lite table would be the most
reusable foundation for Phase 24's analytics, Phase 26's reminder
history, and this phase's activity log simultaneously, if scoped as one
piece of shared infrastructure rather than three separate logs.

## 29. Security & data protection ⬜

The database is currently a plain, unencrypted local SQLite file with no
access control beyond OS file permissions. This phase is about protecting
it, plus the export-format items from the wishlist's Import/Export section
that are really about safe data portability (PDF/HTML/backup formats)
rather than the CSV/JSON/Excel/Markdown already built in Phase 14.

**Deliverables:**
- Database Encryption — encrypt the SQLite file at rest
- Password Lock, PIN Lock, Auto Lock — require a credential to open the
  widget/app, with an idle timeout
- Windows Hello, Touch ID — OS-native biometric unlock as an alternative to
  a password/PIN
- Secure Backup, Backup File, Restore File — a dedicated, versioned backup
  format distinct from a plain task export (should capture Settings and
  Categories too, not just tasks) with a restore flow that doesn't
  silently overwrite existing data without confirmation
- PDF, HTML export — rounding out Phase 14's export formats

**Approach:** SQLite encryption typically means switching the EF Core
SQLite provider to a variant that supports SQLCipher (or an equivalent
encrypted-at-rest extension) — a meaningful dependency change to
`DeskTodo.Infrastructure`'s data layer, ideally decided before too much
more schema work accumulates on the plain provider. Password/PIN lock is
an app-level gate shown before `WidgetWindow` is constructed (a new
"lock screen" View), with the actual credential check either
self-implemented (hashed PIN stored in `AppSettings` or a dedicated
secure-storage API) or, for Windows Hello/Touch ID, OS biometric APIs
following the same per-platform-project interop pattern as Phase 15's
auto-start. Backup/Restore is a natural extension of Phase 14's
`ITaskExportService`/`ITaskImportService` pattern but for a new,
richer format (a manifest of the whole app's persisted state: tasks,
categories, settings — likely a zip containing the SQLite file directly,
or a structured JSON superset of `TaskExportRecord`). PDF export needs
either a PDF-generation library (a new dependency, similar to how
ClosedXML was added for Excel in Phase 14) or rendering via an
intermediate format; HTML export is comparatively simple (a templated
string, similar to the existing Markdown writer).

## 30. Auto-update system ⬜

From the "later" notes directly: *add app version and when a new version
comes, ask the user to update — updating should never delete old/existing
data.*

**Deliverables:**
- Display the current app version somewhere in the UI (e.g. Settings)
- Check for a newer version (against some update feed/endpoint — needs a
  decision on hosting: GitHub Releases' API is a natural zero-infrastructure
  option given this is a git-hosted project)
- Prompt the user when an update is available, with a way to install it
- Guarantee the update process never touches the user's SQLite database or
  `AppSettings` — both already live outside the app's install directory
  (`AppStoragePaths.ResolveDefaultRootDirectory()`, Phase 4/12), which is
  the right foundation for this to already be safe, but the *update
  mechanism itself* still needs to be built and needs to actually replace
  only the application binaries

**Approach:** A new `IUpdateCheckService` (or similar) that periodically
(or on startup) queries a version-feed endpoint and compares against the
running assembly's version, surfacing an "update available" notification
via the existing `INotificationService` (Phase 13) or a dedicated
in-app banner. The actual update mechanism is platform-specific and
non-trivial: on Windows, MSIX packages (Phase 16) get free auto-update
support from the OS itself if distributed through a store or an App
Installer URI, which would mean the *packaging* choice from Phase 16
directly determines how much custom update-installation code needs
writing at all; on macOS, an unsigned/non-MAS `.dmg` distribution
typically means either building a Sparkle-style updater or directing
users to redownload — this needs a decision on distribution channel
(direct download vs. Mac App Store vs. Microsoft Store) before the
implementation approach can even be chosen, making this phase's design
work partly a distribution-strategy decision, not purely an engineering
one.

## 31. Cloud sync & multi-device ⬜

Currently 100% local (SQLite on disk, Phase 4). This phase introduces a
remote component to DeskTodo for the first time — the biggest
architectural departure from everything built so far, and worth flagging
as such rather than understating it.

**Deliverables:**
- Cloud Sync, Multi-Device Sync — the same task data available and kept in
  sync across more than one installation of DeskTodo
- Auto Backup, Restore Backup, Version History — cloud-hosted backups of
  the local database, with the ability to restore a specific point in time
- Offline Sync, Conflict Resolution — the app must remain fully usable with
  no network connection, syncing/reconciling changes once connectivity
  returns

**Approach:** This needs a backend service DeskTodo doesn't have today —
either a self-hosted sync server or a third-party backend-as-a-service —
plus a client-side sync engine that reconciles local SQLite changes
against remote state. `TaskItem`/`Category`/`AppSettings` would all need
either a `LastModifiedAt`/version-vector scheme for conflict detection
(there's already a `ModifiedAt` field on `TaskItem` that a real sync
protocol could build on) or a full CRDT-style approach if true
offline-first multi-writer conflict resolution is required. This is
realistically a multi-phase effort on its own (auth/identity is a
prerequisite — see Phase 32 — before "which account does this data belong
to" even makes sense) and the single largest scope item in this entire
roadmap; recommend treating "Cloud Sync" as its own dedicated planning
exercise before starting implementation, not something to size from this
paragraph alone.

## 32. Team collaboration & sharing ⬜

Depends on Phase 31 existing first (or at least a lighter-weight identity
system) — there's no concept of "another user" anywhere in DeskTodo today.
Also where the "later" notes' team-shaped items land: *send a task to
another user who can accept/reject it; group tasks; user profiles.*

**Deliverables:**
- User Profile concept — an identity for the current user, even in a
  single-player context this might just be "your name/avatar shown in
  Settings," but for anything below it's a real account
- Send Task / Assign Tasks — send a task to another user, who can accept
  or reject it
- Group Tasks, Shared Projects, Shared Tasks — task lists visible to and
  editable by more than one user
- Team Dashboard, Activity Feed, Comments, Mentions, File Sharing,
  Permissions — the collaboration surface once shared data exists

**Approach:** "User Profile" alone (a name/avatar, purely local, no
network) is cheap and could ship independently of everything else in this
phase — a new `UserProfile` concept in `AppSettings` or its own small
entity. Everything past that needs Phase 31's backend/sync
infrastructure and a real account/auth system (which user is "me," how do
they identify "another user" to send a task to) before it's meaningful —
Assign/Send/Group/Shared-anything are all fundamentally "more than one
person's local SQLite database needs to agree on shared state," which is
the same hard problem Phase 31 exists to solve. Comments/Mentions/File
Sharing/Permissions are then additive features *on top of* that shared
data model, each roughly analogous to features already built for the
single-user case (Comments ~ Notes, File Sharing ~ Phase 17's
Attachments, but multi-user) once the underlying sharing model exists.

## 33. Third-party integrations ⬜

Zero integrations exist today — every feature so far is self-contained.
Grouped by the four sub-categories already used in `Later.Implementation.md`.

**Deliverables:**
- Calendar: Google Calendar, Outlook Calendar (two-way sync of due dates as
  calendar events, or at minimum one-way export)
- Project management import: Microsoft To Do, Todoist, Trello, Notion
  (one-time import into DeskTodo's own storage), Jira, Azure DevOps
  (likely ongoing sync given these are typically used alongside a team's
  existing workflow, not migrated away from)
- Development: GitHub Issues, GitLab Issues (import issues as tasks, and/or
  surface assigned issues — overlaps with Phase 36's Developer Mode)
- Communication: Slack, Microsoft Teams, Discord (e.g. create a task from a
  message, or post a daily summary to a channel)
- Cloud storage: OneDrive, Google Drive, Dropbox (as an attachment backend
  for Phase 17, or as a sync transport for Phase 31)

**Approach:** Every one of these is an OAuth-authenticated third-party API
integration — each needs its own client credentials, auth flow (a
system-browser-based OAuth redirect is the standard desktop-app pattern),
and a mapping between the external service's data model and DeskTodo's
`TaskItem`. Realistically each integration is its own small vertical slice
(auth + import/sync logic + a Settings UI section to connect/disconnect
it) that could ship independently of the others, rather than one
monolithic "integrations" effort — recommend picking the single
highest-value one (likely Google/Outlook Calendar or GitHub Issues, based
on how the app is actually being used) and validating the whole
auth-to-sync pipeline once before replicating the pattern across the rest.
The one-time "import" integrations (Todoist/Trello/Notion/MS To Do) are
meaningfully simpler than the ongoing-sync ones (Jira/Azure DevOps/
Calendar) — a one-shot import can reuse Phase 14's
`ITaskImportService` pattern almost directly (parse the external format
into `TaskExportRecord`s), while ongoing sync needs the same
conflict/versioning considerations as Phase 31.

## 34. AI features ⬜

Entirely unbuilt — no AI/LLM integration exists anywhere in DeskTodo today.
All twelve wishlist items in this category depend on the same underlying
capability: sending task/user data to a language model and using the
result.

**Deliverables:**
- AI Task Creation, AI Break Large Tasks (subtask suggestions — depends on
  Phase 17's subtasks existing), AI Priority Suggestions, AI Time
  Estimation, AI Smart Schedule/Reschedule
- AI Daily Planner, AI Weekly Planner — auto-arranging a day/week's tasks
- AI Meeting Summary, AI Note Summary, AI Rewrite Notes — text
  transformation over existing task Notes/Description content
- AI Productivity Coach, AI Goal Suggestions — higher-level, ongoing
  guidance rather than one-shot actions on a single task

**Approach:** Needs a new `IAiAssistantService` abstraction in the
Application layer (kept as an abstraction specifically so the concrete
LLM provider is swappable, the same reasoning behind
`INotificationService`/`ITaskExportService` being interfaces rather than
direct implementations) backed by a real LLM API — this is the first
phase in the whole roadmap needing an external network dependency and API
key/credential management as a hard requirement, not an optional
integration. Each "AI X" feature is a specific prompt-and-parse operation
against that service: e.g. AI Task Creation takes free-form user text and
returns a structured `TaskExportRecord`-shaped suggestion (reusing the
DTO already built in Phase 14) for the user to confirm before it's
actually created; AI Smart Schedule takes the day's task list and due
dates and returns a suggested `DayOrder` sequence. Cost/latency/privacy
are real product considerations here (every call sends task content to a
third-party API) that should be decided — and very likely surfaced as an
explicit opt-in Settings toggle, off by default — before any
implementation work starts, not left implicit.

## 35. Unique capture features ⬜

The wishlist's "Unique Features" section, mostly about *creating* a task
from something other than typing into the add-task box — several depend on
Phase 34's AI service, some don't.

**Deliverables:**
- Smart Clipboard Detection — detect task-shaped text on the clipboard and
  offer to create a task from it
- Screenshot to Task, OCR Image to Task — capture a region of the screen
  (or an existing image) and extract text via OCR to seed a task
- Voice to Task — speech-to-text task creation
- Email to Task — forward or paste an email's content to create a task
- Drag File to Create Task, Drag Browser Tab to Create Task — OS-level drag
  sources dropped onto the widget become a new task
- Smart Daily Briefing, End of Day Summary, Morning Planning Assistant —
  AI-generated (Phase 34) natural-language summaries of the day
- AI Workload Prediction — same dependency

**Approach:** Drag File/Drag Browser Tab is the most self-contained item
here — Avalonia's `DragDrop` API is already used in this project for
in-window drag-to-reorder (Phase 9); accepting an *external* drag (a file
from Finder/Explorer, a URL from a browser) onto `WidgetWindow` is a
natural, incremental extension of that same API, not a new mechanism.
Clipboard Detection needs a clipboard-content poll or hook plus (likely)
Phase 34's AI service to actually parse "task-shaped text" reliably. OCR
needs either a bundled OCR library or an OS-native OCR API (both Windows
and macOS have first-party OCR APIs — Windows.Media.Ocr,
Vision framework on macOS — which would mean per-platform interop again,
following the `Platform.Windows`/`Platform.Mac` split already established).
Voice to Task needs a speech-to-text API (cloud-based or OS-native
dictation). Email to Task needs either an email-forwarding-address
integration (mail server infrastructure DeskTodo doesn't have) or a
simpler "paste email content, we'll parse it" flow that's really just
Smart Clipboard Detection applied to email text specifically. Daily
Briefing/End of Day Summary/Morning Assistant are AI-service consumers
(Phase 34) with no new capture mechanism of their own — they read
existing task data and generate a summary, structurally similar to
Phase 13's existing daily-summary *notification* but as a longer,
AI-generated narrative rather than a one-line count.

## 36. Developer Mode dashboards ⬜

A specific persona's feature set (software developers) layered on top of
Phase 33's GitHub/Jira/Azure DevOps integrations — this phase is really
"once those integrations exist, here's a developer-focused way to surface
them," not new integration work of its own.

**Deliverables:**
- GitHub Dashboard, Azure DevOps Dashboard, Jira Sprint Board — summary
  views of the connected account's assigned work
- Pull Request Reminder, Code Review Reminder — notifications (via the
  existing `INotificationService`, Phase 13) for PRs/reviews waiting on
  the user
- Build Status Widget — a live CI build-status indicator
- Release Tracker, Bug Tracker — views over releases/issues from the
  connected project-tracking integration

**Approach:** Entirely dependent on Phase 33's integrations existing
first — there's nothing to build here without a GitHub/Azure DevOps/Jira
connection already authenticated and syncing. Once that exists, this
phase is mostly UI: a new "Developer Mode" view (opt-in, likely a Settings
toggle that reveals it, given it's not relevant to most users) presenting
the already-synced external data in developer-specific groupings, plus
extending the existing overdue-notification pattern (Phase 13) to also
fire for "PR waiting on your review" style events sourced from the
integration rather than from `TaskItem` due dates.

## 37. Companion apps & extensions ⬜

Everything that means "DeskTodo, but not this desktop app" — separate
codebases/platforms/technology stacks entirely, correctly listed as
"Future Ideas" in the wishlist rather than near-term work.

**Deliverables:**
- Mobile apps (Android, iPhone), Apple Watch app
- Windows Widget, macOS Widget (native OS widget-surface integrations —
  distinct from DeskTodo's own always-visible window, which already
  serves that role today)
- Browser Extension, Chrome Extension, Outlook Add-in, VS Code Extension
- Adjacent product ideas from the same section: Personal Knowledge Base,
  Document Manager, Expense Tracker, Time Billing, Invoice Generator, CRM
  Lite (each of these is closer to "a different product" than "a DeskTodo
  feature" — flagged here for completeness since they're in the source
  wishlist, not because they're recommended)

**Approach:** None of this is buildable on the current .NET/Avalonia
desktop codebase as-is. Mobile apps would mean either .NET MAUI (reusing
the Domain/Application layers, which are already UI-framework-agnostic by
design — a real payoff of the Clean Architecture layering established
since Phase 1) or a fully separate native codebase per platform. Browser
extensions and the VS Code extension are entirely separate
JavaScript/TypeScript codebases with no code-sharing potential with the
current stack beyond talking to the same eventual sync backend (Phase
31) as a remote API. This phase depends the most heavily on Phase 31's
cloud sync existing first, since a companion app with no shared backend is
just a second, disconnected copy of DeskTodo — this is realistically "the
roadmap after this roadmap," worth acknowledging honestly rather than
scoping in detail prematurely.
