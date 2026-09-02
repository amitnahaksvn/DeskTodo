using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DeskTodo.App.Views;

public partial class WidgetWindow : Window
{
    // Mini Widget mode's compact size (Phase 22) — small enough to be a genuine "minimal
    // desktop footprint," tall enough to still fit the header row and footer progress bar
    // without clipping.
    private const double MiniModeHeight = 148;
    private const double MiniModeMinHeight = 120;
    private const double DefaultMinHeight = 360;
    private const double DefaultHeight = 560;

    // Tracks the row being dragged for reordering. A private field (rather than routing
    // the task's Guid through Avalonia's IDataTransfer/DataFormat payload machinery) is
    // enough because this drag never leaves the window it started in — DoDragDropAsync is
    // still used for the actual gesture (visual feedback, DragOver/Drop routing), just not
    // for carrying the payload.
    private Guid? _draggedTaskId;

    // Remembers the window's height from just before entering Mini Widget mode, so toggling
    // back out restores it exactly rather than snapping to a fixed default.
    private double? _preMiniModeHeight;

    // Phase 28's Clipboard History poll. A separate timer from WidgetViewModel's own
    // 30-second _dayRolloverTimer rather than piggybacking on its Tick event, because
    // reading the clipboard needs a live TopLevel/IClipboard — an Avalonia dependency
    // WidgetViewModel deliberately doesn't have (see its doc comments elsewhere). Matches
    // that timer's cadence so this doesn't introduce a *new* polling rhythm, just a second
    // instance of the app's existing one for a need that has to live in code-behind.
    private DispatcherTimer? _clipboardPollTimer;
    private string? _lastSeenClipboardText;

