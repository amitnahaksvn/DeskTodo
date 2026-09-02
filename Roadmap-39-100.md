# Desktop Productivity App — Features 39–100 Detailed Development Specification

## Purpose

This document defines the detailed development scope for features **39 through 100** of the desktop productivity application.

The existing application already contains (Phases 1–38, see `IMPLEMENTATION.md`):

- Solution scaffold
- Domain model
- Application/repository layer
- EF Core + SQLite persistence
- DI and migrations
- Persistence tests
- Task CRUD
- Drag/reorder
- Daily planner
- Calendar navigation
- Search/filter/sort/multi-select
- Settings
- Notifications
- Import/export
- Platform integration
- Packaging
- Subtasks/checklists/templates/rich content
- Tags/labels/grouping
- Recurring tasks/dependencies/auto-reschedule
- Excel-style grid
- Calendar layouts
- System tray/global shortcuts/quick add
- Productivity timers/focus/habits
- Analytics/reporting
- Projects/workspaces/lists
- Reminder enhancements
- Theming
- Power-user tools
- Security/data protection
- Auto-update
- Team collaboration
- Unique capture
- Task Groups

The features below therefore intentionally focus on capabilities that are not already represented by those phases.

**Status:** planning only, except where noted — nothing in this document was implemented as of when it was written. See `IMPLEMENTATION.md`'s roadmap table for a one-line link to this file and its live status. **Features 39 (Task Inbox), 42 (Task History), 43 (Undo/Redo), 44 (Task Versioning), 45 (Archive Vault), 46 (Trash), 47 (Smart Duplicate Detection), 61 (Activity Timeline), 65 (Work Session History), 67 (Local Backup Manager), 68 (Backup Restore Simulator), 69 (Database Maintenance Center) and 70 (Data Integrity Checker) have since been delivered — see each feature's own section below for what shipped.**

---

# Master Feature List

| # | Feature | Primary Area | Priority |
|---:|---|---|---|
| 39 | Task Inbox / Capture Queue | Capture | High |
| 40 | Command Palette | Power User | High |
| 41 | Natural Language Quick Add | Task Creation | High |
| 42 | Task History / Audit Timeline | Data / Audit | High |
| 43 | Undo/Redo Engine | Core Platform | High |
| 44 | Task Versioning | Data / Recovery | Medium |
| 45 | Archive Vault | Data Management | Medium |
| 46 | Trash / Recovery Center | Data Management | High |
| 47 | Smart Duplicate Detection | Task Intelligence | Medium |
| 48 | Task Relationships Graph | Visualization | Medium |
| 49 | Goal → Project → Task Mapping | Planning | High |
| 50 | Milestone Tracking | Project Management | High |
| 51 | Project Health Score | Analytics | High |
| 52 | Deadline Risk Detection | Planning | High |
| 53 | Workload Heatmap | Planning | High |
| 54 | Capacity Planning | Planning | High |
| 55 | Time Estimation Accuracy | Analytics | Medium |
| 56 | Task Cost Tracking | Analytics | Medium |
| 57 | Decision Log | Project Management | Medium |
| 58 | Meeting Mode | Meetings | Medium |
| 59 | Meeting Action Extractor | Meetings | Medium |
| 60 | Daily Journal | Personal Productivity | Medium |
| 61 | Activity Timeline | Analytics | High |
| 62 | Achievement / Progress System | Motivation | Low |
| 63 | Focus Contexts | Organization | Medium |
| 64 | Distraction Log | Productivity | Medium |
| 65 | Work Session History | Productivity | High |
| 66 | Offline-first Conflict Resolver | Sync Foundation | High |
| 67 | Local Backup Manager | Reliability | Critical |
| 68 | Backup Restore Simulator | Reliability | High |
| 69 | Database Maintenance Center | Developer/Admin | Medium |
| 70 | Data Integrity Checker | Reliability | High |
| 71 | Portable Workspace | Storage | Medium |
| 72 | Multiple Profiles | Organization | Medium |
| 73 | Guest / Presentation Mode | Privacy | Medium |
| 74 | Lock Specific Workspace | Security | High |
| 75 | Privacy Mode | Privacy | High |
| 76 | Sensitive Data Detector | Security | Medium |
| 77 | Keyboard Shortcut Manager | Power User | High |
| 78 | Mouse Gesture Manager | Power User | Low |
| 79 | Macro / Automation Recorder | Automation | Medium |
| 80 | Custom Fields Builder | Customization | High |
| 81 | Custom Task Types | Customization | High |
| 82 | Custom Status Workflow | Workflow | High |
| 83 | Saved Views | Customization | High |
| 84 | View Sharing Templates | Configuration | Medium |
| 85 | Workspace Templates | Templates | High |
| 86 | Project Starter Kits | Templates | High |
| 87 | Recurring Project Templates | Templates | Medium |
| 88 | Bulk Edit Rules | Power User | High |
| 89 | Mass Import Wizard | Data Migration | High |
| 90 | Data Migration Center | Data Migration | High |
| 91 | Export Profiles | Data Export | Medium |
| 92 | Print Layout Designer | Reporting | Medium |
| 93 | Custom Dashboard Builder | UI / Analytics | High |
| 94 | Widget Marketplace Architecture | Platform | Medium |
| 95 | Plugin SDK | Platform | Future |
| 96 | Webhook Engine | Integrations | High |
| 97 | Local REST API | Developer Platform | High |
| 98 | Event Bus / Extension Events | Core Platform | Critical |
| 99 | CLI Tool | Developer Platform | Medium |
| 100 | Developer API Explorer | Developer Platform | Medium |

---

# Architectural Principles

## 1. Do not implement every feature as an isolated module

Several features should be implemented as shared platform capabilities.

The major shared foundations are:

1. Event Bus
2. Command/Undo system
3. Audit/history infrastructure
4. Search/indexing
5. Workflow engine
6. Backup engine
7. Import/export framework
8. Template engine
9. Plugin/widget architecture
10. Local API
11. Automation engine

These shared foundations will reduce duplication across later features.

---

# Recommended High-Level Architecture

```text
+--------------------------------------------------------------+
|                         Desktop UI                            |
|                                                                |
| Tasks | Projects | Calendar | Dashboard | Reports | Settings  |
+-----------------------------+----------------------------------+
                              |
+-----------------------------v----------------------------------+
|                    Application Layer                           |
|                                                                |
| TaskService | ProjectService | GoalService | ImportService     |
| ExportService | BackupService | WorkflowService                |
+-----------------------------+----------------------------------+
                              |
+-----------------------------v----------------------------------+
|                       Core Platform                             |
|                                                                |
| Event Bus | Command Bus | Undo/Redo | Audit | Search           |
| Workflow Engine | Automation | Template Engine                 |
| Plugin Runtime | Permission System                              |
+-----------------------------+----------------------------------+
                              |
+-----------------------------v----------------------------------+
|                    Infrastructure Layer                         |
|                                                                |
| EF Core | SQLite | File Storage | Backup Storage                |
| Local REST API | Webhooks | OS Integration                      |
+--------------------------------------------------------------+
```

---

# Feature 39 — Task Inbox / Capture Queue ✅ Delivered (2026-09-01)

## Objective

Provide a temporary place for thoughts, ideas, tasks, reminders, links, or notes that have not yet been organized.

