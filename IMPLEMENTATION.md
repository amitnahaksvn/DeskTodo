# DeskTodo — Implementation Plan

Tracks phase-by-phase progress on DeskTodo. Updated as each phase completes
or changes scope. For *why* things are built the way they are, see
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — this file is the checklist,
that one is the reasoning.

**Legend:** ✅ Done · 🚧 Partial · ⬜ Not started

**Last updated:** 2026-08-01 (Phases 1–16 done; Extended Roadmap — Phases 17–37 — added, all pending)

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

## Extended Roadmap — Phase 17+ (planned, not started)

| # | Phase | Source category (Later.Implementation.md) | Status |
|---|-------|---------------------------------------------|--------|
| 17 | [Subtasks, checklists, templates & rich content](#17-subtasks-checklists-templates--rich-content) | Core Task Management | ⬜ |
| 18 | [Tags, labels & grouping](#18-tags-labels--grouping) | Core Task Management | ⬜ |
| 19 | [Recurring tasks, dependencies & auto-reschedule](#19-recurring-tasks-dependencies--auto-reschedule) | Core Task Management, "Later" notes | ⬜ |
| 20 | [Excel-style grid view](#20-excel-style-grid-view) | Spreadsheet / Grid View | ⬜ |
| 21 | [Calendar, weekly/monthly/year views & alternate layouts](#21-calendar-weeklymonthlyyear-views--alternate-layouts) | Planning | ⬜ |
| 22 | [System tray, global shortcuts & quick add](#22-system-tray-global-shortcuts--quick-add) | Desktop Features | ⬜ |
| 23 | [Productivity tools: timers, focus & habits](#23-productivity-tools-timers-focus--habits) | Productivity | ⬜ |
| 24 | [Analytics & reporting](#24-analytics--reporting) | Analytics | ⬜ |
| 25 | [Organization: projects, workspaces & lists](#25-organization-projects-workspaces--lists) | Organization | ⬜ |
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

Everything below is **planning only — no code has been written for any of
it.** Each phase lists what it covers, why it's grouped that way, the
concrete deliverables (traced back to `Later.Implementation.md`), and the
architectural approach in prose — new entities, services, or UI surfaces —
without pre-committing to exact class names or file layouts that should be
worked out at implementation time. Phases are ordered roughly by
dependency and by how naturally they build on what already exists, not by
priority — that's a product call for whoever picks this up next.

## 17. Subtasks, checklists, templates & rich content ⬜

Everything about a task having *more structure inside it* than a flat
title/description/notes — the most natural next step after Phase 9's
full-field editor, since it extends the same entity/editor rather than
introducing a new concept.

**Deliverables:**
- Subtasks — a task can have child tasks; completing all children could
  optionally auto-suggest completing the parent
- Checklists — lighter-weight than subtasks: an ordered list of
  check-off-able items living *inside* one task (no due date/priority of
  their own), shown inline in the row or the editor
- Task Templates — save a task's current shape (title pattern, priority,
  category, checklist items, estimated time) as a named template; "New from
  template" pre-fills the add/edit flow
- Rich Text Notes — upgrade `Notes` from plain text to Markdown (or a
  similarly lightweight rich-text format), with a preview/edit toggle in the
  editor
- Attachments — attach one or more files to a task; store either a copied
  file (under the app's data directory, alongside the SQLite database) or a
  reference path, with a size/type cap

**Approach:** Subtasks need either a self-referencing `ParentTaskId` on
`TaskItem` or a separate lightweight `ChecklistItem` entity (cleaner,
since subtasks/checklist items don't need the full `TaskItem` field set —
priority, due date, etc. — most apps that support both keep them as two
distinct concepts for exactly this reason). Templates need a new
`TaskTemplate` entity mirroring `TaskItem`'s editable fields, plus a
repository/service pair and a "save as template"/"create from template"
UI entry point on the existing task row context menu and the full-field
editor. Rich text notes only change how `Notes` is *rendered*, not stored
(still a string, just Markdown-flavored) — needs a Markdown renderer
control or a hand-rolled minimal one, plus deciding whether the widget's
row-level preview shows rendered or raw text. Attachments need a new
`Attachment` entity (task FK, file path, original filename, size, added
date), a documents subfolder under `AppStorageOptions.RootDirectory`, and
UI for attach/preview/remove in the full-field editor — this is the
heaviest item in this phase and could be split out on its own if scope
needs trimming.

## 18. Tags, labels & grouping ⬜

Task Management's fields kept separate from Categories intentionally in
the wishlist (Categories = one per task, exclusive; Tags = many per task,
freeform) — this phase adds that second, orthogonal classification axis and
the list-view features that make it useful.

**Deliverables:**
- Tags/Labels — free-form, multi-valued, user-created (functionally the
  same concept under two names in the wishlist; ship as one feature)
- Group By — group `VisibleTasks` under headers (by category, priority,
  tag, or due date) instead of one flat list
- Favorite Tasks — a second boolean flag distinct from Pin (e.g. Pin =
  "keep at the top of today," Favorite = "important across every day," or
  whatever distinction the product actually wants — needs a product
  decision, not just an engineering one)
- Task Color — a per-task custom color override, independent of category
  color, exposed in the full-field editor
- Recently Viewed — a short-lived list of the last N tasks opened in the
  full-field editor, shown somewhere in the search/filter bar

**Approach:** Tags need a many-to-many relationship — a `Tag` entity plus a
join table (`TaskTag`), unlike the existing one-to-many `Category`
relationship. The search/filter bar (Phase 11's `WidgetViewModel`) gains a
tag-filter alongside the existing status/category filters, and the editor
gains a tag-picker/tag-creator control. Group By changes `RefreshVisibleTasks`
from producing a flat `ObservableCollection<TaskItemViewModel>` to
something that can represent group headers — either a heterogeneous
collection with header/item marker types, or a nested
`ObservableCollection<GroupViewModel>` bound via an Avalonia
`ItemsControl` with grouped `DataTemplate`s. Recently Viewed needs a
small in-memory (or lightly persisted) ring buffer tracked wherever the
full-field editor is opened from.

## 19. Recurring tasks, dependencies & auto-reschedule ⬜

Three related "a task's lifecycle isn't just create → complete" features,
including the explicit "later" note: *alert when a task isn't done within
its timeframe, then move it to the next/a different day.*

**Deliverables:**
- Recurring Tasks — daily/weekly/monthly/custom recurrence rules; when the
  latest occurrence is completed (or its day passes), the next occurrence
  is created automatically
- Task Dependencies — task B can be marked blocked-by task A; the UI
  should make a blocked task visually distinct and prevent (or at least
  warn on) completing it before its blocker
- Auto-reschedule overdue tasks — building on Phase 13's overdue-alert
  notification: after alerting, offer (or automatically perform, per a
  Settings toggle) moving an incomplete overdue task to today or the next
  day, rather than leaving it silently stuck on a past date forever

**Approach:** Recurrence needs a `RecurrenceRule` on `TaskItem` (or a
separate `RecurrenceRule` entity referenced by it) capturing frequency,
interval, and an optional end condition, plus a background check —
reusing `WidgetViewModel`'s existing 30-second poll timer pattern from
Phase 10/13 — that generates the next occurrence's `TaskItem` row when due.
Dependencies need a `TaskDependency` join entity (blocking task ID, blocked
task ID) and a guard in `TaskService.CompleteTaskAsync` that checks for
incomplete blockers. Auto-reschedule is the most self-contained of the
three: extend the existing overdue-check in `WidgetViewModel` (Phase 13)
to, after notifying, either prompt via a small confirmation or (if a new
"Auto-reschedule overdue tasks" Settings toggle is on) directly update the
task's `PlanDate` and persist — no new entities needed, just new logic in
an already-existing method.

## 20. Excel-style grid view ⬜

The single largest item in the whole wishlist, and notably the one closest
to this project's *original* pre-pivot concept (the "DesktopSheet"
Excel-overlay idea from before the DeskTodo pivot) — worth calling out
explicitly since it's effectively bringing that original direction back as
an alternate view, not the primary one this time.

**Deliverables:**
- An Excel-like grid as an alternate view of the task list (likely opened
  from a new window/tab, not replacing the compact widget)
- Inline cell editing, multi-row selection, bulk update across selected rows
- Copy/paste rows within the grid, and copy/paste to and from real Excel
  (tab-separated clipboard interop — Excel's copy format is TSV-based, not
  the app's own CSV format)
- Custom, hideable, freezable, resizable, reorderable columns; saved
  column-layout "views"
- Dedicated Progress and Status columns (computed/derived, not raw fields)

**Approach:** This needs a real data-grid control — Avalonia doesn't ship
one in the base `Avalonia.Controls` set used so far in this project; either
a community grid package or a custom `ItemsControl`-based grid would be a
prerequisite decision. The underlying data is the same `TaskItem`s already
modeled — this is a new *View* (and a fairly involved `GridViewModel`
sitting alongside `WidgetViewModel`), not new Domain/Application-layer
work, aside from possibly a "saved view" concept (column layout + filter
state, persisted similarly to `AppSettings`). Clipboard interop with real
Excel needs Avalonia's clipboard API read/written in TSV, separate from
the CSV/JSON import/export already built in Phase 14 (different format,
different transport — clipboard vs. file).

## 21. Calendar, weekly/monthly/year views & alternate layouts ⬜

The widget only ever shows one day at a time (by design — Phase 10's daily
planner). This phase is about *viewing more than one day at once*, in
several different shapes.

**Deliverables:**
- Calendar View — a real month-grid calendar (not just the existing
  jump-to-a-date picker from Phase 10), with a completion/task-count
  indicator per day
- Weekly Planner, Monthly Planner, Year Planner — dedicated views scoped to
  those time ranges
- Agenda View — a scrollable list of upcoming tasks across many days,
  grouped by date (distinct from the grid's flat single-day list)
- Timeline View — tasks plotted against a time axis (likely by due
  date/estimated time, for the tasks that have one)
- Kanban Board — tasks as cards in status-based columns (e.g. To Do /
  In Progress / Done — needs a status concept beyond the current
  boolean `IsCompleted`, or reuses it as a two-column board)
- Eisenhower Matrix — a 2×2 urgent/important grid, derived from
  priority + due-date proximity
- Goal Planner, Milestones, Sprint Planner, Roadmap View — longer-horizon
  planning views, likely needing a new higher-level "Goal"/"Milestone"
  concept that tasks can optionally link to

**Approach:** All of these are read/organize *views* over the same
`TaskItem` data (via `ITaskService.GetAllTasksAsync`, already built for
Phase 14's export), not new persistence beyond Kanban's status concept and
Goals/Milestones' new entity. Each is realistically its own window or a
tab within a larger "Planner" window, since the compact widget's whole
design premise (small, unobtrusive, one day) doesn't fit a month grid or a
Kanban board — this phase implies growing DeskTodo a second, larger window
alongside the widget, not stretching the widget itself. Goal
Planner/Milestones/Sprint Planner/Roadmap View are the most speculative
items here and probably deserve their own scoping discussion before
committing to an entity design — "Goal" means different things in
different apps (a personal habit-style goal vs. a project-management
milestone).

## 22. System tray, global shortcuts & quick add ⬜

The widget currently only exists as its own window — there's no tray icon,
no way to summon it via a keyboard shortcut, and no lightweight "just add a
task from anywhere" flow. This phase is about *ambient* access to the app.

**Deliverables:**
- System Tray icon (Windows) with a context menu (show/hide widget, quick
  add, settings, quit)
- macOS Menu Bar item (the macOS equivalent of the tray)
- Minimize to Tray — closing the widget's window hides it to the tray
  instead of exiting the process
- Global Shortcut — a systemwide hotkey (works even when DeskTodo isn't
  focused) to show/hide the widget or open Quick Add
- Quick Add Window — a small, separate, fast-to-summon window for typing a
  new task without bringing the full widget to the front
- Mini Widget — a further-collapsed display mode (e.g. just today's
  progress ring/count, no task list) for users who want minimal desktop
  footprint
- Multi Monitor Support — an explicit "which monitor" placement option in
  Settings, rather than relying on default window placement

**Approach:** Avalonia has a `TrayIcon` API (`Avalonia.Controls.TrayIcon`)
usable cross-platform for the tray/menu-bar piece — this is the most
"just wire it up" item in this phase. Global shortcuts need OS-level
interop, similar in spirit to Phase 15's auto-start work: a Win32
`RegisterHotKey` call on Windows, an `NSEvent` global monitor (or the
`Carbon` `RegisterEventHotKey` API) on macOS — same
`IGlobalHotkeyService` abstraction / per-platform-project pattern already
established for `INotificationService`/`IAutoStartService`. Quick Add is a
new small window + `QuickAddViewModel` (title + maybe priority/category,
intentionally NOT the full editor) that creates a task and closes itself.
Mini Widget is a new `WidgetUiScale`-like Settings toggle plus a
significantly trimmed alternate `WidgetWindow` layout (or a runtime
show/hide of most of the existing XAML). Multi-monitor placement needs
`Screens` enumeration (Avalonia exposes this via `TopLevel.Screens`) and a
monitor-identity value persisted alongside the existing `WindowLeft/Top`
in `AppSettings`.

## 23. Productivity tools: timers, focus & habits ⬜

None of this exists yet — DeskTodo currently tracks *what* to do and
*whether* it's done, not time spent doing it or supporting habits/focus
routines around it.

**Deliverables:**
- Pomodoro Timer, Stopwatch, Focus Timer, Focus Mode, Deep Work Session —
  a family of timed-work-session features, likely sharing one underlying
  timer engine with different presets/behaviors
- Break Reminder, Water Reminder, Stretch Reminder — periodic
  wellness nudges, delivered via the existing `INotificationService`
  (Phase 13) on a configurable interval
- Daily/Weekly/Monthly Goals, Habit Tracker — recurring, checkable
  commitments distinct from one-off tasks (a habit is "do this every day,"
  tracked via a streak, not a single due date)
- Time Tracking, Actual Time — start/stop a timer against a specific task
  and record elapsed time into the `ActualMinutes` field that already
  exists on `TaskItem` but has no UI (see `Later.Implementation.md`)
- Productivity Score — a computed metric from completion rate, time
  tracked vs. estimated, and/or streaks

**Approach:** A shared `ISessionTimerService` (start/pause/stop/tick,
optionally OS-backed for accuracy while the app isn't focused) could back
Pomodoro/Stopwatch/Focus/Deep Work as different presets of the same
underlying primitive, with a small floating timer window or a
`WidgetWindow` header indicator while a session is running. Break/Water/
Stretch reminders are the simplest item here — a new recurring-interval
setting plus `INotificationService.NotifyAsync` calls, no new UI beyond a
Settings section. Habit Tracker needs a genuinely new `Habit` entity
(distinct from `TaskItem` — a habit doesn't have a single due date, it has
a recurrence + a streak count) and its own small CRUD surface, likely
reusing patterns from `TaskService`/`TaskItemViewModel` rather than
literally extending them. Time Tracking wires a timer session's elapsed
time into `TaskItem.ActualMinutes` via the existing `UpdateTaskAsync`.
Productivity Score is a derived, computed value (a new method on
`ITaskService` or a small dedicated analytics service) — see Phase 24,
since it overlaps heavily with Analytics.

## 24. Analytics & reporting ⬜

Zero analytics exist beyond the widget's own today's-progress bar (Phase
7). This phase is about looking *backward* across days/weeks/months,
which the current day-scoped `WidgetViewModel` architecture doesn't do at
all.

**Deliverables:**
- Dashboard — a summary view combining several of the metrics below
- Weekly/Monthly Progress, Completion Rate, Streak Counter
- Focus Time, Time Per Project, Category Analytics (all depend on Phase 23's
  time tracking and/or Phase 18's tags or a "Project" concept from Phase 25)
- Heat Map — a calendar-style heat map of completion density per day
- Weekly Report, Monthly Report — a generated summary, potentially
  exportable via the existing Phase 14 export pipeline (Markdown/PDF would
  be natural formats for a "report")

**Approach:** Needs a new `IAnalyticsService` (or similar) in the
Application layer that aggregates over `ITaskService.GetAllTasksAsync`
(already built) — most of these are read-only computations over existing
data, not new persistence, with the exception of Streak Counter (which
either recomputes from history each time or maintains a running counter
incrementally) and anything depending on Time Tracking (Phase 23) or
Projects (Phase 25) not existing yet. The Dashboard is a new, dedicated
window (not the compact widget) with charts/summary tiles — this is a
natural candidate to reuse whatever charting approach gets picked, if any
(Avalonia doesn't ship a charting control; a lightweight custom-drawn
summary using `DrawingContext` may be more appropriate than a full
charting library dependency, or the artifact/dataviz conventions used
elsewhere).

## 25. Organization: projects, workspaces & lists ⬜

A second, coarser grouping concept above Category/Tags — the wishlist
treats "a Category on a task" and "a Project/Workspace that contains many
tasks" as different things, and this phase is exactly that distinction.

**Deliverables:**
- Projects, Lists, Folders, Sections — nested/flat containers for grouping
  many tasks (needs a product decision on how many of these four
  near-synonymous concepts actually ship as distinct features vs.
  collapsing into one)
- Workspaces — a higher-level container potentially holding multiple
  Projects/Lists (most relevant once Phase 32's multi-user features exist —
  low priority standalone)
- Smart Lists — a saved filter/query (e.g. "overdue," "due this week")
  presented as if it were a static list
- Saved Searches, Bookmarks — persisted versions of an ad-hoc search
  (Phase 11) or a specific task, for quick return access

**Approach:** A new `Project` (or `List`) entity that `TaskItem` gets an
optional FK to, parallel to the existing `CategoryId` — the
`WidgetViewModel`/search-and-filter surface (Phase 11) would need a
project-scoped view mode alongside the existing day-scoped one, which is
a meaningful architectural shift (the whole app is currently organized
around "today's tasks," not "this project's tasks"). Smart Lists and Saved
Searches are lighter: persist a filter-criteria object (status + category +
tag + search text, mirroring `WidgetViewModel`'s existing filter
properties from Phase 11) under a user-given name, and re-apply it on
selection — no new Domain entities needed, just a new small persisted
list, similar in spirit to how `AppSettings` is persisted today.

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