    public WidgetWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is WidgetViewModel viewModel)
        {
            viewModel.TaskEditRequested += OnTaskEditRequested;
            viewModel.SettingsRequested += OnSettingsRequested;
            viewModel.GridViewRequested += OnGridViewRequested;
            viewModel.CalendarViewRequested += OnCalendarViewRequested;
            viewModel.PlannerViewRequested += OnPlannerViewRequested;
            viewModel.FocusTimerRequested += OnFocusTimerRequested;
            viewModel.AnalyticsRequested += OnAnalyticsRequested;
            viewModel.CommandPaletteRequested += OnCommandPaletteRequested;
            viewModel.ClipboardHistoryRequested += OnClipboardHistoryRequested;
            viewModel.TaskGroupsRequested += OnTaskGroupsRequested;
            viewModel.TrashRequested += OnTrashRequested;
            viewModel.BackupRequested += OnBackupRequested;
            viewModel.IntegrityCheckRequested += OnIntegrityCheckRequested;
            viewModel.InboxRequested += OnInboxRequested;
            viewModel.ArchiveVaultRequested += OnArchiveVaultRequested;
            viewModel.ActivityTimelineRequested += OnActivityTimelineRequested;
            viewModel.DatabaseMaintenanceRequested += OnDatabaseMaintenanceRequested;
            viewModel.WorkSessionHistoryRequested += OnWorkSessionHistoryRequested;
            viewModel.PlanningInsightsRequested += OnPlanningInsightsRequested;
            viewModel.DecisionLogRequested += OnDecisionLogRequested;
            viewModel.JournalRequested += OnJournalRequested;
            viewModel.AchievementsRequested += OnAchievementsRequested;
            viewModel.DistractionLogRequested += OnDistractionLogRequested;
            viewModel.ContextsRequested += OnContextsRequested;
            viewModel.KeyboardShortcutsRequested += OnKeyboardShortcutsRequested;
            viewModel.MeetingModeRequested += OnMeetingModeRequested;
            viewModel.WebhooksRequested += OnWebhooksRequested;
            viewModel.ApiExplorerRequested += OnApiExplorerRequested;
            viewModel.ProjectTemplatesRequested += OnProjectTemplatesRequested;
            viewModel.BulkEditRulesRequested += OnBulkEditRulesRequested;
            viewModel.MassImportRequested += OnMassImportRequested;
            viewModel.ExportProfilesRequested += OnExportProfilesRequested;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            ApplyMiniWidgetModeSize(viewModel.IsMiniWidgetMode);
            _ = viewModel.LoadTasksAsync();
            _ = RegisterKeyboardShortcutsAsync(viewModel);
        }

        // The header's running-session indicator binds directly to the DI-singleton
        // FocusTimerViewModel (not this window's own WidgetViewModel) — see
        // FocusTimerIndicator's doc comment in the XAML for why.
        if (App.Services is not null)
        {
            FocusTimerIndicator.DataContext = App.Services.GetRequiredService<FocusTimerViewModel>();

            _clipboardPollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _clipboardPollTimer.Tick += OnClipboardPollTick;
            _clipboardPollTimer.Start();
        }
    }

    /// <summary>
    /// Phase 28's Clipboard History poll tick. Reads the current clipboard text and hands it
    /// to the DI-singleton ClipboardHistoryViewModel — which itself no-ops for null/blank/
    /// unchanged/too-long text (see its <see cref="ClipboardHistoryViewModel.AddEntry"/> doc
    /// comment), so <see cref="_lastSeenClipboardText"/> here only needs to short-circuit the
    /// common case (clipboard unchanged since last poll) before even reaching for a
    /// TopLevel/IClipboard, not duplicate that dedupe logic.
    /// </summary>
    private async void OnClipboardPollTick(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            return;
        }

        var text = await clipboard.TryGetTextAsync();
        if (text == _lastSeenClipboardText)
        {
            return;
        }

        _lastSeenClipboardText = text;
        App.Services.GetRequiredService<ClipboardHistoryViewModel>().AddEntry(text);
    }

    private void OnClipboardHistoryRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        ClipboardHistoryWindow.ShowOrActivate(App.Services.GetRequiredService<ClipboardHistoryViewModel>());
    }

    private async void OnTaskGroupsRequested(object? sender, EventArgs e)
    {
        if (App.Services is null || DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        var groupViewModel = App.Services.GetRequiredService<TaskGroupViewModel>();
        var groupWindow = new TaskGroupWindow { DataContext = groupViewModel };
        await groupWindow.ShowDialog(this);

        // A group may have just added tasks for the day currently being viewed — same
        // "reload after the dialog closes" pattern OnSettingsRequested and Import/Export use.
        await viewModel.LoadTasksAsync();
    }

    private async void OnTrashRequested(object? sender, EventArgs e)
    {
        if (App.Services is null || DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        var trashViewModel = App.Services.GetRequiredService<TrashViewModel>();
        var trashWindow = new TrashWindow { DataContext = trashViewModel };
        await trashWindow.ShowDialog(this);

        // A restore may have just brought back a task for the day currently being viewed —
        // same "reload after the dialog closes" pattern OnTaskGroupsRequested uses.
        await viewModel.LoadTasksAsync();
    }

    /// <summary>Feature 67, Roadmap-39-100.md.</summary>
    private async void OnBackupRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var backupViewModel = App.Services.GetRequiredService<BackupViewModel>();
        var backupWindow = new BackupWindow { DataContext = backupViewModel };
        await backupWindow.ShowDialog(this);
    }

    /// <summary>Feature 70, Roadmap-39-100.md.</summary>
    private async void OnIntegrityCheckRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var integrityViewModel = App.Services.GetRequiredService<IntegrityCheckViewModel>();
        var integrityWindow = new IntegrityCheckWindow { DataContext = integrityViewModel };
        await integrityWindow.ShowDialog(this);
    }

    /// <summary>Feature 39, Roadmap-39-100.md.</summary>
    private async void OnInboxRequested(object? sender, EventArgs e)
    {
        if (App.Services is null || DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        var inboxViewModel = App.Services.GetRequiredService<InboxViewModel>();
        var inboxWindow = new InboxWindow { DataContext = inboxViewModel };
        await inboxWindow.ShowDialog(this);

        // A conversion may have just added a task to the day currently being viewed — same
        // "reload after the dialog closes" pattern OnTrashRequested uses.
        await viewModel.LoadTasksAsync();
    }

    /// <summary>Feature 45, Roadmap-39-100.md.</summary>
    private async void OnArchiveVaultRequested(object? sender, EventArgs e)
    {
        if (App.Services is null || DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        var archiveViewModel = App.Services.GetRequiredService<ArchiveViewModel>();
        var archiveWindow = new ArchiveWindow { DataContext = archiveViewModel };
        await archiveWindow.ShowDialog(this);
        await viewModel.LoadTasksAsync();
    }

    /// <summary>Feature 61, Roadmap-39-100.md.</summary>
    private async void OnActivityTimelineRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var timelineViewModel = App.Services.GetRequiredService<ActivityTimelineViewModel>();
        var timelineWindow = new ActivityTimelineWindow { DataContext = timelineViewModel };
        await timelineWindow.ShowDialog(this);
    }

    /// <summary>Feature 69, Roadmap-39-100.md.</summary>
    private async void OnDatabaseMaintenanceRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var maintenanceViewModel = App.Services.GetRequiredService<DatabaseMaintenanceViewModel>();
        var maintenanceWindow = new DatabaseMaintenanceWindow { DataContext = maintenanceViewModel };
        await maintenanceWindow.ShowDialog(this);
    }

    /// <summary>Feature 65, Roadmap-39-100.md.</summary>
    private async void OnWorkSessionHistoryRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var sessionHistoryViewModel = App.Services.GetRequiredService<WorkSessionHistoryViewModel>();
        var sessionHistoryWindow = new WorkSessionHistoryWindow { DataContext = sessionHistoryViewModel };
        await sessionHistoryWindow.ShowDialog(this);
    }

    /// <summary>Features 51/52/53/55/56, Roadmap-39-100.md.</summary>
    private async void OnPlanningInsightsRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var insightsViewModel = App.Services.GetRequiredService<PlanningInsightsViewModel>();
        var insightsWindow = new PlanningInsightsWindow { DataContext = insightsViewModel };
        await insightsWindow.ShowDialog(this);
    }

    /// <summary>Feature 57, Roadmap-39-100.md.</summary>
    private async void OnDecisionLogRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var decisionLogViewModel = App.Services.GetRequiredService<DecisionLogViewModel>();
        var decisionLogWindow = new DecisionLogWindow { DataContext = decisionLogViewModel };
        await decisionLogWindow.ShowDialog(this);
    }

    /// <summary>Feature 60, Roadmap-39-100.md.</summary>
    private async void OnJournalRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var journalViewModel = App.Services.GetRequiredService<JournalViewModel>();
        var journalWindow = new JournalWindow { DataContext = journalViewModel };
        await journalWindow.ShowDialog(this);
    }

    /// <summary>Feature 62, Roadmap-39-100.md.</summary>
    private async void OnAchievementsRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var achievementsViewModel = App.Services.GetRequiredService<AchievementsViewModel>();
        var achievementsWindow = new AchievementsWindow { DataContext = achievementsViewModel };
        await achievementsWindow.ShowDialog(this);
    }

    /// <summary>Feature 64, Roadmap-39-100.md.</summary>
    private async void OnDistractionLogRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var distractionLogViewModel = App.Services.GetRequiredService<DistractionLogViewModel>();
        var distractionLogWindow = new DistractionLogWindow { DataContext = distractionLogViewModel };
        await distractionLogWindow.ShowDialog(this);
    }

    /// <summary>Feature 63, Roadmap-39-100.md.</summary>
    private async void OnContextsRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var contextsViewModel = App.Services.GetRequiredService<ContextsViewModel>();
        var contextsWindow = new ContextsWindow { DataContext = contextsViewModel };
        await contextsWindow.ShowDialog(this);
    }

    /// <summary>Feature 77, Roadmap-39-100.md. Re-registers keyboard shortcuts afterward, since a rebind may have just changed one.</summary>
    private async void OnKeyboardShortcutsRequested(object? sender, EventArgs e)
    {
        if (App.Services is null || DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        var shortcutsViewModel = App.Services.GetRequiredService<KeyboardShortcutsViewModel>();
        var shortcutsWindow = new KeyboardShortcutsWindow { DataContext = shortcutsViewModel };
        await shortcutsWindow.ShowDialog(this);

        KeyBindings.Clear();
        await RegisterKeyboardShortcutsAsync(viewModel);
    }

    /// <summary>Feature 58, Roadmap-39-100.md. A fresh <see cref="MeetingSessionViewModel"/> per summon (it's registered transient) — Meeting Mode is a scratch workspace, not something with state to reload.</summary>
    private async void OnMeetingModeRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var meetingViewModel = App.Services.GetRequiredService<MeetingSessionViewModel>();
        var meetingWindow = new MeetingModeWindow { DataContext = meetingViewModel };
        await meetingWindow.ShowDialog(this);
    }

    /// <summary>Feature 96, Roadmap-39-100.md.</summary>
    private async void OnWebhooksRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var webhooksViewModel = App.Services.GetRequiredService<WebhooksViewModel>();
        var webhooksWindow = new WebhooksWindow { DataContext = webhooksViewModel };
        await webhooksWindow.ShowDialog(this);
    }

    /// <summary>Feature 100, Roadmap-39-100.md.</summary>
    private async void OnApiExplorerRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var apiExplorerViewModel = App.Services.GetRequiredService<ApiExplorerViewModel>();
        var apiExplorerWindow = new ApiExplorerWindow { DataContext = apiExplorerViewModel };
        await apiExplorerWindow.ShowDialog(this);
    }

    /// <summary>Features 86/87, Roadmap-39-100.md.</summary>
    private async void OnProjectTemplatesRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var projectTemplatesViewModel = App.Services.GetRequiredService<ProjectTemplatesViewModel>();
        var projectTemplatesWindow = new ProjectTemplatesWindow { DataContext = projectTemplatesViewModel };
        await projectTemplatesWindow.ShowDialog(this);
    }

    /// <summary>Feature 88, Roadmap-39-100.md.</summary>
    private async void OnBulkEditRulesRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var bulkEditRulesViewModel = App.Services.GetRequiredService<BulkEditRulesViewModel>();
        var bulkEditRulesWindow = new BulkEditRulesWindow { DataContext = bulkEditRulesViewModel };
        await bulkEditRulesWindow.ShowDialog(this);
    }

    /// <summary>Features 89/90, Roadmap-39-100.md.</summary>
    private async void OnMassImportRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var massImportViewModel = App.Services.GetRequiredService<MassImportViewModel>();
        var massImportWindow = new MassImportWindow { DataContext = massImportViewModel };
        await massImportWindow.ShowDialog(this);
    }

    /// <summary>Feature 91, Roadmap-39-100.md.</summary>
    private async void OnExportProfilesRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var exportProfilesViewModel = App.Services.GetRequiredService<ExportProfilesViewModel>();
        var exportProfilesWindow = new ExportProfilesWindow { DataContext = exportProfilesViewModel };
        await exportProfilesWindow.ShowDialog(this);
    }

    /// <summary>Feature 83, Roadmap-39-100.md — same "sync the flyout's list on open" pattern as <c>GridWindow.OnViewsFlyoutOpened</c>.</summary>
    private async void OnViewsFlyoutOpened(object? sender, EventArgs e)
    {
        if (DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        var views = await viewModel.GetSavedViewsAsync();
        SavedViewsListBox.ItemsSource = views.Select(v => v.Name).ToList();
    }

    private async void OnApplyViewClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WidgetViewModel viewModel || SavedViewsListBox.SelectedItem is not string name)
        {
            return;
        }

        await viewModel.ApplyViewAsync(name);
    }

    private async void OnDeleteViewClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WidgetViewModel viewModel || SavedViewsListBox.SelectedItem is not string name)
        {
            return;
        }

        await viewModel.DeleteSavedViewAsync(name);
        var views = await viewModel.GetSavedViewsAsync();
        SavedViewsListBox.ItemsSource = views.Select(v => v.Name).ToList();
    }

    private async void OnSaveViewClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        var name = NewWidgetViewNameTextBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await viewModel.SaveCurrentViewAsync(name);
        NewWidgetViewNameTextBox.Text = string.Empty;
    }

    /// <summary>
    /// Phase 28's app-wide keyboard shortcuts, added programmatically rather than declared
    /// as static <c>KeyBinding</c>s in XAML — Avalonia's <c>KeyGesture</c> string parser has
    /// no OS-conditional translation between "Cmd" and "Ctrl", so a single XAML gesture
    /// string can't correctly mean Cmd on macOS and Ctrl on Windows/Linux at once. Choosing
    /// the modifier explicitly per-OS here is what's actually verified to press correctly on
    /// each platform, rather than assumed from an unverified gesture string.
    ///
    /// As of Feature 77 (Roadmap-39-100.md), each binding's combo is read from
    /// <c>AppSettings.KeyboardShortcutOverrides</c> (falling back to
    /// <see cref="KeyboardShortcutDefinition.All"/>'s own default) rather than hardcoded here —
    /// this is now async (settings must load first), unlike before this feature existed.
    /// </summary>
    private async Task RegisterKeyboardShortcutsAsync(WidgetViewModel viewModel)
    {
        var modifier = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;

        var commandsByCommandId = new Dictionary<string, ICommand>
        {
            ["CommandPalette"] = viewModel.OpenCommandPaletteCommand,
            ["ToggleSearch"] = viewModel.ToggleSearchBarCommand,
            ["Settings"] = viewModel.OpenSettingsCommand,
            ["Undo"] = viewModel.UndoCommand,
            ["Redo"] = viewModel.RedoCommand,
            ["TogglePrivacyMode"] = viewModel.TogglePrivacyModeCommand,
        };

        var overrides = App.Services is { } services
            ? (await services.GetRequiredService<ISettingsService>().LoadAsync()).KeyboardShortcutOverrides
            : [];

        foreach (var definition in KeyboardShortcutDefinition.All)
        {
            var combo = overrides.GetValueOrDefault(definition.CommandId, definition.DefaultCombo);
            if (KeyboardShortcutDefinition.TryParseGesture(combo, modifier) is { } gesture)
            {
                KeyBindings.Add(new KeyBinding { Gesture = gesture, Command = commandsByCommandId[definition.CommandId] });
            }
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WidgetViewModel.IsMiniWidgetMode) && DataContext is WidgetViewModel viewModel)
        {
            ApplyMiniWidgetModeSize(viewModel.IsMiniWidgetMode);
        }
    }

    // MinHeight is lowered/restored alongside Height — the XAML's default MinHeight="360"
    // would otherwise silently clamp the window back up the moment Height drops below it.
    private void ApplyMiniWidgetModeSize(bool isMiniWidgetMode)
    {
        if (isMiniWidgetMode)
        {
            _preMiniModeHeight ??= Height;
            MinHeight = MiniModeMinHeight;
            Height = MiniModeHeight;
        }
        else
        {
            MinHeight = DefaultMinHeight;
            Height = _preMiniModeHeight ?? DefaultHeight;
            _preMiniModeHeight = null;
        }
    }

    // Bounds are captured here (before the window actually closes) rather than in
    // OnClosed, since Position/Width/Height are meaningless to read once the window has
    // torn down. Saved with GetAwaiter().GetResult() — blocking briefly on a local JSON
    // write during shutdown, the same pattern Program.cs uses for the database migration —
    // rather than fire-and-forget, since fire-and-forget here could easily lose the write
    // to process exit. Wrapped in Task.Run for the same reason as App.axaml.cs's
    // LoadSettingsAsync call — blocking the UI thread on an un-decoupled async chain risks
    // the same deadlock class confirmed live at startup.
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is WidgetViewModel viewModel)
        {
            var left = Position.X;
            var top = Position.Y;
            var width = Width;
            var height = Height;
            Task.Run(() => viewModel.SaveWindowBoundsAsync(left, top, width, height)).GetAwaiter().GetResult();
        }

        base.OnClosing(e);

        // "Minimize to Tray" (Phase 22): the widget's own close button hides it rather than
        // exiting the app — only the tray icon's "Quit" item (App.IsQuitting) really closes it.
        // Gated on App.Services being set so headless tests, which construct WidgetWindow
        // directly and never touch that static, keep closing for real exactly as before.
        if (App.Services is not null && !App.IsQuitting)
        {
            e.Cancel = true;
            Hide();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is WidgetViewModel viewModel)
        {
            viewModel.TaskEditRequested -= OnTaskEditRequested;
            viewModel.SettingsRequested -= OnSettingsRequested;
            viewModel.GridViewRequested -= OnGridViewRequested;
            viewModel.CalendarViewRequested -= OnCalendarViewRequested;
            viewModel.PlannerViewRequested -= OnPlannerViewRequested;
            viewModel.FocusTimerRequested -= OnFocusTimerRequested;
            viewModel.AnalyticsRequested -= OnAnalyticsRequested;
            viewModel.CommandPaletteRequested -= OnCommandPaletteRequested;
            viewModel.ClipboardHistoryRequested -= OnClipboardHistoryRequested;
            viewModel.TaskGroupsRequested -= OnTaskGroupsRequested;
            viewModel.TrashRequested -= OnTrashRequested;
            viewModel.BackupRequested -= OnBackupRequested;
            viewModel.IntegrityCheckRequested -= OnIntegrityCheckRequested;
            viewModel.InboxRequested -= OnInboxRequested;
            viewModel.ArchiveVaultRequested -= OnArchiveVaultRequested;
            viewModel.ActivityTimelineRequested -= OnActivityTimelineRequested;
            viewModel.DatabaseMaintenanceRequested -= OnDatabaseMaintenanceRequested;
            viewModel.WorkSessionHistoryRequested -= OnWorkSessionHistoryRequested;
            viewModel.PlanningInsightsRequested -= OnPlanningInsightsRequested;
            viewModel.DecisionLogRequested -= OnDecisionLogRequested;
            viewModel.JournalRequested -= OnJournalRequested;
            viewModel.AchievementsRequested -= OnAchievementsRequested;
            viewModel.DistractionLogRequested -= OnDistractionLogRequested;
            viewModel.ContextsRequested -= OnContextsRequested;
            viewModel.KeyboardShortcutsRequested -= OnKeyboardShortcutsRequested;
            viewModel.MeetingModeRequested -= OnMeetingModeRequested;
            viewModel.WebhooksRequested -= OnWebhooksRequested;
            viewModel.ApiExplorerRequested -= OnApiExplorerRequested;
            viewModel.ProjectTemplatesRequested -= OnProjectTemplatesRequested;
            viewModel.BulkEditRulesRequested -= OnBulkEditRulesRequested;
            viewModel.MassImportRequested -= OnMassImportRequested;
            viewModel.ExportProfilesRequested -= OnExportProfilesRequested;
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (_clipboardPollTimer is not null)
        {
            _clipboardPollTimer.Stop();
            _clipboardPollTimer.Tick -= OnClipboardPollTick;
            _clipboardPollTimer = null;
        }

        (DataContext as IDisposable)?.Dispose();
        base.OnClosed(e);
    }

    private async void OnSettingsRequested(object? sender, EventArgs e)
    {
        if (App.Services is null || DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        var settingsViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        var settingsWindow = new SettingsWindow { DataContext = settingsViewModel };
        var wasSaved = false;
        settingsViewModel.Saved += (_, _) => { wasSaved = true; settingsWindow.Close(); };
        settingsViewModel.CancelRequested += (_, _) => settingsWindow.Close();

        var monitorOptions = Screens.All.Select(screen => new MonitorOption(MonitorIdentity.GetId(screen), MonitorIdentity.GetLabel(screen))).ToList();
        settingsViewModel.SetAvailableMonitors(monitorOptions);

        await settingsViewModel.LoadAsync();
        await settingsWindow.ShowDialog(this);

        // Re-applies live even on Cancel — cheap, and correct either way: Cancel didn't
        // persist anything, so this just reloads the same settings that were already active.
        await viewModel.LoadSettingsAsync();
        App.ApplyAccentColor(viewModel.AccentColorHex);
        App.ApplyTheme(viewModel.Theme);
        viewModel.IsDarkTheme = ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;

        // Only on Save, not Cancel: repositioning is a real action (moves the window the
        // user is looking at), not just re-applying already-persisted state like the two
        // calls above.
        if (wasSaved)
        {
            RepositionOnMonitor(settingsViewModel.SelectedMonitor.Id);
        }

        // Also reloads tasks: an Import/Export round trip through Settings (see
        // SettingsWindow's "Import / Export tasks…" button) may have added tasks for the
        // day currently being viewed.
        await viewModel.LoadTasksAsync();
    }

    /// <summary>Phase 22's "Multi Monitor Support" — centers the widget on the chosen monitor's working area immediately. A no-op for <see cref="MonitorOption.Unspecified"/> (empty id) or a since-disconnected monitor, both of which <see cref="MonitorIdentity.Resolve"/> reports as null.</summary>
    private void RepositionOnMonitor(string monitorId)
    {
        var screen = MonitorIdentity.Resolve(Screens, monitorId);
        if (screen is null)
        {
            return;
        }

        var area = screen.WorkingArea;
        Position = new PixelPoint(
            area.X + (area.Width - (int)Width) / 2,
            area.Y + (area.Height - (int)Height) / 2);
    }

    private async void OnGridViewRequested(object? sender, EventArgs e)
    {
        if (App.Services is null || DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        var gridViewModel = App.Services.GetRequiredService<GridViewModel>();
        var gridWindow = new GridWindow { DataContext = gridViewModel };
        gridViewModel.CloseRequested += (_, _) => gridWindow.Close();

        await gridViewModel.LoadAsync();
        await gridWindow.ShowDialog(this);

        // Grid edits (title/date/priority/category/due/completed/notes, or deletes) may
        // have changed what belongs on the day currently being viewed.
        await viewModel.LoadTasksAsync();
    }

    private async void OnCalendarViewRequested(object? sender, EventArgs e)
    {
        if (App.Services is null || DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        var calendarViewModel = App.Services.GetRequiredService<CalendarViewModel>();
        var calendarWindow = new CalendarWindow { DataContext = calendarViewModel };
        calendarViewModel.CloseRequested += (_, _) => calendarWindow.Close();

        await calendarViewModel.LoadAsync(viewModel.PlanDate);
        await calendarWindow.ShowDialog(this);

        // Picking a day in the calendar navigates the widget there — a no-op if the
        // calendar was closed without picking one (Close/OS close button leave this null).
        if (calendarWindow.SelectedDate is { } selected)
        {
            viewModel.SelectedDate = selected.ToDateTime(TimeOnly.MinValue);
        }
    }

    private async void OnPlannerViewRequested(object? sender, EventArgs e)
    {
        if (App.Services is null || DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        var plannerViewModel = App.Services.GetRequiredService<PlannerViewModel>();
        var plannerWindow = new PlannerWindow { DataContext = plannerViewModel };
        plannerViewModel.CloseRequested += (_, _) => plannerWindow.Close();
        plannerViewModel.DateSelected += (_, date) =>
        {
            viewModel.SelectedDate = date.ToDateTime(TimeOnly.MinValue);
            plannerWindow.Close();
        };

        await plannerViewModel.LoadAsync(viewModel.PlanDate);
        await plannerWindow.ShowDialog(this);
    }

    /// <summary>
    /// Non-modal (<c>Show</c>, not <c>ShowDialog</c>) — unlike every other header icon's
    /// window, a running timer needs the widget to stay interactive (checking off tasks
    /// while a Pomodoro runs is the whole point). <see cref="FocusTimerWindow.ShowOrActivate"/>
    /// reuses one window over the DI-singleton <see cref="FocusTimerViewModel"/> rather than
    /// opening a new one per click — shared with <c>TaskEditWindow</c>'s "Start Timer" entry
    /// point too, so neither can end up duplicating the other's window.
    /// </summary>
    private void OnFocusTimerRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        FocusTimerWindow.ShowOrActivate(App.Services.GetRequiredService<FocusTimerViewModel>());
    }

    private async void OnAnalyticsRequested(object? sender, EventArgs e)
    {
        if (App.Services is null)
        {
            return;
        }

        var analyticsViewModel = App.Services.GetRequiredService<AnalyticsViewModel>();
        var analyticsWindow = new AnalyticsWindow { DataContext = analyticsViewModel };
        await analyticsWindow.ShowDialog(this);
    }

    /// <summary>Phase 28's Command Palette — built fresh on every summon (not cached) from this window's own live <c>WidgetViewModel</c>'s commands, so a state change (e.g. Mini Widget mode) is reflected the next time it's opened without extra bookkeeping.</summary>
    private async void OnCommandPaletteRequested(object? sender, EventArgs e)
    {
        if (App.Services is null || DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        // Resolved as a DI singleton (see ServiceCollectionExtensions), not "new"'d here —
        // the palette is rebuilt fresh from this window's live commands on every summon (see
        // this method's own doc comment), but its "recent commands" list needs to survive
        // across separate summons within the same session, which a fresh instance wouldn't.
        var paletteViewModel = App.Services.GetRequiredService<CommandPaletteViewModel>();
        paletteViewModel.SetEntries([
            new CommandPaletteEntry("Go to Today", viewModel.GoToTodayCommand),
            new CommandPaletteEntry("Previous Day", viewModel.GoToPreviousDayCommand),
            new CommandPaletteEntry("Next Day", viewModel.GoToNextDayCommand),
            new CommandPaletteEntry("Toggle Search & Filter", viewModel.ToggleSearchBarCommand),
            new CommandPaletteEntry("Toggle Select Mode", viewModel.ToggleSelectModeCommand),
            new CommandPaletteEntry("Open Grid View", viewModel.OpenGridViewCommand),
            new CommandPaletteEntry("Open Calendar View", viewModel.OpenCalendarViewCommand),
            new CommandPaletteEntry("Open Planner", viewModel.OpenPlannerViewCommand),
            new CommandPaletteEntry("Open Focus Timer", viewModel.OpenFocusTimerCommand),
            new CommandPaletteEntry("Open Analytics & Reports", viewModel.OpenAnalyticsCommand),
            new CommandPaletteEntry("Open Settings", viewModel.OpenSettingsCommand),
            new CommandPaletteEntry("Toggle Mini Widget", viewModel.ToggleMiniWidgetModeCommand),
            new CommandPaletteEntry("Clipboard History", viewModel.OpenClipboardHistoryCommand),
            new CommandPaletteEntry("Task Groups", viewModel.OpenTaskGroupsCommand),
            new CommandPaletteEntry("Trash", viewModel.OpenTrashCommand),
            new CommandPaletteEntry("Backups", viewModel.OpenBackupsCommand),
            new CommandPaletteEntry("Data Integrity Check", viewModel.OpenIntegrityCheckCommand),
            new CommandPaletteEntry("Undo", viewModel.UndoCommand),
            new CommandPaletteEntry("Redo", viewModel.RedoCommand),
            new CommandPaletteEntry("Inbox", viewModel.OpenInboxCommand),
            new CommandPaletteEntry("Archive Vault", viewModel.OpenArchiveVaultCommand),
            new CommandPaletteEntry("Activity Timeline", viewModel.OpenActivityTimelineCommand),
            new CommandPaletteEntry("Database Maintenance", viewModel.OpenDatabaseMaintenanceCommand),
            new CommandPaletteEntry("Work Session History", viewModel.OpenWorkSessionHistoryCommand),
            new CommandPaletteEntry("Planning Insights", viewModel.OpenPlanningInsightsCommand),
            new CommandPaletteEntry("Decision Log", viewModel.OpenDecisionLogCommand),
            new CommandPaletteEntry("Journal", viewModel.OpenJournalCommand),
            new CommandPaletteEntry("Achievements", viewModel.OpenAchievementsCommand),
            new CommandPaletteEntry("Distraction Log", viewModel.OpenDistractionLogCommand),
            new CommandPaletteEntry("Focus Contexts", viewModel.OpenContextsCommand),
            new CommandPaletteEntry("Keyboard Shortcuts", viewModel.OpenKeyboardShortcutsCommand),
            new CommandPaletteEntry("Meeting Mode", viewModel.OpenMeetingModeCommand),
            new CommandPaletteEntry("Webhooks", viewModel.OpenWebhooksCommand),
            new CommandPaletteEntry("API Explorer", viewModel.OpenApiExplorerCommand),
            new CommandPaletteEntry("Project Templates", viewModel.OpenProjectTemplatesCommand),
            new CommandPaletteEntry("Bulk Edit Rules", viewModel.OpenBulkEditRulesCommand),
            new CommandPaletteEntry("Mass Import Wizard", viewModel.OpenMassImportCommand),
            new CommandPaletteEntry("Export Profiles", viewModel.OpenExportProfilesCommand),
            new CommandPaletteEntry("Toggle Privacy Mode", viewModel.TogglePrivacyModeCommand),
            new CommandPaletteEntry("Enter Presentation Mode", viewModel.EnterPresentationModeCommand),
            new CommandPaletteEntry("Exit Presentation Mode", viewModel.ExitPresentationModeCommand),
        ]);

        var paletteWindow = new CommandPaletteWindow { DataContext = paletteViewModel };
        await paletteWindow.ShowDialog(this);
    }

    private async void OnTaskEditRequested(object? sender, Guid taskId)
    {
        if (App.Services is null)
        {
            return;
        }

        var editViewModel = App.Services.GetRequiredService<TaskEditViewModel>();
        var editWindow = new TaskEditWindow { DataContext = editViewModel };
        editViewModel.Saved += (_, _) => editWindow.Close();
        editViewModel.CancelRequested += (_, _) => editWindow.Close();

        await editViewModel.LoadAsync(taskId);
        await editWindow.ShowDialog(this);

        if (DataContext is WidgetViewModel viewModel)
        {
            await viewModel.LoadTasksAsync();
        }
    }

    // The window has no title bar (SystemDecorations="None"), so the header
    // area itself drives moving it — the standard Avalonia pattern for
    // borderless/chromeless windows.
    private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnCloseButtonClick(object? sender, RoutedEventArgs e) => Close();

    private void OnAddTaskKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is WidgetViewModel viewModel)
        {
            viewModel.AddTaskCommand.Execute(null);
        }
    }

    private void OnTitleDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control { DataContext: TaskItemViewModel taskItem } titleBlock)
        {
            return;
        }

        taskItem.BeginEditCommand.Execute(null);

        // The edit TextBox becomes visible as a side effect of the command above, but
        // toggling IsVisible doesn't detach/reattach it from the visual tree, so there's
        // no "just appeared" lifecycle event to hook — focusing has to be deferred past
        // this layout pass instead of attempted immediately.
        if (titleBlock.GetVisualParent() is Control row)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var editBox = row.GetVisualChildren().OfType<TextBox>().FirstOrDefault();
                editBox?.Focus();
                editBox?.SelectAll();
            }, DispatcherPriority.Loaded);
        }
    }

    private void OnEditTitleKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not Control { DataContext: TaskItemViewModel taskItem })
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                taskItem.CommitEditCommand.Execute(null);
                break;
            case Key.Escape:
                taskItem.CancelEditCommand.Execute(null);
                break;
        }
    }

    private async void OnDragHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: TaskItemViewModel taskItem } handle)
        {
            return;
        }

        if (!e.GetCurrentPoint(handle).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _draggedTaskId = taskItem.Id;
        await DragDrop.DoDragDropAsync(e, new DataTransfer(), DragDropEffects.Move);
        _draggedTaskId = null;
    }

    private void OnRowDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = _draggedTaskId.HasValue ? DragDropEffects.Move : DragDropEffects.None;
    }

    private async void OnRowDrop(object? sender, DragEventArgs e)
    {
        if (_draggedTaskId is not { } draggedId ||
            sender is not Control { DataContext: TaskItemViewModel targetItem } ||
            DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        await viewModel.ReorderAsync(draggedId, targetItem.Id);
    }

    // Phase 35's Drag File/Drag Browser Tab to Create Task. Lives on the outer task-list
    // Panel rather than per-row, so a drop anywhere in the list (including empty space) is
    // recognized, not just directly on an existing row. This coexists with the row-level
    // internal reorder drag above without extra guarding: that drag carries an empty
    // DataTransfer (see the _draggedTaskId field's comment), so Contains(File)/Contains(Text)
    // below is naturally false for it, and DragOver/Drop are bubbling routed events — this
    // Panel-level handler runs after the row-level one and simply leaves DragEffects/handling
    // alone whenever there's no real external payload.
    private void OnExternalDragOver(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.Contains(DataFormat.File) || e.DataTransfer.Contains(DataFormat.Text))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
    }

    private async void OnExternalDrop(object? sender, DragEventArgs e)
    {
        if (App.Services is null || DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        var files = e.DataTransfer.TryGetFiles();
        if (files is { Length: > 0 })
        {
            var attachmentService = App.Services.GetRequiredService<IAttachmentService>();
            foreach (var file in files)
            {
                var task = await viewModel.CreateTaskFromDropAsync(file.Name);
                var localPath = file.TryGetLocalPath();
                if (localPath is not null)
                {
                    await attachmentService.AddAttachmentAsync(task.Id, localPath);
                }
            }

            return;
        }

        var text = e.DataTransfer.TryGetText()?.Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            await viewModel.CreateTaskFromDropAsync(text);
        }
    }

    // Delete is gated behind ConfirmDialogWindow here in code-behind rather than
    // TaskItemViewModel/WidgetViewModel showing the dialog themselves — no ViewModel in
    // this app owns a Window reference (see TaskEditRequested/SettingsRequested/
    // GridViewRequested, all handled the same way), so the confirm step lives on the same
    // side as every other dialog hand-off. DeleteCommand/BulkDeleteCommand themselves are
    // unchanged; this only gates *invoking* them.
    private async void OnDeleteTaskClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: TaskItemViewModel taskItem })
        {
            return;
        }

        var confirmed = await ConfirmDialogWindow.ShowAsync(this, "Delete task?",
            $"\"{taskItem.Title}\" will be deleted. This can't be undone from here.");
        if (confirmed)
        {
            await taskItem.DeleteCommand.ExecuteAsync(null);
        }
    }

    private async void OnBulkDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WidgetViewModel viewModel)
        {
            return;
        }

        var count = viewModel.SelectedCount;
        var confirmed = await ConfirmDialogWindow.ShowAsync(this, "Delete selected tasks?",
            $"{count} selected {(count == 1 ? "task" : "tasks")} will be deleted. This can't be undone from here.");
        if (confirmed)
        {
            await viewModel.BulkDeleteCommand.ExecuteAsync(null);
        }
    }
}
