# DeskTodo - Product Roadmap

> A cross-platform desktop productivity application for Windows & macOS built with .NET.

> **Status key:** `[x]` shipped (see IMPLEMENTATION.md for which phase). `[ ]`
> not built. Checked/unchecked below reflects the state as of Phase 16
> completing — cross-referenced item by item against IMPLEMENTATION.md, not
> assumed. Everything still `[ ]` here has been carried into
> IMPLEMENTATION.md's "Extended Roadmap (Phase 17+)" as a planned,
> not-yet-started item.

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
- [ ] Tags
- [ ] Labels
- [ ] Task Color _(the field exists on the entity; no UI to set a custom per-task color)_
- [ ] Subtasks
- [ ] Checklists
- [x] Task Notes
- [ ] Rich Text Notes
- [ ] Attachments
- [ ] Recurring Tasks
- [ ] Task Dependencies
- [x] Archive Tasks
- [ ] Favorite Tasks _(distinct from Pin, which is built)_
- [x] Duplicate Task
- [ ] Task Templates
- [x] Pin Tasks
- [x] Search Tasks
- [x] Advanced Filters _(status/category filters; no saved filter presets or multi-criteria combos yet)_
- [x] Sorting
- [ ] Group By
- [ ] Bulk Edit _(bulk complete/delete exist; no bulk field edit, e.g. reassign category for N selected tasks)_
- [x] Multi Select
- [x] Drag & Drop
- [ ] Recently Viewed

---

# 📅 Planning

- [x] Daily Planner
- [ ] Weekly Planner
- [ ] Monthly Planner
- [ ] Year Planner
- [ ] Calendar View _(a date-picker to jump to a day exists; no month-grid view)_
- [ ] Agenda View
- [ ] Timeline View
- [ ] Kanban Board
- [ ] Eisenhower Matrix
- [ ] Goal Planner
- [ ] Milestones
- [ ] Sprint Planner
- [ ] Roadmap View

---

# 📊 Spreadsheet / Grid View

- [ ] Excel-like Grid
- [ ] Inline Editing _(inline title-rename exists in the list view; no grid)_
- [ ] Copy Rows
- [ ] Paste Rows
- [ ] Copy from Excel
- [ ] Paste from Excel
- [ ] Bulk Update
- [ ] Multi Row Selection
- [ ] Custom Columns
- [ ] Hide Columns
- [ ] Freeze Columns
- [ ] Resize Columns
- [ ] Reorder Columns
- [ ] Saved Views
- [ ] Filters _(list-view filters are built; grid-specific column filters are not)_
- [ ] Progress Column
- [ ] Status Column
- [x] Import CSV
- [ ] Import Excel _(Excel export exists; import is CSV/JSON only)_
- [x] Export CSV
- [x] Export Excel

---

# 🖥 Desktop Features

- [x] Always On Top Widget
- [x] Floating Widget
- [x] Transparent Widget
- [ ] Mini Widget
- [ ] Sticky Notes
- [ ] Desktop Notes
- [x] Desktop Reminder Popup _(native OS notifications)_
- [ ] System Tray
- [ ] macOS Menu Bar
- [ ] Global Shortcut
- [ ] Quick Add Window
- [x] Auto Start
- [ ] Minimize to Tray
- [ ] Multi Monitor Support _(no explicit monitor-choice feature; relies on default OS/toolkit placement)_
- [x] Native Notifications

---

# ⏰ Productivity

- [ ] Pomodoro Timer
- [ ] Stopwatch
- [ ] Focus Timer
- [ ] Focus Mode
- [ ] Deep Work Session
- [ ] Break Reminder
- [ ] Water Reminder
- [ ] Stretch Reminder
- [ ] Daily Goals
- [ ] Weekly Goals
- [ ] Monthly Goals
- [ ] Habit Tracker
- [ ] Time Tracking
- [x] Estimated Time
- [ ] Actual Time _(field exists on the entity; no UI to record it)_
- [ ] Productivity Score

---

# 📈 Analytics

- [ ] Dashboard
- [x] Today's Progress _(the widget's completed/total progress bar)_
- [ ] Weekly Progress
- [ ] Monthly Progress
- [ ] Productivity Score
- [ ] Completion Rate
- [ ] Focus Time
- [ ] Time Per Project
- [ ] Category Analytics
- [ ] Heat Map
- [ ] Weekly Report
- [ ] Monthly Report
- [ ] Streak Counter

---

# 📁 Organization

- [ ] Projects
- [ ] Workspaces
- [ ] Lists
- [ ] Folders
- [ ] Sections
- [ ] Smart Lists
- [ ] Saved Searches
- [ ] Favorites
- [ ] Bookmarks

---

# 🔔 Reminders

- [x] One Time Reminder _(overdue-task alert)_
- [ ] Recurring Reminder _(needs Recurring Tasks first)_
- [x] Desktop Notification
- [ ] Sound Notification _(relies on OS default notification sound; no custom sound)_
- [ ] Snooze
- [ ] Reminder History
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

- [ ] Command Palette
- [ ] Keyboard Shortcuts _(app-wide; only per-field Enter/Escape exist today)_
- [x] Quick Search _(in-app search bar; no global hotkey)_
- [ ] Quick Add _(an inline add-task box exists in the widget; no standalone global quick-add popup)_
- [ ] Undo / Redo
- [ ] Clipboard History
- [ ] Activity Log
- [x] Batch Actions _(bulk complete/delete on a multi-selection)_
- [ ] Task Templates

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
- [ ] Database Encryption
- [ ] Password Lock
- [ ] PIN Lock
- [ ] Windows Hello
- [ ] Touch ID
- [ ] Face ID
- [ ] Secure Backup
- [ ] Auto Lock

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
- [ ] Excel-style Grid
- [ ] Calendar View
- [x] Daily Planner
- [x] Always-on-Top Widget
- [x] SQLite Storage
- [x] Search & Filters
- [x] Desktop Notifications
- [ ] Dark Theme
- [x] Auto Start
- [ ] Global Shortcut
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
- [ ] ✅ Checklist tracking
- [ ] 📊 Excel-style task management
- [x] 💬 Notes _(the "comments" half — a discussion thread on a task — is not built)_
- [x] ⏰ Reminders
- [ ] 🖥️ Native Windows & macOS experience _(macOS notifications/auto-start are verified live; the Windows equivalents are authored but not runtime-verified — no Windows machine in this dev environment)_
- [ ] Alert when a task is overdue and move it to the next/a different day automatically
- [ ] App version display + update-available prompt; updating must never delete existing data
- [ ] Nicer UI polish (a "Bootstrap-like" free component/styling pass)
- [ ] Send a task to another user, who can accept or reject it
- [ ] Group tasks (shared, multi-user task groups)
- [ ] User profile concept (accounts/identity)
