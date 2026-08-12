# DeskTodo - Product Roadmap

> A cross-platform desktop productivity application for Windows & macOS built with .NET.

> **Status key:** `[x]` shipped (see IMPLEMENTATION.md for which phase). `[ ]`
> not built. Checked/unchecked below reflects the state as of Phase 29
> completing (2026-08-12) — Phase 27 was explicitly skipped for this pass
> (see IMPLEMENTATION.md's own Phase 27 section), Phases 28–29 both
> shipped scoped down — cross-referenced item by item against
> IMPLEMENTATION.md, not assumed; several `[x]` items note a partial
> scope inline (see IMPLEMENTATION.md's Phase 17–29 sections for the full
> detail on what shipped vs. what's still deliberately out of scope).
> Everything still `[ ]` here has been carried into IMPLEMENTATION.md's
> "Extended Roadmap" as a planned, not-yet-started item (Phase 27 and
> Phases 30–37).

---

# 📋 Core Task Management

- [x] Create Task
- [x] Edit Task
- [x] Delete Task
- [x] Complete Task
- [x] Undo Complete
- [ ] Start Date
- [x] Due Date
- [ ] Due Time _(the due-date picker is date-only; no time-of-day component in the UI yet)_
- [x] Priority (Low, Medium, High, Urgent) _(built as Low/Medium/High/Critical)_
- [x] Categories
- [x] Tags _(free-form, many-to-many, added/removed as chips in the full-field editor)_
- [x] Labels _(same feature as Tags — the wishlist lists them separately, shipped as one)_
- [x] Task Color _(an 8-swatch palette + "none" in the full-field editor; the widget row's dot shows it when set)_
- [x] Subtasks _(a single-level parent/child relationship — a subtask having its own subtasks isn't offered at the UI layer)_
- [x] Checklists
- [x] Task Notes
- [x] Rich Text Notes _(a hand-rolled minimal Markdown preview — bold/italic/bullet lines — not a general Markdown parser)_
- [x] Attachments _(files copied into app storage, 20 MB cap; attach/open/remove in the full-field editor)_
- [x] Recurring Tasks _(Daily/Weekly/Monthly + interval + optional end date; completing a recurring task creates its next occurrence)_
- [x] Task Dependencies _(a "Blocked by" picker + a completion guard; deep transitive cycles — A blocks B blocks C blocks A — aren't detected, only direct two-task cycles)_
- [x] Archive Tasks
- [x] Favorite Tasks _(distinct from Pin — a second ⭐ flag, toggled from the row context menu)_
- [x] Duplicate Task
- [x] Task Templates _(save a task's shape — incl. checklist — as a named template; "New from template" in the widget's add-task row; seeded with 7 starter templates, one per built-in category, so the picker isn't empty on a fresh install)_
- [x] Pin Tasks
- [x] Search Tasks
- [x] Advanced Filters _(status/category/tag filters; no saved filter presets or multi-criteria combos yet)_
- [x] Sorting
- [x] Group By _(shipped as a "sort by Category" mode that visually clusters same-category rows, not a separate grouped-list UI with header rows)_
- [ ] Bulk Edit _(bulk complete/delete exist; no bulk field edit, e.g. reassign category for N selected tasks)_
- [x] Multi Select
- [x] Drag & Drop
- [x] Recently Viewed _(session-only — resets on restart, never persisted)_

---

# 📅 Planning

- [x] Daily Planner
- [x] Weekly Planner _(a "Week" tab in the new Planner window — seven day cells with per-day task counts)_
- [x] Monthly Planner _(the month-grid Calendar View below doubles as this — a month grid already shows a full month's shape)_
- [x] Year Planner _(a "Year" tab: 12 month tiles, each a task-count summary rather than a mini-calendar)_
- [x] Calendar View _(a real month-grid calendar — fixed 7x6 cells, a completion/task-count indicator per day)_
- [x] Agenda View _(an "Agenda" tab: incomplete tasks across the next 14 days, grouped by date)_
- [x] Timeline View _(a "Timeline" tab: every incomplete task with a due date, chronologically — a plain list, not a proportionally-drawn axis)_
- [x] Kanban Board _(a "Kanban" tab: To Do/Done columns reusing IsCompleted, not a new status concept — click to move, not drag-and-drop)_
- [x] Eisenhower Matrix _(a "Matrix" tab: 2x2 grid derived from Priority + due-date proximity, no new persistence)_
- [x] Goal Planner _(a "Goals" tab: personal, ongoing habit-style targets tracked by a computed daily streak)_
- [x] Milestones _(a "Milestones" tab: a target-date deliverable tasks can optionally link to, with progress shown as linked-task completion)_
- [x] Sprint Planner _(served by the same Milestones tab — near-term milestones at the top read as "what's coming up next")_
- [x] Roadmap View _(also served by the same Milestones tab — the full chronological list reads as the project's timeline)_

---

# 📊 Spreadsheet / Grid View

- [x] Excel-like Grid _(a separate "All Tasks" grid window, opened from the widget header, over every non-archived task across every day)_
- [x] Inline Editing _(title/notes text, date pickers, priority/category dropdowns, a completed checkbox — each cell persists immediately)_
- [ ] Copy Rows
- [ ] Paste Rows
- [x] Copy from Excel _(TSV clipboard interop — pastes a real Excel range's tab-separated cells into the grid as new tasks)_
- [x] Paste from Excel _(same TSV interop, both directions)_
- [x] Bulk Update _("Delete Selected" across a multi-selection; no bulk field edit — e.g. reassign category for N rows — yet)_
- [x] Multi Row Selection _(a per-row selection checkbox column, not the grid's native selection — Avalonia's DataGrid doesn't expose a two-way-bindable SelectedItems)_
- [ ] Custom Columns
- [x] Hide Columns _(a "Columns" flyout toggle, persisted across sessions)_
- [x] Freeze Columns _(a "Freeze checkbox + Title columns" toggle in the Columns flyout, persisted; still a fixed 2-column freeze point, not an arbitrary user-chosen one)_
- [x] Resize Columns _(the DataGrid control's own built-in behavior)_
- [x] Reorder Columns _(same — built-in)_
- [x] Saved Views _(a "Views" flyout: save the grid's current hidden-column set under a name, apply or delete it later — column widths/order still aren't captured, only visibility)_
- [ ] Filters _(list-view filters are built; grid-specific column filters are not)_
- [x] Progress Column _(checklist completion, "checked/total" — "—" for a task with no checklist)_
- [x] Status Column _(derived: Done / Overdue / Due Today / Upcoming / No due date)_
- [x] Import CSV
- [ ] Import Excel _(Excel export exists; import is CSV/JSON only)_
- [x] Export CSV
- [x] Export Excel

---

# 🖥 Desktop Features

- [x] Always On Top Widget
- [x] Floating Widget
- [x] Transparent Widget
- [x] Mini Widget
- [ ] Sticky Notes
- [ ] Desktop Notes
- [x] Desktop Reminder Popup _(native OS notifications)_
- [x] System Tray
- [x] macOS Menu Bar
- [x] Global Shortcut
- [x] Quick Add Window
- [x] Auto Start
- [x] Minimize to Tray
- [x] Multi Monitor Support
- [x] Native Notifications

---

# ⏰ Productivity

- [x] Pomodoro Timer
- [x] Stopwatch
- [x] Focus Timer _(a CountdownTimer preset — see FocusSessionType)_
- [x] Focus Mode _(same CountdownTimer preset as Focus Timer)_
- [x] Deep Work Session _(same CountdownTimer preset, longer default length)_
- [x] Break Reminder
- [x] Water Reminder
- [x] Stretch Reminder
- [x] Daily Goals _(satisfied by Phase 21's Goal — daily-streak habit tracking)_
- [ ] Weekly Goals _(Goal's streak logic assumes a daily cadence; no 3×/week-style tracking yet)_
- [ ] Monthly Goals _(same gap as Weekly Goals)_
- [x] Habit Tracker _(Phase 21's Goal entity; see the Daily Goals note)_
- [x] Time Tracking
- [x] Estimated Time
- [x] Actual Time _(now written by completed Focus Timer sessions, and shown in the full-field editor)_
- [ ] Productivity Score

---

# 📈 Analytics

- [x] Dashboard
- [x] Today's Progress _(the widget's completed/total progress bar)_
- [x] Weekly Progress
- [x] Monthly Progress
- [ ] Productivity Score
- [x] Completion Rate
- [x] Focus Time
- [x] Time Per Project _(delivered as Time Per Category — "Project" is Phase 25's concept, which doesn't exist yet)_
- [x] Category Analytics
- [x] Heat Map
- [x] Weekly Report
- [x] Monthly Report
- [x] Streak Counter

---

# 📁 Organization

- [x] Projects
- [ ] Workspaces _(deferred — a full separate task-space silo is a much larger structural change than this single-user, single-database app currently supports; no honest partial version exists)_
- [x] Lists _(satisfied by Projects — a Project's linked tasks ARE a list, not a second concept)_
- [ ] Folders _(deferred — nested Project hierarchy; no precedent for general hierarchy anywhere in the domain, only `TaskItem`'s single-level, deliberately-not-a-tree subtask relationship)_
- [ ] Sections _(deferred — sub-grouping headers within a project's list; real UI complexity for low value at this stage)_
- [x] Smart Lists _(Favorites/Pinned/Overdue/Due Today/High Priority/No Project — a computed quick filter in the grid view, the natural home since it already spans every day)_
- [x] Saved Searches _(unified with the grid's existing "saved column views" into one named preset, rather than a second parallel concept)_
- [x] Favorites _(the underlying `TaskItem.IsFavorite` flag already existed; this phase added its missing cross-day "view them all" UI)_
- [x] Bookmarks _(same story as Favorites, via `TaskItem.IsPinned`)_

---

# 🔔 Reminders

- [x] One Time Reminder _(overdue-task alert)_
- [x] Recurring Reminder _(satisfied by `Type = Reminder` + Phase 19's `RecurrenceFrequency` — each occurrence is its own `TaskItem` with its own overdue notification, no new mechanism needed)_
- [x] Desktop Notification
- [x] Sound Notification _(a Settings toggle controls it; macOS maps to `display notification`'s `sound name` clause, Windows' balloon tip always plays the OS default with no documented way to suppress just the sound — a documented platform gap, not silently ignored)_
- [x] Snooze _("Snooze 1 hour" on any overdue task row's context menu)_
- [ ] Reminder History _(deferred — needs a new entity/repository/service/migration/UI surface; left for a future pass rather than shipped half-built)_
- [x] Missed Reminder Alert _(overdue-task alert)_

---

# 🎨 Appearance

- [ ] Light Theme _(the app is light by default, but it's hardcoded, not a switchable theme)_
- [ ] Dark Theme
- [ ] System Theme
- [x] Custom Accent Color
- [ ] Custom Font Size
- [ ] Compact Mode
- [ ] Zoom
- [ ] Animations
- [ ] Responsive Layout _(the window is resizable; true responsive reflow at all sizes isn't a deliberate design goal yet)_

---

# ⚡ Power User Features

- [x] Command Palette _(Cmd/Ctrl+K — a searchable list of every WidgetWindow header-icon action, live-verified end-to-end)_
- [x] Keyboard Shortcuts _(app-wide Cmd/Ctrl+K/F/, added; not exhaustive — a starting set, not every conceivable binding)_
- [x] Quick Search _(in-app search bar; no global hotkey)_
- [x] Quick Add _(standalone Quick Add window, summoned from the tray or the Cmd/Ctrl+Shift+N global shortcut)_
- [ ] Undo / Redo _(deferred — an architecturally significant command-stack pattern shift touching nearly every mutating operation; needs its own dedicated pass)_
- [ ] Clipboard History _(deferred — needs OS clipboard-change monitoring with no existing precedent in this codebase)_
- [ ] Activity Log _(deferred — overlaps with Phase 26's already-deferred Reminder History; better designed as one shared piece of infrastructure for both in a future pass)_
- [x] Batch Actions _(bulk complete/delete on a multi-selection)_
- [x] Task Templates _(same feature already listed under Core Task Management, above)_

---

# 🤖 AI Features (Pro)

- [ ] AI Task Creation
- [ ] AI Break Large Tasks
- [ ] AI Daily Planner
- [ ] AI Weekly Planner
- [ ] AI Priority Suggestions
- [ ] AI Time Estimation
- [ ] AI Smart Schedule
- [ ] AI Meeting Summary
- [ ] AI Note Summary
- [ ] AI Rewrite Notes
- [ ] AI Productivity Coach
- [ ] AI Goal Suggestions

---

# ☁ Cloud Features (Pro)

- [ ] Cloud Sync
- [ ] Multi Device Sync
- [ ] Auto Backup
- [ ] Restore Backup
- [ ] Version History
- [ ] Offline Sync
- [ ] Conflict Resolution

---

# 👥 Team Features

- [ ] Shared Projects
- [ ] Shared Tasks
- [ ] Assign Tasks
- [ ] Team Dashboard
- [ ] Activity Feed
- [ ] Comments
- [ ] Mentions
- [ ] File Sharing
- [ ] Permissions

---

# 🔗 Integrations

## Calendar

- [ ] Google Calendar
- [ ] Outlook Calendar

## Project Management

- [ ] Microsoft To Do Import
- [ ] Todoist Import
- [ ] Trello Import
- [ ] Notion Import
- [ ] Jira
- [ ] Azure DevOps

## Development

- [ ] GitHub Issues
- [ ] GitLab Issues

## Communication

- [ ] Slack
- [ ] Microsoft Teams
- [ ] Discord

## Cloud Storage

- [ ] OneDrive
- [ ] Google Drive
- [ ] Dropbox

---

# 📤 Import / Export

- [x] Excel _(export only)_
- [x] CSV
- [x] JSON
- [x] Markdown _(export only)_
- [ ] PDF
- [ ] HTML
- [ ] Backup File _(no dedicated backup format/flow, distinct from a plain task export)_
- [ ] Restore File

---

# 🔐 Security

- [x] Local SQLite Database
- [ ] Database Encryption _(deferred — switching the EF Core SQLite provider to a SQLCipher-supporting variant is a meaningful infra change with real migration risk; deserves its own dedicated pass)_
- [x] Password Lock _(satisfied by PIN Lock below — the field isn't digit-only, so it works as a short passphrase gate too)_
- [x] PIN Lock _(a new lock screen shown at startup when enabled, PBKDF2-hashed, live-verified to correctly block/show the widget and refuse a bypass via the OS close button)_
- [ ] Windows Hello _(deferred — needs a signed app bundle identity, the same Phase 16 packaging prerequisite already flagged for Phase 26's richer macOS notification API)_
- [ ] Touch ID _(deferred, same reasoning as Windows Hello — plus no way to exercise real biometric hardware in this dev environment)_
- [ ] Face ID _(not a macOS/Windows desktop API — this wishlist item doesn't apply to this app's platforms)_
- [ ] Secure Backup _(deferred — a natural extension of Phase 14's export/import pattern, but for a new full-state format; real design work of its own)_
- [ ] Auto Lock _(deferred — a genuine idle-timeout re-lock needs real OS-level idle detection, a separate concern from PIN verification itself)_

---

# 💡 Unique Features

- [ ] Floating Daily Planner
- [ ] Smart Desktop Widget
- [ ] Smart Clipboard Detection
- [ ] Screenshot to Task
- [ ] OCR Image to Task
- [ ] Voice to Task
- [ ] Email to Task
- [ ] Drag File to Create Task
- [ ] Drag Browser Tab to Create Task
- [ ] Smart Daily Briefing
- [ ] End of Day Summary
- [ ] Morning Planning Assistant
- [ ] AI Workload Prediction
- [ ] AI Smart Reschedule

---

# 👨‍💻 Developer Mode

- [ ] GitHub Dashboard
- [ ] Azure DevOps Dashboard
- [ ] Jira Sprint Board
- [ ] Pull Request Reminder
- [ ] Code Review Reminder
- [ ] Build Status Widget
- [ ] Release Tracker
- [ ] Bug Tracker

---

# 💎 Premium Features

- [ ] AI Assistant
- [ ] Cloud Sync
- [ ] Multi Device Sync
- [ ] Unlimited Projects
- [ ] Unlimited Workspaces
- [ ] Advanced Analytics
- [ ] Premium Themes
- [ ] Voice Commands
- [ ] OCR
- [ ] Calendar Sync
- [ ] Team Collaboration
- [ ] Automatic Backup
- [ ] Version History
- [ ] Priority Support

---

# 🚀 Future Ideas

- [ ] Mobile App (Android)
- [ ] Mobile App (iPhone)
- [ ] Apple Watch App
- [ ] Windows Widget
- [ ] macOS Widget
- [ ] Browser Extension
- [ ] Chrome Extension
- [ ] Outlook Add-in
- [ ] VS Code Extension
- [ ] AI Chat Assistant
- [ ] Personal Knowledge Base
- [ ] Document Manager
- [ ] Expense Tracker
- [ ] Time Billing
- [ ] Invoice Generator
- [ ] CRM Lite

---

# 🏆 MVP (Version 1)

- [x] Task Management
- [x] Excel-style Grid
- [x] Calendar View
- [x] Daily Planner
- [x] Always-on-Top Widget
- [x] SQLite Storage
- [x] Search & Filters
- [x] Desktop Notifications
- [ ] Dark Theme
- [x] Auto Start
- [x] Global Shortcut
- [x] CSV Import/Export

---

# 🎯 Version 2

- [ ] AI Features
- [ ] Cloud Sync
- [ ] Multi-device Sync
- [ ] Time Tracking
- [ ] Analytics Dashboard
- [ ] Voice Input
- [ ] Screenshot to Task
- [ ] Calendar Integration
- [ ] OCR
- [ ] Productivity Reports

---

# 🌟 Version 3

- [ ] Team Collaboration
- [ ] GitHub Integration
- [ ] Azure DevOps Integration
- [ ] Jira Integration
- [ ] Mobile Apps
- [ ] Browser Extension
- [ ] AI Assistant
- [ ] Smart Automation

---

# 📝 "Later" notes (carried in from an earlier pass)

Originally left as freeform notes ("add below point in implementation note
to do later, do not change current implementation plan"). Now formally
tracked in IMPLEMENTATION.md's Extended Roadmap instead of sitting here as
prose:

- [x] 📌 Always-on-top desktop widget
- [x] 📅 Date-wise planning
- [x] ✅ Checklist tracking
- [x] 📊 Excel-style task management _(the grid window — see the Spreadsheet / Grid View section above for exactly what did/didn't ship)_
- [x] 💬 Notes _(the "comments" half — a discussion thread on a task — is not built)_
- [x] ⏰ Reminders
- [ ] 🖥️ Native Windows & macOS experience _(macOS notifications/auto-start are verified live; the Windows equivalents are authored but not runtime-verified — no Windows machine in this dev environment)_
- [x] Alert when a task is overdue and move it to the next/a different day automatically _(the existing overdue notification, plus a new opt-in "Auto-reschedule overdue tasks" Settings toggle)_
- [ ] App version display + update-available prompt; updating must never delete existing data
- [ ] Nicer UI polish (a "Bootstrap-like" free component/styling pass)
- [ ] Send a task to another user, who can accept or reject it
- [ ] Group tasks (shared, multi-user task groups)
- [ ] User profile concept (accounts/identity)