**Delivered.** A new `InboxItem` entity (`Content`, `Status` — Unprocessed/Converted/Archived —
`CreatedAt`/`ProcessedAt`, `ConvertedTaskId`) plus `IInboxService` (Capture/GetUnprocessed/
ConvertToTask/Archive/Delete). "Convert to Task" creates a plain task on a given day and marks
the item Converted; add due date/priority/tags/project happen afterward through the normal
full-field editor rather than being duplicated in the Inbox itself. Reachable via a new
`InboxWindow` (capture box + queue, each item's Convert/Archive/Delete), from the tray menu
("Inbox…") and Command Palette.

**Deliberately not built:** URL/voice capture detection, Merge, Move to List, and a `Source`/
`Metadata` field — this pass is plain-text capture only, matching the spec's own "Plain text"
as the one universally-needed capture type; the richer capture kinds can layer on later without
changing `InboxItem`'s shape.

**Verified:** real EF Core/SQLite repository tests (unprocessed-queue ordering, status
transitions, the `SetNull` FK to a hard-deleted task) plus mocked service-level tests for
capture/convert/archive/delete.

The Inbox is intentionally different from a normal task list.

A normal task means:

> "I have already decided where and how this should be managed."

An Inbox item means:

> "I need to capture this now and organize it later."

## Functional Requirements

### Capture

Users should be able to capture:

- Plain text
- Task-like text
- URLs
- Notes
- Voice/transcribed text if supported later
- Quick snippets
- Imported items

### Inbox actions

Every Inbox item should support:

- Convert to Task
- Add to Project
- Add due date
- Add priority
- Add tags
- Move to list
- Schedule
- Merge
- Archive
- Delete

## Data Model

Possible entity:

```text
InboxItem
---------
Id
Content
CreatedAt
UpdatedAt
ProcessedAt
Status
Source
ConvertedTaskId
Metadata
```

Status:

```text
Unprocessed
Processing
Converted
Archived
Deleted
```

## UI

Recommended UI:

```text
Inbox
------------------------------------------------
+ Capture something...
------------------------------------------------
Today
[ ] Call client about deployment
[ ] Buy replacement cable
[ ] Research Redis caching

Right click / ...
Convert to Task
Schedule
Move
Archive
Delete
```

## Acceptance Criteria

- Capture should require minimal interaction.
- Inbox items should not automatically become normal tasks.
- Conversion should preserve original capture information.
- Processed items should remain traceable.

---

# Feature 40 — Command Palette

## Objective

Provide a universal keyboard-driven command system.

Shortcut:

```text
Windows/Linux: Ctrl + K
macOS: Cmd + K
```

> **Note:** DeskTodo already has a Command Palette (Phase 28, Cmd/Ctrl+K), wrapping `WidgetViewModel`'s own commands. This entry describes a more general, registry-based version — see "Architecture" below for the gap between what exists today and what this describes.

## Commands

Examples:

```text
Create Task
Search Tasks
Open Inbox
Open Calendar
Open Project
Start Focus Session
Export Tasks
Import Data
Open Settings
Toggle Privacy Mode
Lock Workspace
Open Dashboard
Backup Now
Check Database
```

## Architecture

Create a central command registry.

```text
IAppCommand
    Name
    Description
    Shortcut
    Category
    CanExecute()
    Execute()
```

Each module registers commands.

This prevents the Command Palette from becoming a hardcoded list.

## Search

Search should match:

- Command name
- Keywords
- Shortcut
- Category

Example:

Typing:

```text
backup
```

returns:

```text
Backup Now
Open Backup Manager
Verify Latest Backup
```

## Acceptance Criteria

- Keyboard-only operation.
- Fuzzy search.
- Recent commands.
- Command categories.
- Shortcut display.
- Disabled commands should explain why they are unavailable.

---

# Feature 41 — Natural Language Quick Add

## Objective

Allow users to create tasks using natural text.

Example:

```text
Call Rahul tomorrow at 4pm for 30 minutes
```

Should become approximately:

```text
Title: Call Rahul
Date: Tomorrow
Time: 16:00
Duration: 30 minutes
```

## Important Architecture

Do not couple the task service directly to an AI provider.

Create:

```text
IQuickAddParser
```

Possible implementations:

```text
RuleBasedQuickAddParser
AIQuickAddParser
```

Both return:

```text
TaskDraft
```

Example:

```text
TaskDraft
---------
Title
Description
DueDate
StartTime
Duration
Priority
Project
Tags
TaskType
```

## Initial implementation

Start with deterministic parsing:

- today
- tomorrow
- yesterday
- weekdays
- dates
- times
- duration
- priority keywords
- project syntax
- tag syntax

Example:

```text
Prepare release notes tomorrow 5pm #release @ProjectA
```

## Future AI implementation

Later the AI layer can convert:

```text
Need to get the API ready before Friday and ask John to review it
```

into a structured TaskDraft.

The TaskService remains unchanged.

---

# Feature 42 — Task History / Audit Timeline ✅ Delivered (2026-08-27)

## Objective

Track important changes to tasks.

**Delivered.** A new `TaskHistory` entity (`Id`, `TaskId`, `Action`, `FieldName`, `OldValue`, `NewValue`, `Timestamp`) records a fixed set of task actions — Created, Renamed, Updated (field-level diffs from the general editor's save), Completed, Reopened, Archived, Restored, Deleted. `TaskId` is optional with a `SetNull` foreign key (the same pattern as `FocusSession.TaskId`, Phase 23), so a task's audit trail survives that task being permanently removed via Feature 46's "Delete Forever"/"Empty Trash" — it just becomes unreachable from the UI at that point, since there's no surviving task to open a history view from. Reachable via a new "History" button in the task editor (`TaskEditWindow`), opening a read-only `TaskHistoryWindow`.

**Deliberately scoped down from the spec above:** no `Source`/`Actor`/`Metadata` fields (single-user desktop app — no "who" to distinguish). High-frequency, low-signal actions — Pin/Unpin/Favorite/Unfavorite/Snooze/AddActualMinutes, and the bulk "reschedule overdue tasks" sweep — are deliberately **not** recorded, to keep the timeline readable rather than flooded; this mirrors Feature 46's own documented scope cut (skipping auto-purge retention). The general editor's field-diffing only watches a curated subset (Title, Description, Priority, DueDate, PlanDate, CategoryId), not every property on `TaskItem`.

**Verified:** a real EF Core/SQLite integration test confirms the `SetNull`-on-hard-delete behavior specifically (a history entry survives its task's permanent deletion, with `TaskId` set to null), plus repository-, service-, and ViewModel-level tests for recording and reading history. 586/586 tests passing, zero-warning build. Live-verified non-interactively only (app launch + schema query confirming the migration applied to the real database + clean log) — same safety rationale as Feature 46, recorded in `IMPLEMENTATION.md`.

Example:

```text
Task created
10:32 AM

Priority changed
Normal → High

Due date changed
Aug 25 → Aug 27

Status changed
In Progress → Completed
```

## Data Model

```text
TaskHistory
-----------
Id
TaskId
Action
FieldName
OldValue
NewValue
Timestamp
Source
Actor
Metadata
```

## Actions

Examples:

```text
Created
Updated
Completed
Reopened
Moved
Deleted
Restored
Archived
PriorityChanged
DueDateChanged
StatusChanged
```

## UI

Timeline should show:

- Timestamp
- Action
- Old value
- New value
- Source
- User/profile where applicable

## Requirements

History should normally be append-only.

Do not allow ordinary users to edit audit records.

---

# Feature 43 — Undo / Redo Engine ✅ Delivered (2026-09-01)

## Objective

Provide application-wide reversible operations.

> **Note:** DeskTodo's own Phase 28 planning notes already flagged this as "the architecturally significant item" in that phase — a real pattern shift from the "call the service, reload" model used throughout the app since Phase 8. This entry is the detailed design for that same deferred item, not a new idea.

**Delivered, deliberately scoped down from the full Command-pattern architecture below.**
Rather than an `ICommand` object per mutation type (`CreateTaskCommand`/`UpdateTaskCommand`/etc.),
a single `IUndoRedoService` (`src/DeskTodo.Application/Services/{IUndoRedoService,UndoRedoService}.cs`)
holds a bounded (50-entry) stack of `(description, undo delegate, redo delegate)` tuples —
`TaskItemViewModel`'s own row commands (`ToggleCompleteAsync`, `CommitEditAsync` (rename),
`TogglePinAsync`, `DeleteAsync`) each record a pair of closures over `ITaskService` calls after
their action succeeds. `WidgetViewModel` exposes `UndoCommand`/`RedoCommand` (Cmd/Ctrl+Z,
Cmd/Ctrl+Shift+Z, and Command Palette entries), reloading today's list after either runs so
every row reflects the new state uniformly rather than each command patching its own row.

**Deliberately not built:** per-command classes, and undo/redo for every mutation in the app
(bulk actions, grid edits, settings, archive/duplicate) — scoped to the four actions above,
matching the same cut Phase 11 and Phase 28 already called out when they first deferred general
undo/redo. Undoing a completion that triggered a recurring task's next occurrence
(`TaskService.CompleteTaskAsync`) does not remove that occurrence — a narrow, documented
limitation. Bulk-operations-as-one-undo-entry (this file's own "Important" note above) isn't
implemented, since bulk actions aren't covered at all in this pass.

**Verified:** `UndoRedoServiceTests` (pure unit — LIFO order, redo-stack-clears-on-new-record,
state-changed event) plus the underlying `ITaskService` calls each undo/redo delegate closes
over are already covered by `TaskServiceTests`. 619 tests passing (2 pre-existing macOS-only
failures unrelated to this feature — this sandbox has no real macOS Carbon APIs), zero-warning
build.

## Architecture

Use the Command pattern.

```text
ICommand
---------
Execute()
Undo()
```

Examples:

```text
CreateTaskCommand
UpdateTaskCommand
DeleteTaskCommand
MoveTaskCommand
CompleteTaskCommand
BulkUpdateCommand
```

## Command stacks

```text
Undo Stack
    |
    +-- Update Task
    +-- Move Task
    +-- Change Priority

Redo Stack
```

When a new command executes:

```text
UndoStack.Push(command)
RedoStack.Clear()
```

Undo:

```text
command.Undo()
RedoStack.Push(command)
```

## Important

Bulk operations should be treated as one logical command where possible.

Example:

Selecting 100 tasks and changing their priority should be undone with one action, not 100 separate Undo operations.

---

# Feature 44 — Task Versioning ✅ Delivered (2026-09-01)

## Objective

Allow users to see and restore previous task versions.

**Delivered.** A new `TaskVersion` entity (`Id`, `TaskId` (nullable, `SetNull` on hard delete —
same survives-permanent-deletion pattern as `TaskHistory.TaskId`), `VersionNumber`, and a
structured column per field (`Title`/`Description`/`Priority`/`CategoryId`/`DueDate`/`Notes`/
`ColorHex`/`EstimatedMinutes`) rather than a normalized JSON blob — a structured table, per this
spec's own "Snapshot can be normalized JSON or a structured version table" note, keeps a version
row queryable/diffable without a deserialize step. `TaskService.UpdateTaskAsync` and
`RenameTaskAsync` each capture the task's *pre-edit* state as a new version before applying the
change; `RestoreTaskVersionAsync` overwrites the live task's fields from a chosen version and
captures the pre-restore state as one more version first, so a restore is never a one-way trip.
Reachable via a new "Versions" button in the task editor (`TaskEditWindow`, next to Feature 42's
"History"), opening a `TaskVersionWindow` with a "Restore" action per row.

**Deliberately not built:** `CreatedBy`/`ChangeReason` fields (single-user desktop app — no
"who," and a reason field would just sit empty), and the "Compare Versions" diff view from this
feature's own UI sketch — a version's raw field values are shown, not a before/after diff
against another chosen version.

**Verified:** a real EF Core/SQLite integration test confirms the same `SetNull`-on-hard-delete
survival behavior Feature 42 verified for `TaskHistory`, plus repository- and service-level
tests for capture-on-edit, capture-on-rename, restore-overwrites-fields, and
restore-captures-the-pre-restore-state-too. 619 tests passing, zero-warning build.

## Difference from History

History:

> "What changed?"

Versioning:

> "What did the complete task look like at that point?"

## Data

```text
TaskVersion
-----------
Id
TaskId
VersionNumber
Snapshot
CreatedAt
CreatedBy
ChangeReason
```

Snapshot can be normalized JSON or a structured version table.

## UI

```text
Task Versions

v8 Current
v7 Aug 19 21:20
v6 Aug 18 10:12
v5 Aug 15 14:30
```

Actions:

- View
- Compare
- Restore

Restoring should create a new version rather than deleting history.

---

# Feature 45 — Archive Vault ✅ Delivered (2026-09-01)

## Objective

Provide long-term storage for completed or inactive content.

**Delivered.** A new `ArchiveWindow` unifies the two archive flags this app already had
(`TaskItem.IsArchived` since Phase 8/25, `Project.IsArchived` since Phase 25) into one searchable
view with Restore per row — closing the same "nothing could ever look at what's archived again"
gap Feature 46 closed for Trash. `ITaskService.GetArchivedTasksAsync` is the one new method
(a thin delegate to the already-existing `ITaskRepository.GetArchivedAsync`); Projects needed no
new repository method since `IProjectService.GetProjectsAsync` already returns every project and
`SetArchivedAsync` already handles unarchiving. Reachable from the tray menu ("Archive Vault…")
and Command Palette.

**Deliberately not built:** "Archive workspace content" (no workspace concept exists in this
app), bulk archive (single-item Restore only — archiving itself is already single-item via the
existing per-task/per-project actions), and `ArchiveReason`/`ArchivedBy` fields (single-user
desktop app, same reasoning `TaskHistory` and `TaskVersion` both already documented for skipping
"who"/"why" fields). Search is a client-side substring filter over title/name, not a persisted
index.

**Verified:** covered by the same `TaskService`/`ProjectService` tests already exercising
`GetArchivedAsync`/`SetArchivedAsync`; the window's filtering logic is plain LINQ with no new
service-level branching to test in isolation.

Archive should not behave like Trash.

Archive means:

> Keep permanently, but remove from normal operational views.

> **Note:** DeskTodo already has a per-task Archive action (Phase 8/25 era). This entry describes a broader vault covering projects and workspace content too, with search/filter over archived items specifically.

## Requirements

- Archive task
- Archive project
- Archive workspace content
- Search archived items
- Restore
- Bulk archive
- Archive filters

## Suggested fields

```text
ArchivedAt
ArchiveReason
ArchivedBy
```

Archived records should normally remain searchable.

---

# Feature 46 — Trash / Recovery Center ✅ Delivered (2026-08-26)

## Objective

Make destructive deletion recoverable.

**Delivered.** DeskTodo already had a soft-delete flag (`TaskItem.IsDeleted`, set by `ITaskService.DeleteTaskAsync` since Phase 8) with no way to ever see or act on what was in it — a real, if quiet, gap: nothing in the app could reach a deleted task again except by knowing its Id. This feature closed that gap rather than building soft-delete from scratch.

**What shipped:**
- `TaskItem.DeletedAt` (new column, migration `AddTaskDeletedAt`) — distinct from `ModifiedAt`, which any later touch would overwrite; needed for a stable "deleted N days ago" display.
- `ITaskRepository.GetDeletedAsync()` / `ITaskService.GetDeletedTasksAsync()` — lists what's in the trash, most recently deleted first.
- `ITaskRepository.RemoveAsync(id)` — the one genuine **hard delete** path in this app (every other "delete" is the soft flag). Also cleans up anything that would otherwise block it at the database level: removes `TaskDependency` rows referencing the task (that FK is `Restrict`, not `Cascade`, by design — see `TaskDependencyConfiguration`) and *orphans* (does not delete) any subtasks, since a subtask of a since-soft-deleted parent may still be an entirely live task the user never asked to delete.
- `ITaskService.PermanentlyDeleteTaskAsync` / `EmptyTrashAsync` — the service-layer actions the Trash window's "Delete Forever" and "Empty Trash" buttons call, each gated behind a confirmation dialog in code-behind (matching this app's established "no ViewModel owns a Window to show a dialog from" split).
- A new `TrashWindow` (Restore / Delete Forever per row, Empty Trash), reachable from the tray menu ("Trash…") and the Command Palette ("Trash").

**Deliberately not built in this pass:** the retention-period auto-purge (7/30/90 days / never) the original spec describes. This pass is manual-only — Restore, Delete Forever, and Empty Trash are all explicit user actions, with no background sweep deleting anything on a timer. Automatic purging is a real feature with its own real risk (silently, permanently deleting something on a schedule) and deserves its own deliberate pass rather than being folded in here.

**Verified:** a real EF Core/SQLite integration test (not a mock) confirms `RemoveAsync` actually works against the real foreign-key constraints — including the two edge cases that would otherwise throw (a task with an active `TaskDependency` link, and a task with a still-live subtask) — plus service- and ViewModel-level tests for the rest of the flow. 568/568 tests passing, zero-warning build. Live-verified only as a non-interactive smoke check (app starts cleanly, the `DeletedAt` column applies to the real database on startup) — the interactive Restore/Delete Forever/Empty Trash flow was **not** clicked through live this pass, on the same safety grounds already recorded for Feature 38/Task Groups earlier in `IMPLEMENTATION.md` (synthetic UI input in this environment was found to reach the user's actual, concurrently-in-use desktop rather than an isolated surface).

## Soft Delete

Instead of immediately deleting:

```text
DeletedAt = current timestamp
```

Normal queries filter deleted records.

## Trash UI

Display:

- Deleted item
- Deleted date
- Original project
- Remaining retention period

Actions:

```text
Restore
Delete Permanently
Empty Trash
```

## Retention

Allow:

```text
7 days
30 days
90 days
Never automatically delete
```

Permanent deletion should require confirmation.

---

# Feature 47 — Smart Duplicate Detection ✅ Delivered (2026-09-01)

## Objective

Prevent users from accidentally creating duplicate tasks.

**Delivered, as a non-blocking notice rather than the spec's own modal (see below).**
`IDuplicateDetectionService.FindPossibleDuplicates` implements Level 1 (exact normalized-title
match, score 1.0) and Level 2 (Jaccard token-similarity over normalized word sets) against a
caller-supplied candidate pool, plus a same-day/same-category score boost standing in for Level
3's fuller context weighting. Wired into `WidgetViewModel.AddTaskAsync`: the task is still always
created (Enter-to-add is this app's fastest, most-used path), but if a candidate scores ≥ 0.6
among that day's incomplete tasks, `DuplicateWarningMessage` shows a small "Possible duplicate
of…" note under the add-task row.

**Deliberately not built:** the "Possible duplicate detected — [Use Existing] [Create Anyway]"
blocking modal — gating the fastest path in the app behind a dialog on every merely-similar
title would cost more than the occasional true duplicate does; Level 3's full context (existing
status) and Level 4's semantic embeddings are both out of scope for this pass, as the spec's own
"Future" note already anticipated for the embeddings tier.

**Verified:** pure unit tests (`DuplicateDetectionServiceTests`) covering exact match, no match,
partial-similarity scoring, the context boost, normalization (punctuation/case), and result
ordering — no database involved, since this is a stateless text/context comparison.

## Detection layers

### Level 1 — Exact match

Normalize title and compare.

### Level 2 — Similar text

Compare normalized text and token similarity.

### Level 3 — Context

Consider:

- Project
- Due date
- Tags
- Task type
- Existing status

### Future

Semantic embeddings can be added later without changing the UI contract.

## UX

When creating:

```text
Possible duplicate detected

"Deploy production API"

Existing:
Project: Release 2.4
Due: Aug 25
Status: In Progress

[Use Existing] [Create Anyway]
```

---

# Feature 48 — Task Relationships Graph

## Objective

Show relationships between tasks and projects visually.

## Relationship types

```text
Related
Blocks
Blocked By
Depends On
Duplicate Of
Derived From
Follow-up Of
```

> **Note:** DeskTodo already has Blocks/Blocked By as a lightweight dependency guard (Phase 19's `TaskDependency`, with a completion guard). This entry describes the fuller relationship model plus a visual graph view — an extension, not a replacement.

## Data Model

```text
TaskRelationship
----------------
Id
SourceTaskId
TargetTaskId
RelationshipType
CreatedAt
```

## UI

Nodes:

```text
Task A
  |
  +---- blocks ----> Task B
  |
  +---- related ---> Task C
```

## Requirements

- Zoom
- Pan
- Select node
- Open task
- Filter relationship types
- Avoid infinite graph loading

---

# Feature 49 — Goal → Project → Task Mapping

## Objective

Introduce a strategic planning hierarchy.

```text
Goal
  ↓
Project
  ↓
Task
  ↓
Subtask
```

Example:

```text
Goal:
Launch Product

Projects:
Website
Mobile App
Marketing

Tasks:
Implement login
Create landing page
Prepare launch campaign
```

> **Note:** DeskTodo already has a `Goal` entity (Phase 21/23, habit-streak shaped) and a `Project` entity (Phase 25). This entry's "Goal" is a different shape (a strategic target with projects under it, not a daily habit streak) — reconciling the two `Goal` concepts is real design work this entry doesn't resolve on its own.

## Goal Entity

```text
Goal
----
Id
Name
Description
TargetDate
Status
Progress
Priority
```

## Progress

Goal progress can be calculated from:

- Project completion
- Milestone completion
- Weighted tasks

The algorithm should be configurable.

---

# Feature 50 — Milestone Tracking

## Objective

Represent major project checkpoints.

> **Note:** DeskTodo already has a `Milestone` entity (Phase 21, target-date deliverables tasks can link to). This entry adds ordering within a project and richer status — check for overlap before building a second `Milestone` shape.

## Entity

```text
Milestone
---------
Id
ProjectId
Name
Description
TargetDate
CompletedAt
Status
Order
```

## Example

```text
Project: Mobile App

Milestones:
1. Architecture complete
2. MVP complete
3. Beta complete
4. Production release
```

## UI

Display milestones:

- Project timeline
- Calendar
- Project dashboard
- Progress reports

---

# Feature 51 — Project Health Score

## Objective

Provide an automatic project health assessment.

## Inputs

Potential factors:

- Completion percentage
- Overdue task percentage
- Blocked tasks
- Deadline proximity
- Milestone status
- Workload
- Recent activity
- Unassigned tasks

## Example formula

A configurable scoring engine could calculate:

```text
Health =
  Completion Score
  - Overdue Penalty
  - Risk Penalty
  - Blocked Penalty
  + Recent Activity Score
```

Do not make the exact formula hardcoded.

## Output

```text
Healthy
Warning
Critical
Unknown
```

The UI should explain why.

Example:

```text
Warning

Reasons:
- 7 overdue tasks
- Next milestone is in 2 days
- Capacity is 115%
```

---

# Feature 52 — Deadline Risk Detection

## Objective

Identify tasks likely to miss deadlines before they become overdue.

## Inputs

- Remaining estimated effort
- Available working hours
- Due date
- Historical completion speed
- Dependencies
- Current status
- Blocking tasks

## Example

```text
Task:
Complete API migration

Remaining effort: 12h
Available capacity: 7h
Deadline: Tomorrow

Risk: HIGH
```

## UI

Show:

- Risk level
- Risk explanation
- Suggested action

Possible actions:

```text
Reschedule
Increase capacity
Split task
Reduce scope
Change priority
```

---

# Feature 53 — Workload Heatmap

## Objective

Visualize planned work across time.

> **Note:** DeskTodo already has a 12-week completion heat map (Phase 24 Analytics — task-count based, GitHub-contribution-graph style). This entry is a different axis: planned *hours* vs. available *capacity*, not completed-task count.

## Calculation

For every day:

```text
Planned Work = sum(task estimated duration)
```

Compare with:

```text
Available Capacity
```

## Example

```text
Monday      4h / 8h
Tuesday     7h / 8h
Wednesday  11h / 8h  OVERLOAD
Thursday    3h / 8h
```

## UI

Use calendar heatmap.

Clicking a day should show the tasks contributing to the workload.

---

# Feature 54 — Capacity Planning

## Objective

Allow users to define how much time they actually have available.

## Capacity Inputs

- Working hours
- Work days
- Holidays
- Breaks
- Personal unavailable periods
- Focus availability
- Existing commitments

## Model

```text
CapacityProfile
---------------
WorkingHoursPerDay
WorkingDays
BreakMinutes
Timezone
HolidayCalendar
```

## Calculation

```text
Available Capacity
-
Planned Work
=
Remaining Capacity
```

Negative values indicate overload.

---

# Feature 55 — Time Estimation Accuracy

## Objective

Learn whether task estimates are realistic.

> **Note:** DeskTodo already has `TaskItem.EstimatedMinutes` and `ActualMinutes` (Phase 23 Time Tracking). This entry is the analytics layer on top of fields that already exist — no new schema needed for the raw numbers, only for the aggregated accuracy report.

## Data

Every task can contain:

```text
EstimatedDuration
ActualDuration
```

Calculate:

```text
Variance = Actual - Estimated
AccuracyRatio = Actual / Estimated
```

## Analytics

Example:

```text
Average estimate accuracy: 82%

Bug tasks: 65%
Meetings: 110%
Documentation: 145%
Development: 92%
```

This becomes useful for future planning.

---

# Feature 56 — Task Cost Tracking

## Objective

Optionally estimate the financial cost of work.

## Inputs

- Hourly rate
- Estimated duration
- Actual duration

## Calculation

```text
Estimated Cost =
Estimated Hours × Hourly Rate

Actual Cost =
Actual Hours × Hourly Rate
```

## Scope

Keep this optional because many users will not need monetary tracking.

---

# Feature 57 — Decision Log

## Objective

Record important decisions independently from ordinary tasks.

## Entity

```text
Decision
--------
Id
ProjectId
Title
Context
Decision
Alternatives
Reason
CreatedAt
RelatedTaskIds
```

## Example

```text
Decision:
Use PostgreSQL instead of MongoDB

Reason:
Relational reporting requirements

Alternatives:
MongoDB
Cosmos DB
PostgreSQL
```

## UI

Decisions should be searchable and linked to projects/tasks.

---

# Feature 58 — Meeting Mode

## Objective

Create a temporary workspace optimized for meetings.

## Layout

```text
Meeting
------------------------------------------------
Title
Date
Participants
------------------------------------------------
Agenda

1. Release status
2. API migration
3. Production deployment

Notes

...

Decisions

...

Action Items

[ ] John - Review API
[ ] Amit - Prepare deployment
------------------------------------------------
End Meeting
```

## End Meeting

Provide actions:

```text
Create Tasks
Save Notes
Save Decisions
Schedule Follow-ups
```

---

# Feature 59 — Meeting Action Extractor

## Objective

Turn meeting notes into structured action candidates.

Input:

```text
John will review the API by Friday.
Amit needs to prepare the release notes.
Sarah will arrange testing next week.
```

Output:

```text
Task: Review API
Owner: John
Deadline: Friday

Task: Prepare release notes
Owner: Amit

Task: Arrange testing
Owner: Sarah
Deadline: Next week
```

## Architecture

Create:

```text
IMeetingActionExtractor
```

Implement deterministic parsing first.

AI can be plugged in later.

---

# Feature 60 — Daily Journal

## Objective

Provide a date-based personal/work journal.

## Entity

```text
JournalEntry
-----------
Id
Date
Title
Content
MoodOptional
Tags
RelatedTaskIds
RelatedProjectIds
```

## Features

- Markdown/rich text
- Daily navigation
- Search
- Link tasks
- Link projects
- Attachments
- Export

Do not turn the journal into another task list.

---

# Feature 61 — Activity Timeline ✅ Delivered (2026-09-01)

## Objective

Show what happened over time.

**Delivered, as a query-time aggregation rather than an Event-Bus consumer (see below).**
`IActivityTimelineService.GetRecentActivityAsync` merges three already-persisted sources —
`TaskHistory` (Feature 42) via a new `ITaskHistoryRepository.GetAllAsync`, completed
`FocusSession`s (Phase 23), and `GoalCompletion`s (Phase 21) — into one chronological feed,
newest first. Reachable via a new `ActivityTimelineWindow` from the Command Palette.

**Deliberately not built exactly as specified:** this feature explicitly calls for reusing an
Event Bus (Feature 98), which doesn't exist yet. Building a full pub/sub platform just to feed
one read-only feed would be backwards — this aggregates directly from each feature's own history
instead, the same "no new persistence, read what already exists" approach Phase 21's Agenda/
Timeline views already use. Project-created and Milestone-completed events aren't included:
`Project`/`Milestone` have no creation/completion timestamp suited to a timeline entry (Milestone
tracks `IsCompleted` as a plain boolean with no completion time — see docs on `Milestone` — so
"when" isn't knowable). Once Feature 98 lands, this is the service that should be rewritten to
consume it instead of polling three sources at query time.

**Verified:** mocked service-level tests (`ActivityTimelineServiceTests`) confirming each source
maps to an entry, timestamp-descending ordering across sources, and the `limit` parameter.

Events can include:

```text
Task completed
Project created
Milestone completed
Focus session completed
Decision recorded
Task moved
Task archived
Backup completed
```

## Architecture

Reuse the Event Bus (Feature 98).

Do not create separate logging code for every feature.

The Activity Timeline consumes application events.

---

# Feature 62 — Achievement / Progress System

## Objective

Provide optional productivity feedback.

Examples:

```text
Completed first project
Completed 100 tasks
50 focus hours
Completed all weekly milestones
Maintained task organization
```

Avoid aggressive gamification.

The system should focus on:

- Progress
- Personal milestones
- Historical improvement

rather than points and addictive mechanics.

---

# Feature 63 — Focus Contexts

## Objective

Allow users to switch the application's active context.

Examples:

```text
Work
Personal
Learning
Side Project
Finance
```

## Important distinction

Contexts should not duplicate projects.

A task may belong to:

```text
Project: InElection
Context: Side Project
```

## UI

Context switcher can filter:

```text
All
Work
Personal
Learning
```

---

# Feature 64 — Distraction Log

## Objective

Record interruptions during focus sessions.

> **Note:** builds on DeskTodo's existing `FocusSession` (Phase 23) — an interruption is naturally scoped to a running session.

Quick action:

```text
Log Distraction
```

Categories:

```text
Phone
Email
Chat
Meeting
Website
Personal
Other
```

Store:

```text
Start
End
Duration
Category
Notes
RelatedSession
```

## Analytics

Show:

- Number of interruptions
- Total interruption time
- Most common category
- Average interruption duration
- Distraction by time of day

---

# Feature 65 — Work Session History ✅ Delivered (2026-09-01)

## Objective

Provide permanent history of focus/work sessions.

**Delivered as a reporting layer, per the spec's own note — no parallel `WorkSession` entity was
added.** `FocusSession` (Phase 23) already had everything needed except a task title for display,
which `FocusSessionRepository.GetAllAsync` now `Include`s. A new `WorkSessionHistoryWindow` shows
the full session list plus Today/This week totals, computed client-side in
`WorkSessionHistoryViewModel` from that same list — no new service method needed. Reachable from
the Command Palette.

**Deliberately not built:** `SessionType` beyond the existing `FocusSessionType` (Pomodoro/
Stopwatch/CountdownTimer already cover this — see that enum's own doc comment on why "Deep Work"/
"Planning"/"Meeting"/"Research" don't need to be separate members), `Interruptions`/`ProjectId`
fields, and the per-project report line — `FocusSession` links to a `TaskItem`, not directly to a
`Project`, and adding a project rollup would need a join this pass didn't need for the headline
Today/This week numbers.

**Verified:** no new backend logic beyond the `Include` — covered by the existing
`FocusSessionRepositoryTests`/`FocusSessionServiceTests`; the totals are plain LINQ sums over
already-tested data.

> **Note:** DeskTodo's `FocusSession` entity (Phase 23) already persists every session with start/end/duration. This entry is mostly a reporting/UI layer over existing data, plus a `SessionType` field the current model may not have yet — check before adding a parallel entity.

## Entity

```text
WorkSession
-----------
Id
TaskId
ProjectId
StartedAt
EndedAt
Duration
SessionType
Completed
Interruptions
Notes
```

## Session types

```text
Focus
Deep Work
Planning
Meeting
Research
Other
```

## Reports

Examples:

```text
Today: 4h 20m
This week: 27h 40m
Project X: 11h 15m
```

---

# Feature 66 — Offline-first Conflict Resolver

## Objective

Prepare for cloud sync while keeping local-first behavior.

> **Note:** directly overlaps with DeskTodo's Phase 31 (Cloud sync & multi-device, deferred to last). The approach discussion already had for Phase 31 (see `IMPLEMENTATION.md`) picked a lighter "sync via an existing cloud folder" path with per-record last-write-wins merging via `TaskItem.ModifiedAt` — this entry's field-level/device-vector model is more ambitious than that decision. Reconcile with Phase 31's recorded approach before building either.

This phase is especially important because Phase 31 is cloud sync.

## Record Versioning

Every synchronizable entity should eventually have:

```text
EntityId
Revision
UpdatedAt
UpdatedBy
DeviceId
```

## Conflict

Example:

```text
Device A:
Task title = "Deploy API"

Device B:
Task title = "Deploy Production API"
```

The resolver should show both.

## Resolution

Options:

```text
Keep Local
Keep Remote
Merge
Keep Newer
Manual
```

## Field-level merge

Prefer merging independent fields.

Example:

```text
Device A changed Priority
Device B changed DueDate
```

Both can be retained.

---

# Feature 67 — Local Backup Manager ✅ Delivered (2026-09-01)

## Objective

Protect user data independently of cloud sync.

**Delivered, scoped to Manual/Full backups only (see below).** `IBackupService`
(`src/DeskTodo.Infrastructure/Backup/BackupService.cs`) zips the live SQLite database and
settings.json into a timestamped archive under `{AppStorageOptions.RootDirectory}/backups/` —
millisecond-resolution filenames (`desktodo-backup-yyyyMMdd-HHmmssfff.zip`), not just seconds,
specifically so a rapid pair of backups (e.g. the pre-restore safety backup Feature 68 takes)
can never collide and silently overwrite one another. Retention keeps the most recent 14
backups, pruning older ones after each create. Reachable via a new `BackupWindow` (Create Backup
Now / Preview Restore / Delete per row), from the tray menu ("Backups…") and Command Palette.

**Deliberately not built:** Scheduled/Incremental backup types (manual-only, matching Feature
46's Trash own "manual-only, no auto-purge" scope cut), Attachments/Templates/Custom Fields in
the archive (only the database file + settings.json — attachments live under the same root
directory the database backup already anchors to, but aren't separately archived in this pass),
and encrypted backups.

**Verified:** real (not mocked) file-based tests — an actual SQLite file on disk, a real zip
archive, and a real file-copy restore — covering create/list/delete, and restore actually
replacing the live database's row count. One genuine bug this testing caught and fixed: the
original second-resolution filename let a same-second safety-backup overwrite the very backup
being restored from, silently no-op'ing the restore; the millisecond-resolution filename above
is the fix, verified by a regression test exercising exactly that sequence.

## Backup Types

- Manual
- Scheduled
- Full
- Incremental if later supported

## Backup contents

Potentially:

```text
SQLite database
Attachments
Configuration
Workspace metadata
Templates
Custom fields
Views
```

## Retention

Example:

```text
Keep last 7 backups
Keep weekly backups for 3 months
```

## Security

Optional encrypted backup.

---

# Feature 68 — Backup Restore Simulator ✅ Delivered (2026-09-01)

## Objective

Verify that backups are usable without overwriting the active workspace.

**Delivered, as a diff summary rather than a temporary-workspace dry-run (see below).**
`IBackupService.SimulateRestoreAsync` extracts a backup's database to a temp file and reads its
`Tasks` table via a raw ADO.NET query (deliberately *not* a second `DeskTodoDbContext` — pointing
EF Core's migration-aware context at an old backup file could try to apply pending migrations to
it, which a read-only preview should never risk), then compares task IDs/`ModifiedAt`/
`IsDeleted` against the live database to report how many tasks would be added/updated/removed,
with a handful of sample titles. `BackupWindow`'s "Preview Restore" shows this summary before
"Restore This Backup" is enabled, gated behind the same `ConfirmDialogWindow` every other
consequential action in this app uses.

**Deliberately not built:** the "Create Temporary Workspace → Restore → Run Migrations" flow —
no second full copy of the app's storage tree is materialized; the raw-query comparison above
answers the same "is this backup usable and what would change" question without needing a
throwaway workspace. Attachment/index/migration-version counts from this feature's own sketch
output aren't included, only task-level add/update/remove counts.

**Verified:** a real file-based test seeds a live database with an extra task the backup doesn't
have, then asserts `SimulateRestoreAsync` correctly reports it as a removal.

## Process

```text
Select Backup
      ↓
Create Temporary Workspace
      ↓
Restore
      ↓
Run Migrations
      ↓
Run Integrity Checks
      ↓
Generate Report
```

## Result

```text
Backup valid
Records: 18,430
Attachments: 231
Integrity: PASS
Migration: PASS
```

---

# Feature 69 — Database Maintenance Center ✅ Delivered (2026-09-01)

## Objective

Provide diagnostic tools for the application's SQLite database.

**Delivered.** `IDatabaseMaintenanceService.GetStatsAsync` reports database file size and
task/project/tag/history/version/attachment counts plus the latest applied migration; `VacuumAsync`/
`RebuildIndexesAsync` run SQLite's own `VACUUM`/`REINDEX`. Backup and Integrity Check — two of
this feature's five listed operations — are deliberately **not** duplicated here: Features 67
and 70 already own those, each with its own window, so `DatabaseMaintenanceWindow` only exposes
the two operations neither of those covers, plus the stats dashboard. Reachable from the tray
menu ("Database Maintenance…") and Command Palette.

**Verified:** real SQLite file-on-disk tests — real migrations applied (so the reported migration
version is genuine, not "(none)" the way `EnsureCreated` would leave it), real row counts, and
both `VACUUM`/`REINDEX` actually run against a real file without corrupting it (confirmed by
re-querying afterward).

---

**Stage 1 (Core Reliability and Data Infrastructure) is now fully delivered**: 42, 43, 44, 46,
67, 68, 69, 70 — every item this file's own "Recommended Implementation Order" section listed.

## Dashboard

Display:

- Database size
- Number of tasks
- Projects
- Tags
- History records
- Versions
- Attachments
- Index information
- Migration version

## Operations

Potential operations:

```text
Analyze Database
Rebuild Indexes
Vacuum
Integrity Check
Backup
```

Destructive operations must require confirmation.

---

# Feature 70 — Data Integrity Checker ✅ Delivered (2026-09-01)

## Objective

Find invalid internal references and inconsistent data.

**Delivered.** `IDataIntegrityService` (`src/DeskTodo.Infrastructure/Data/DataIntegrityService.cs`)
runs SQLite's own `PRAGMA integrity_check` (low-level page corruption — reported, never
auto-repaired) plus application-level checks: a task referencing a deleted category, a task that
is its own parent or references a deleted parent, an attachment row whose backing file is
missing from disk, and negative estimated/actual-minutes values. Each finding is an
`IntegrityIssue(Category, Description, IsAutoRepairable)`; `RepairAsync` fixes only the
unambiguously-safe subset (clearing a dangling reference, removing an orphaned attachment row,
clamping a negative minutes value to 0) — never the SQLite-level corruption finding. Reachable
via a new `IntegrityCheckWindow` ("Run Check" / "Fix All Safe Issues"), from the tray menu ("Data
Integrity Check…") and Command Palette.

**Deliberately not built:** duplicate-identifier and invalid-status checks (this schema has no
free-form status field to validate — see `TaskItem`'s own doc comment on why there's no single
`TaskStatus` enum — and GUID primary keys make accidental duplicates a non-issue), and Project/
Milestone dangling-reference checks (only `CategoryId`/`ParentTaskId`/`Attachment` are covered in
this pass — Project and Milestone both already use `SetNull` FKs the same way Category does, so
a dangling reference there isn't actually reachable today, but a check wasn't added for
completeness beyond what's currently possible to produce).

**Verified:** real EF Core/SQLite tests (a genuinely self-parented task, a genuinely negative
estimate, a genuinely missing attachment file) confirming both detection and repair, plus a
healthy-database case confirming zero false positives.

## Checks

Examples:

```text
Task references missing Project
Subtask references missing Parent
Relationship references missing Task
Invalid status
Invalid dates
Duplicate identifiers
Missing required values
```

## Output

```text
Integrity Check

Errors: 2
Warnings: 5

[View Problems]
[Repair Safe Issues]
```

Repairs should be explicit and logged.

---

# Feature 71 — Portable Workspace

## Objective

Allow users to place workspace data in a selectable location.

Example:

```text
D:\MyProductivityWorkspace
/Users/Amit/ProductivityWorkspace
External Drive/Productivity
```

## Workspace structure

```text
Workspace/
    database/
    attachments/
    backups/
    exports/
    templates/
    configuration/
```

## Requirements

- Select workspace
- Open existing workspace
- Validate workspace
- Move workspace
- Detect missing files

---

# Feature 72 — Multiple Profiles

## Objective

Support completely independent environments.

Example:

```text
Personal
Work
Testing
```

Each profile may have:

```text
Database
Settings
Themes
Templates
Views
Shortcuts
Workspace
```

## Important

Switching profiles must not accidentally expose data from another profile.

---

# Feature 73 — Guest / Presentation Mode

## Objective

Provide a safe presentation view.

Example:

User wants to share a project screen but does not want to show:

- Personal tasks
- Private notes
- Financial data
- Other projects

## Flow

```text
Presentation Mode
       ↓
Select Workspace/Project
       ↓
Select visible fields
       ↓
Enter presentation
```

Exit returns to the normal application.

---

# Feature 74 — Lock Specific Workspace

## Objective

Protect sensitive workspace data.

> **Note:** DeskTodo already has app-wide PIN Lock (Phase 29, PBKDF2-hashed, off by default, opt-in only — see that phase's notes on why it must never be silently required). This entry describes locking a specific workspace/profile rather than the whole app; reuse the existing `PinHasher`/lock-screen pattern rather than building a second one.

## Requirements

- Lock manually
- Auto-lock after inactivity
- Unlock
- Lock when application starts
- Lock on system sleep where supported

Prefer OS-level secure authentication where platform APIs permit it.

Passwords should never be stored as plaintext.

---

# Feature 75 — Privacy Mode

## Objective

Quickly hide sensitive information.

Shortcut:

```text
Ctrl/Cmd + Shift + P
```

Possible behavior:

```text
Project names → ▓▓▓▓▓▓▓
Task titles → ▓▓▓▓▓▓▓▓▓
Notes → Hidden
```

Privacy mode should also affect:

- Notifications
- Tray previews
- Dashboard
- Search suggestions
- Presentation surfaces

---

# Feature 76 — Sensitive Data Detector

## Objective

Warn users if they accidentally save secrets in task content.

Potential patterns:

```text
API keys
JWT tokens
Private keys
Connection strings
Passwords
Cloud credentials
```

## Architecture

Create:

```text
ISensitiveDataDetector
```

Rules can be updated independently.

## UX

```text
Potential secret detected

This text appears to contain a credential.

[Remove]
[Keep Anyway]
[Don't Warn Again]
```

Never transmit the content externally just to perform detection.

---

# Feature 77 — Keyboard Shortcut Manager

## Objective

Allow users to customize application shortcuts.

> **Note:** DeskTodo already registers a fixed set of shortcuts in code (Phase 28, `RegisterKeyboardShortcuts` — Cmd/Ctrl+K/F/,), specifically because Avalonia's XAML `KeyGesture` has no OS-conditional Cmd/Ctrl translation. This entry's customizable version needs to preserve that same per-OS modifier resolution while adding user-editable bindings on top.

## Model

```text
Shortcut
--------
CommandId
KeyCombination
Scope
Enabled
```

## Requirements

- Display defaults
- Edit shortcut
- Detect conflict
- Restore default
- Export configuration
- Import configuration

Scopes can include:

```text
Global
Application
Task Editor
Calendar
Grid
```

---

# Feature 78 — Mouse Gesture Manager

## Objective

Allow configurable mouse gestures.

Example:

```text
Right mouse + Left
    → Previous page

Right mouse + Right
    → Next page
```

## Architecture

Gestures should resolve to registered commands rather than directly calling UI methods.

This keeps gestures compatible with the Command Palette and shortcut manager.

---

# Feature 79 — Macro / Automation Recorder

## Objective

Allow users to automate repetitive application workflows.

Example:

```text
Create Task
Set Project
Set Priority
Add Tag
Set Due Date
```

Save as:

```text
"Create Bug Task"
```

## Important design decision

Record semantic commands:

```text
CreateTask
SetPriority
SetProject
```

Do not record raw UI coordinates.

This makes macros resilient to UI changes.

## Safety

Destructive macros require confirmation.

---

# Feature 80 — Custom Fields Builder

## Objective

Allow users to define custom metadata.

Examples:

```text
Client
Ticket ID
Environment
Cost
Story Points
Release
URL
```

## Supported types

```text
Text
Number
Boolean
Date
DateTime
Dropdown
Multi-select
URL
Email
Currency
```

## Field definition

```text
CustomFieldDefinition
---------------------
Id
Name
Type
Required
DefaultValue
ValidationRules
DisplayOrder
```

## Values

Use a flexible value storage strategy that does not require schema migrations whenever a user creates a new field.

---

# Feature 81 — Custom Task Types

## Objective

Allow users to define task categories with different behavior.

> **Note:** DeskTodo already has a fixed `TaskType` enum (Task/Event/Reminder/Note/Meeting, Phase 8). This entry replaces that closed set with a user-definable one — a real breaking change to the existing type system, not an additive feature; needs a migration plan for existing tasks' types.

Examples:

```text
Bug
Feature
Meeting
Research
Deployment
Call
Maintenance
```

## Task type configuration

Potential settings:

- Name
- Icon
- Color
- Default priority
- Default status
- Custom fields
- Workflow
- Default duration

---

# Feature 82 — Custom Status Workflow

## Objective

Allow users to define lifecycle states.

Example:

```text
Backlog
   ↓
Ready
   ↓
Development
   ↓
Review
   ↓
Testing
   ↓
Released
```

## Data Model

```text
Workflow
WorkflowStatus
WorkflowTransition
```

## Transition rules

Example:

```text
Development → Review
Review → Testing
Testing → Released
```

Optional restrictions:

- Required fields
- Required checklist completion
- Required approval
- Required comment

---

# Feature 83 — Saved Views

## Objective

Save complex query configurations.

> **Note:** DeskTodo already has this, scoped to the grid view (Phase 20, `GridSavedView` — filters, sort, columns, layout, persisted in `AppSettings.GridSavedViews`). This entry is the same concept generalized beyond the grid; reuse `GridSavedView`'s shape rather than inventing a parallel one if extending it to other surfaces (Calendar, Planner tabs).

Example:

```text
My Critical Work This Week
```

Configuration:

```text
Filter:
Priority = High
Status != Completed
DueDate <= This Week

Sort:
DueDate ASC

Group:
Project

Columns:
Title
Priority
DueDate
Status
```

## View model

```text
SavedView
---------
Id
Name
Filters
Sort
Group
Columns
Layout
Scope
```

---

# Feature 84 — View Sharing Templates

## Objective

Export and import saved views.

## Format

Use a versioned JSON structure.

Example conceptual format:

```text
{
  version,
  name,
  filters,
  sorting,
  grouping,
  columns,
  layout
}
```

## Requirements

- Export
- Import
- Validate
- Version compatibility
- Conflict handling

---

# Feature 85 — Workspace Templates

## Objective

Create complete reusable workspace configurations.

A template may contain:

```text
Projects
Lists
Tags
Task Types
Statuses
Workflows
Views
Custom Fields
Dashboards
Templates
```

## Creation

```text
Current Workspace
       ↓
Save as Template
       ↓
Template Library
```

## Usage

```text
New Workspace
       ↓
Choose Template
       ↓
Create
```

---

# Feature 86 — Project Starter Kits

## Objective

Create standard projects quickly.

Example:

```text
Software Release Kit

Tasks:
- Requirements
- Development
- Testing
- Documentation
- Deployment
- Monitoring

Milestones:
- Code Complete
- QA Complete
- Release
```

Dates should be relative.

Example:

```text
Requirements: Day 1
Development: Day 2–7
Testing: Day 8–10
Release: Day 11
```

---

# Feature 87 — Recurring Project Templates

## Objective

Generate complete project structures repeatedly.

> **Note:** DeskTodo already has per-task recurrence (Phase 19, daily/weekly/monthly) and, separately, Task Groups (Phase 38 — a named list of `TaskTemplate`s applied to a chosen day in one click). This entry is recurrence applied at the *project* level, not the task level — a materially bigger scope than either existing feature, not a small extension of Task Groups.

Examples:

```text
Monthly Reporting
Weekly Team Review
Quarterly Planning
Annual Audit
```

## Scheduler

Configuration:

```text
Frequency
Start Date
Template
Generated Project Name
Date Offset Strategy
```

## Important

Generated projects should receive unique IDs and retain a link to the originating template.

---

# Feature 88 — Bulk Edit Rules

## Objective

Provide powerful multi-task operations.

> **Note:** DeskTodo already has bulk complete/delete on a multi-selection (Phase 28's Batch Actions). This entry is a much larger, rule/condition-based system ("find tasks matching X, apply Y") — a genuinely new capability, not an extension of the existing bulk actions.

Example condition:

```text
Project = X
AND
Priority = High
AND
DueDate < Today
```

Action:

```text
Set Priority = Critical
Add Tag = overdue
Move to Project = Recovery
```

## Safety

Always show:

```text
37 tasks will be modified.

[Preview]
[Apply]
[Cancel]
```

For destructive operations, require additional confirmation.

---

# Feature 89 — Mass Import Wizard

## Objective

Import arbitrary CSV/JSON files.

> **Note:** DeskTodo already has CSV/JSON import (Phase 14, `ITaskImportService`) with a fixed `TaskExportRecord` shape. This entry adds field mapping (arbitrary source columns → DeskTodo fields) and a preview/validate/dedupe pipeline the existing importer doesn't have.

## Flow

```text
Select File
    ↓
Detect Format
    ↓
Map Fields
    ↓
Preview
    ↓
Validate
    ↓
Duplicate Check
    ↓
Import
    ↓
Report
```

## Mapping

Example:

```text
CSV "Task Name" → Title
CSV "Deadline" → DueDate
CSV "Category" → Tags
```

## Rollback

Import should be transactional where practical.

If validation fails, no partial import should remain.

---

# Feature 90 — Data Migration Center

## Objective

Provide a central framework for moving data from other systems.

Potential sources later:

```text
CSV
JSON
Generic API
Other task applications
Project-management systems
```

## Migration pipeline

```text
Source
 ↓
Reader
 ↓
Normalizer
 ↓
Mapper
 ↓
Validator
 ↓
Duplicate Resolver
 ↓
Importer
 ↓
Migration Report
```

Each migration should have an ID and log.

---

# Feature 91 — Export Profiles

## Objective

Save frequently used export configurations.

> **Note:** DeskTodo already has CSV/JSON/Markdown/Excel export (Phase 14, `ITaskExportService`). This entry is a saved-configuration layer on top (which format + which filters + which fields, named and reusable) — no new export format needed, just a preset system.

Example:

```text
Weekly Project Report
```

Configuration:

```text
Format: CSV
Project: Current
Date Range: This Week
Fields: Title, Status, Priority, DueDate
```

Another:

```text
Executive Report
Format: PDF
Group: Project
Include: Milestones + Progress
```

---

# Feature 92 — Print Layout Designer

## Objective

Allow users to design printable reports.

## Components

Potential components:

```text
Title
Project Metadata
Task Table
Milestone Timeline
Progress Summary
Charts
Notes
Footer
```

## Layout

Support:

- Page size
- Margins
- Orientation
- Header/footer
- Repeating table headers
- Page breaks

Layouts should be saved and reusable.

---

# Feature 93 — Custom Dashboard Builder

## Objective

Allow users to create personalized dashboards.

> **Note:** DeskTodo already has a fixed Analytics dashboard (Phase 24 — weekly/monthly completion, streak counter, focus time, heat map, per-category breakdown). This entry is a user-configurable dashboard with arbitrary widgets, a materially different (and larger) feature than customizing the existing fixed layout.

## Widget examples

```text
Today's Tasks
Overdue Tasks
Project Progress
Workload
Focus Time
Milestones
Goals
Activity
Upcoming Deadlines
```

## Dashboard model

```text
Dashboard
---------
Id
Name
Widgets
Layout
Settings
```

Each widget should have:

```text
WidgetId
Position
Width
Height
Configuration
```

Use a grid layout.

---

# Feature 94 — Widget Marketplace Architecture

## Objective

Create an extensible architecture for independent widgets.

This does not necessarily mean building a public marketplace immediately.

First build the architecture.

## Widget contract

Conceptually:

```text
IWidget
-------
Id
Name
Version
Initialize()
Render()
Configure()
Dispose()
```

## Widget metadata

```text
Permissions
Settings
SupportedPlatform
Version
Author
```

## Security

Third-party widgets must not automatically receive unrestricted application access.

---

# Feature 95 — Plugin SDK

## Objective

Allow developers to extend the application.

## Plugin structure

Potentially:

```text
plugin.json
plugin.dll
assets/
```

Manifest:

```text
Name
Id
Version
Author
RequiredAppVersion
Permissions
EntryPoint
```

## Plugin lifecycle

```text
Discover
 ↓
Validate
 ↓
Load
 ↓
Initialize
 ↓
Run
 ↓
Disable
 ↓
Unload
```

## Permissions

Possible permissions:

```text
ReadTasks
WriteTasks
ReadProjects
WriteProjects
ReadFiles
NetworkAccess
Webhooks
```

Use least privilege.

---

# Feature 96 — Webhook Engine

## Objective

Allow external systems to react to application events.

## Example

When task completes:

```text
TaskCompleted
     ↓
Webhook
     ↓
POST https://example.com/webhook
```

## Configuration

```text
Webhook Name
URL
Events
Headers
Secret
Enabled
Retry Policy
```

## Reliability

Implement:

- Retry
- Exponential backoff
- Timeout
- Delivery status
- Failure history
- Disable after repeated failures

## Security

Support signing webhook payloads.

---

# Feature 97 — Local REST API

## Objective

Allow external applications to interact with the desktop app.

## Example endpoints

```text
GET    /api/v1/tasks
GET    /api/v1/tasks/{id}
POST   /api/v1/tasks
PUT    /api/v1/tasks/{id}
DELETE /api/v1/tasks/{id}

GET    /api/v1/projects
POST   /api/v1/projects
```

Additional APIs:

```text
/search
/views
/goals
/milestones
/tags
/events
```

## Security

The API should:

- Bind to localhost by default
- Require authentication
- Use API tokens
- Support permission scopes
- Version endpoints

Do not expose the API publicly by default.

---

# Feature 98 — Event Bus / Extension Events

## Objective

Create a central event-driven architecture.

## Example events

```text
TaskCreated
TaskUpdated
TaskCompleted
TaskDeleted
TaskRestored

ProjectCreated
ProjectUpdated
ProjectCompleted

MilestoneCompleted

FocusSessionStarted
FocusSessionCompleted

BackupCompleted
```

## Event model

```text
ApplicationEvent
---------------
EventId
EventType
Timestamp
Source
EntityId
Payload
```

## Consumers

The same event can be consumed by:

```text
Activity Timeline
Analytics
Notifications
Webhooks
Plugins
Automation
Audit
```

This dramatically reduces coupling.

---

# Feature 99 — CLI Tool

## Objective

Provide terminal-based access.

Examples:

```bash
app task add "Prepare release notes"
app task list
app task list --overdue
app task complete 123
app task search "release"
app project list
app project open 123
```

## Architecture

Prefer:

```text
CLI
 ↓
Local REST API
 ↓
Application Layer
```

rather than duplicating business logic in the CLI.

This ensures the CLI behaves exactly like the desktop application.

---

# Feature 100 — Developer API Explorer

## Objective

Create an embedded API-development/testing interface.

It should feel similar to a lightweight Postman/Swagger client.

## Sections

```text
API Explorer
----------------------------------------
GET    /api/v1/tasks

Query Parameters
Headers
Body

[Send]

Response
----------------------------------------
Status: 200

{
  ...
}
```

## Features

- Endpoint list
- Search endpoints
- Request editor
- Query parameters
- Headers
- JSON body editor
- Send request
- Response viewer
- Response timing
- Status code
- Copy response
- Save request
- Authentication testing

## Event Explorer

Also provide:

```text
Recent Events
-------------
TaskCreated
TaskCompleted
ProjectUpdated
```

Allow developers to inspect event payloads.

---

# Cross-Cutting Technical Requirements

## Logging

All major infrastructure features should use structured logging.

Log categories:

```text
Task
Import
Export
Backup
Sync
Webhook
Plugin
API
Database
Automation
```

Avoid logging secrets or sensitive task content.

---

# Error Handling

All user-visible operations should have controlled failure behavior.

Examples:

```text
Import failed
Backup failed
Webhook failed
Plugin failed
Database check failed
API request failed
```

Errors should include:

- User-friendly message
- Technical detail in diagnostics
- Correlation/request ID where useful
- Recovery action

---

# Testing Strategy

Every feature should include tests at multiple levels.

## Unit Tests

Test:

- Business rules
- Parsers
- Calculations
- Workflow transitions
- Risk scoring
- Capacity calculations
- Duplicate detection
- Validation

## Integration Tests

Test:

- EF Core
- SQLite
- Backup/restore
- Import/export
- Webhooks
- Local API
- Event Bus

## UI Tests

Test:

- Critical workflows
- Keyboard shortcuts
- Command Palette
- Import wizard
- Backup restore
- Workspace switching
- Privacy mode

---

# Database Migration Strategy

New entities will likely include:

```text
InboxItem
TaskHistory
TaskVersion
TaskRelationship
Goal
Milestone
Decision
JournalEntry
WorkSession
Distraction
BackupMetadata
Profile
CustomFieldDefinition
CustomFieldValue
TaskType
Workflow
WorkflowStatus
WorkflowTransition
SavedView
WorkspaceTemplate
ProjectTemplate
Webhook
Plugin
ApplicationEvent
```

Do not introduce database schema changes without:

1. EF migration
2. Migration test
3. Upgrade test from previous schema
4. Fresh database test
5. Backup/restore test

---

# Feature Dependency Map

## Capture and task foundation

```text
39 Inbox
   ↓
41 Quick Add
   ↓
42 History
   ↓
43 Undo/Redo
   ↓
44 Versioning
   ↓
46 Trash
```

## Planning

```text
49 Goals
   ↓
50 Milestones
   ↓
51 Project Health
   ↓
52 Deadline Risk
   ↓
53 Workload
   ↓
54 Capacity
   ↓
55 Estimation
   ↓
56 Cost
```

## Productivity

```text
58 Meeting Mode
   ↓
59 Action Extraction

60 Journal
61 Activity Timeline
63 Contexts
64 Distractions
65 Work Sessions
```

## Reliability

```text
67 Backup
   ↓
68 Restore Verification
   ↓
69 Database Maintenance
   ↓
70 Integrity Checker
```

## Customization

```text
80 Custom Fields
   ↓
81 Task Types
   ↓
82 Workflows
   ↓
83 Saved Views
   ↓
84 View Templates
   ↓
85 Workspace Templates
   ↓
86 Project Starter Kits
   ↓
87 Recurring Project Templates
```

## Developer platform

```text
94 Widget Architecture
        ↓
98 Event Bus
        ↓
96 Webhooks
        ↓
97 Local REST API
        ↓
99 CLI
        ↓
100 API Explorer
        ↓
95 Plugin SDK
```

---

# Recommended Implementation Order

The numeric order is useful for documentation, but it is not necessarily the best technical implementation order.

## Stage 1 — Core Reliability and Data Infrastructure

Implement:

```text
42 Task History
43 Undo/Redo
46 Trash
44 Versioning
67 Backup
68 Restore Simulator
70 Integrity Checker
```

These reduce the risk of adding many features on top of unstable data behavior.

**Progress: 46 (Trash) delivered 2026-08-26; 42 (Task History) delivered 2026-08-27; 43 (Undo/Redo), 44 (Task Versioning), 67 (Local Backup Manager), 68 (Backup Restore Simulator), 69 (Database Maintenance Center) and 70 (Data Integrity Checker) delivered 2026-09-01 — Stage 1 (Core Reliability and Data Infrastructure) is now fully delivered.**

**Also delivered 2026-09-01, outside Stage 1's own list: 39 (Task Inbox), 45 (Archive Vault), 47 (Smart Duplicate Detection), 61 (Activity Timeline), 65 (Work Session History) — see each feature's section above.**

---

## Stage 2 — Capture and Power User Experience

Implement:

```text
39 Inbox
40 Command Palette
41 Quick Add
47 Duplicate Detection
77 Shortcut Manager
83 Saved Views
88 Bulk Edit
```

This makes the existing application dramatically faster to use.

---

## Stage 3 — Planning Engine

Implement:

```text
49 Goals
50 Milestones
51 Project Health
52 Deadline Risk
53 Workload Heatmap
54 Capacity Planning
55 Estimation Accuracy
56 Cost Tracking
```

This creates the application's advanced planning capabilities.

---

## Stage 4 — Productivity Intelligence

Implement:

```text
58 Meeting Mode
59 Meeting Action Extractor
60 Journal
61 Activity Timeline
63 Focus Contexts
64 Distraction Log
65 Work Session History
```

---

## Stage 5 — Customization

Implement:

```text
80 Custom Fields
81 Custom Task Types
82 Custom Workflows
83 Saved Views
84 View Templates
85 Workspace Templates
86 Project Starter Kits
87 Recurring Project Templates
93 Custom Dashboard
```

---

## Stage 6 — Privacy and Administration

Implement:

```text
71 Portable Workspace
72 Multiple Profiles
73 Presentation Mode
74 Workspace Lock
75 Privacy Mode
76 Sensitive Data Detection
69 Database Maintenance
```

---

## Stage 7 — Data Migration and Reporting

Implement:

```text
89 Mass Import
90 Migration Center
91 Export Profiles
92 Print Layout Designer
```

---

## Stage 8 — Developer Platform

Implement:

```text
98 Event Bus
94 Widget Architecture
96 Webhook Engine
97 Local REST API
99 CLI
100 API Explorer
95 Plugin SDK
```

The Event Bus should be built before Webhooks/Plugins because those systems can consume application events.

---

# Suggested Core Interfaces

The following abstractions will help keep the architecture extensible.

```text
IEventBus
IApplicationEvent
ICommand
ICommandHandler
IUndoRedoService
IAuditService
IVersioningService
IBackupService
IRestoreService
IDataIntegrityChecker
IQuickAddParser
IDuplicateDetector
IRiskCalculator
ICapacityCalculator
IWorkflowEngine
ITemplateService
IImportProvider
IExportProvider
IWebhookService
IPluginManager
IWidgetRegistry
```

---

# Important Design Rule — Keep AI Separate

AI should not become a dependency of the core application.

The architecture should look like:

```text
                +----------------------+
                |      AI Features     |
                +----------+-----------+
                           |
                    AI Adapter Layer
                           |
                +----------v-----------+
                | Application Services |
                +----------------------+
```

For example:

```text
NaturalLanguageTaskParser
        ↓
TaskDraft
        ↓
TaskService
```

The AI parser can later be replaced with:

```text
OpenAI
Local LLM
Ollama
Other provider
```

without modifying TaskService.

The same approach should be used for:

- Meeting Action Extraction
- Duplicate Detection
- Deadline Risk enhancement
- Productivity recommendations
- Future AI assistant functionality

> **Note:** this is the same principle DeskTodo's own Phase 34 (AI features) planning notes already committed to — an `IQuickAddParser`/adapter-layer split, not a direct `TaskService` → AI-provider dependency.

---

# Definition of Done

A feature should not be marked complete simply because its UI exists.

Each feature should satisfy:

- [ ] Domain model implemented where required
- [ ] Application service implemented
- [ ] Repository abstraction implemented where required
- [ ] Infrastructure implementation completed
- [ ] Database migration completed
- [ ] DI registration completed
- [ ] UI implemented
- [ ] Validation implemented
- [ ] Error handling implemented
- [ ] Logging implemented
- [ ] Unit tests completed
- [ ] Integration tests completed where applicable
- [ ] UI/functional tests completed where applicable
- [ ] Import/export compatibility considered
- [ ] Backup/restore compatibility considered
- [ ] Accessibility/keyboard behavior considered
- [ ] Windows behavior verified
- [ ] macOS behavior verified
- [ ] Documentation updated

---

# Final Product Architecture

After completing these phases, the application should evolve from a task manager into a broader desktop productivity platform:

```text
                           PRODUCTIVITY PLATFORM
                                    |
       +----------------------------+----------------------------+
       |                            |                            |
   Productivity                Planning                    Organization
       |                            |                            |
   Tasks                        Goals                       Projects
   Inbox                        Milestones                  Workspaces
   Focus                        Capacity                    Templates
   Journal                      Workload                    Views
   Meetings                     Risk                        Custom Fields
       |                            |                            |
       +----------------------------+----------------------------+
                                    |
                              Core Platform
                                    |
             +----------------------+----------------------+
             |                      |                      |
          Events                Commands               Automation
             |                      |                      |
          Audit                 Undo/Redo              Webhooks
          Activity              Macros                 Notifications
             |                      |                      |
             +----------------------+----------------------+
                                    |
                            Developer Platform
                                    |
            +-----------------------+-----------------------+
            |                       |                       |
        Local REST API             CLI                  Plugins
            |                       |                       |
        API Explorer           Terminal                Widgets
                                    |
                               Integrations
```

The long-term goal should therefore be more than:

> "A desktop task manager."

It can become:

> **A local-first, extensible productivity operating system for tasks, projects, planning, automation, and developer integrations.**

---

# Recommended Priority Summary

## Must Build

```text
39  Task Inbox
40  Command Palette
42  Task History
43  Undo/Redo
46  Trash
49  Goals
50  Milestones
51  Project Health
52  Deadline Risk
53  Workload Heatmap
54  Capacity Planning
61  Activity Timeline
65  Work Session History
67  Backup
68  Restore Simulator
70  Integrity Checker
80  Custom Fields
81  Custom Task Types
82  Custom Workflows
83  Saved Views
88  Bulk Edit
89  Import Wizard
90  Migration Center
93  Dashboard Builder
96  Webhooks
97  Local REST API
98  Event Bus
```

## Should Build

```text
41  Natural Language Quick Add
44  Task Versioning
45  Archive Vault
47  Duplicate Detection
48  Relationship Graph
55  Estimation Accuracy
57  Decision Log
58  Meeting Mode
59  Meeting Action Extractor
60  Journal
63  Focus Contexts
64  Distraction Log
71  Portable Workspace
72  Multiple Profiles
73  Presentation Mode
74  Workspace Lock
75  Privacy Mode
77  Shortcut Manager
79  Macro Recorder
85  Workspace Templates
86  Project Starter Kits
87  Recurring Project Templates
91  Export Profiles
92  Print Designer
99  CLI
100 Developer API Explorer
```

## Future / Advanced

```text
62  Achievement System
66  Offline Conflict Resolver
76  Sensitive Data Detector
78  Mouse Gestures
84  View Sharing Templates
94  Widget Marketplace Architecture
95  Plugin SDK
```

---

# Final Recommendation

Do not start Feature 95 Plugin SDK immediately.

The strongest technical sequence is:

```text
Event Bus
    ↓
Command System
    ↓
Audit / History
    ↓
Workflow Engine
    ↓
Template Engine
    ↓
Widget Architecture
    ↓
Local REST API
    ↓
Webhooks
    ↓
CLI
    ↓
Developer API Explorer
    ↓
Plugin SDK
```

Once this foundation exists, your future integrations and AI functionality can be implemented as extensions instead of repeatedly modifying the core application.

That will make the application much easier to maintain as the feature set grows beyond the current 100-phase roadmap.
