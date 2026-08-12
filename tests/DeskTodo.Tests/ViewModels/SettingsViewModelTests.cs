using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Settings;
using DeskTodo.Application.Updates;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class SettingsViewModelTests
{
    private static IAutoStartService CreateAutoStartService(bool isEnabled = false)
    {
        var mock = new Mock<IAutoStartService>();
        mock.SetupGet(s => s.IsEnabled).Returns(isEnabled);
        return mock.Object;
    }

    private static IUpdateCheckService CreateStubUpdateCheckService()
    {
        var mock = new Mock<IUpdateCheckService>();
        mock.Setup(s => s.CheckForUpdateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new UpdateCheckResult(false, null, null, null));
        return mock.Object;
    }

    [Fact]
    public async Task LoadAsync_PopulatesAccentColorAndOpacityPercentFromSettings()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings { AccentColorHex = "#10B981", WidgetOpacity = 0.8 });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);

        await sut.LoadAsync();

        Assert.Equal("#10B981", sut.AccentColorHex);
        Assert.Equal(80, sut.OpacityPercent);
        Assert.True(sut.IsLoaded);
    }

    [Fact]
    public void SelectAccentColorCommand_UpdatesAccentColorHex()
    {
        var settingsService = new Mock<ISettingsService>();
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);

        sut.SelectAccentColorCommand.Execute("#8B5CF6");

        Assert.Equal("#8B5CF6", sut.AccentColorHex);
    }

    [Fact]
    public async Task SaveAsync_PersistsCurrentAccentAndOpacity_AndRaisesSaved()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();
        sut.SelectAccentColorCommand.Execute("#F97316");
        sut.OpacityPercent = 60;

        var raised = false;
        sut.Saved += (_, _) => raised = true;
        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(raised);
        settingsService.Verify(s => s.SaveAsync(
            It.Is<AppSettings>(a => a.AccentColorHex == "#F97316" && a.WidgetOpacity == 0.6),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveAsync_PreservesWindowBoundsLoadedFromSettings()
    {
        // Save shouldn't clobber fields it doesn't have UI for (window bounds are only
        // ever written by WidgetWindow.OnClosing) — it must round-trip whatever was loaded.
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings { WindowLeft = 50, WindowTop = 60, WindowWidth = 340, WindowHeight = 560 });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();

        await sut.SaveCommand.ExecuteAsync(null);

        settingsService.Verify(s => s.SaveAsync(
            It.Is<AppSettings>(a => a.WindowLeft == 50 && a.WindowTop == 60 && a.WindowWidth == 340 && a.WindowHeight == 560),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetWindowPositionAsync_ClearsWindowBoundsAndSaves()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings { WindowLeft = 50, WindowTop = 60, WindowWidth = 340, WindowHeight = 560 });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();

        await sut.ResetWindowPositionCommand.ExecuteAsync(null);

        settingsService.Verify(s => s.SaveAsync(
            It.Is<AppSettings>(a => a.WindowLeft == null && a.WindowTop == null && a.WindowWidth == null && a.WindowHeight == null),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void CancelCommand_RaisesCancelRequested()
    {
        var settingsService = new Mock<ISettingsService>();
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);

        var raised = false;
        sut.CancelRequested += (_, _) => raised = true;
        sut.CancelCommand.Execute(null);

        Assert.True(raised);
    }

    [Fact]
    public async Task LoadAsync_PopulatesNotificationsEnabledFromSettings_AndAutoStartEnabledFromTheRealOsState()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { NotificationsEnabled = false });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(isEnabled: true), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);

        await sut.LoadAsync();

        Assert.False(sut.NotificationsEnabled);
        Assert.True(sut.AutoStartEnabled); // From IAutoStartService.IsEnabled, not a persisted flag — see the property's doc comment.
    }

    [Fact]
    public async Task SaveAsync_PersistsNotificationsEnabled()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { NotificationsEnabled = true });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();
        sut.NotificationsEnabled = false;

        await sut.SaveCommand.ExecuteAsync(null);

        settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => !a.NotificationsEnabled), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_PopulatesNotificationSoundEnabledFromSettings()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { NotificationSoundEnabled = false });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);

        await sut.LoadAsync();

        Assert.False(sut.NotificationSoundEnabled);
    }

    [Fact]
    public async Task SaveAsync_PersistsNotificationSoundEnabled()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { NotificationSoundEnabled = true });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();
        sut.NotificationSoundEnabled = false;

        await sut.SaveCommand.ExecuteAsync(null);

        settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => !a.NotificationSoundEnabled), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_PopulatesHasPinSet_FromWhetherAHashIsStored()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { PinLockEnabled = true, PinHash = "somehash", PinSalt = "somesalt" });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);

        await sut.LoadAsync();

        Assert.True(sut.PinLockEnabled);
        Assert.True(sut.HasPinSet);
        Assert.Equal(string.Empty, sut.NewPin); // The actual PIN is never round-tripped back into the UI.
    }

    [Fact]
    public async Task SaveAsync_WithPinLockOffAndNoPreviousPin_SavesSuccessfully()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();

        var saved = false;
        sut.Saved += (_, _) => saved = true;
        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(saved);
        settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => !a.PinLockEnabled && a.PinHash == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_TogglingPinLockOn_WithAMatchingNewPinAndConfirmation_HashesAndSaves()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();
        sut.PinLockEnabled = true;
        sut.NewPin = "4242";
        sut.ConfirmPin = "4242";

        var saved = false;
        sut.Saved += (_, _) => saved = true;
        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(saved);
        settingsService.Verify(s => s.SaveAsync(
            It.Is<AppSettings>(a => a.PinLockEnabled && !string.IsNullOrEmpty(a.PinHash) && !string.IsNullOrEmpty(a.PinSalt) && a.PinHash != "4242"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_TogglingPinLockOn_WithMismatchedPins_DoesNotSave_AndSetsAnError()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();
        sut.PinLockEnabled = true;
        sut.NewPin = "4242";
        sut.ConfirmPin = "0000";

        var saved = false;
        sut.Saved += (_, _) => saved = true;
        await sut.SaveCommand.ExecuteAsync(null);

        Assert.False(saved);
        Assert.NotEqual(string.Empty, sut.PinErrorMessage);
        settingsService.Verify(s => s.SaveAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_TogglingPinLockOn_WithATooShortPin_DoesNotSave_AndSetsAnError()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();
        sut.PinLockEnabled = true;
        sut.NewPin = "12";
        sut.ConfirmPin = "12";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.NotEqual(string.Empty, sut.PinErrorMessage);
        settingsService.Verify(s => s.SaveAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_TogglingPinLockOn_WithNoPinEnteredAndNoneAlreadySet_DoesNotSave_AndSetsAnError()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();
        sut.PinLockEnabled = true;

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.NotEqual(string.Empty, sut.PinErrorMessage);
        settingsService.Verify(s => s.SaveAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_PinLockAlreadyOnWithAnExistingPin_LeftBlank_KeepsTheExistingHash()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { PinLockEnabled = true, PinHash = "existinghash", PinSalt = "existingsalt" });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();
        // NewPin/ConfirmPin left blank — user didn't intend to change the PIN.

        var saved = false;
        sut.Saved += (_, _) => saved = true;
        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(saved);
        settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => a.PinHash == "existinghash" && a.PinSalt == "existingsalt"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_TogglingPinLockOff_ClearsTheStoredHash()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { PinLockEnabled = true, PinHash = "existinghash", PinSalt = "existingsalt" });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();
        sut.PinLockEnabled = false;

        await sut.SaveCommand.ExecuteAsync(null);

        settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => !a.PinLockEnabled && a.PinHash == null && a.PinSalt == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WhenAutoStartToggledOn_CallsEnable()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var autoStartService = new Mock<IAutoStartService>();
        var sut = new SettingsViewModel(settingsService.Object, autoStartService.Object, CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();
        sut.AutoStartEnabled = true;

        await sut.SaveCommand.ExecuteAsync(null);

        autoStartService.Verify(a => a.Enable(), Times.Once);
        autoStartService.Verify(a => a.Disable(), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_WhenAutoStartToggledOff_CallsDisable()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var autoStartService = new Mock<IAutoStartService>();
        autoStartService.SetupGet(a => a.IsEnabled).Returns(true);
        var sut = new SettingsViewModel(settingsService.Object, autoStartService.Object, CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();
        sut.AutoStartEnabled = false;

        await sut.SaveCommand.ExecuteAsync(null);

        autoStartService.Verify(a => a.Disable(), Times.Once);
        autoStartService.Verify(a => a.Enable(), Times.Never);
    }

    [Fact]
    public async Task LoadAsync_PopulatesShowInTaskbarFromSettings()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { ShowInTaskbar = false });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);

        await sut.LoadAsync();

        Assert.False(sut.ShowInTaskbar);
    }

    [Fact]
    public async Task SaveAsync_PersistsShowInTaskbar()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { ShowInTaskbar = true });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();
        sut.ShowInTaskbar = false;

        await sut.SaveCommand.ExecuteAsync(null);

        settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => !a.ShowInTaskbar), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_PopulatesAutoRescheduleOverdueTasksFromSettings()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { AutoRescheduleOverdueTasks = true });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);

        await sut.LoadAsync();

        Assert.True(sut.AutoRescheduleOverdueTasks);
    }

    [Fact]
    public async Task SaveAsync_PersistsAutoRescheduleOverdueTasks()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { AutoRescheduleOverdueTasks = false });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();
        sut.AutoRescheduleOverdueTasks = true;

        await sut.SaveCommand.ExecuteAsync(null);

        settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => a.AutoRescheduleOverdueTasks), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_WithNoMonitorsSet_SelectsUnspecified()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);

        await sut.LoadAsync();

        Assert.Equal(MonitorOption.Unspecified, sut.SelectedMonitor);
        Assert.Single(sut.Monitors); // Just Unspecified — SetAvailableMonitors was never called.
    }

    [Fact]
    public async Task SetAvailableMonitors_ThenLoadAsync_SelectsThePersistedMonitor_WhenStillConnected()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { PreferredMonitorId = "monitor-2" });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        sut.SetAvailableMonitors([new MonitorOption("monitor-1", "Built-in"), new MonitorOption("monitor-2", "External")]);

        await sut.LoadAsync();

        Assert.Equal("monitor-2", sut.SelectedMonitor.Id);
        Assert.Equal(["Default (current position)", "Built-in", "External"], sut.Monitors.Select(m => m.Label));
    }

    [Fact]
    public async Task LoadAsync_WhenThePersistedMonitorIsNoLongerConnected_FallsBackToUnspecified()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { PreferredMonitorId = "unplugged-monitor" });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        sut.SetAvailableMonitors([new MonitorOption("monitor-1", "Built-in")]);

        await sut.LoadAsync();

        Assert.Equal(MonitorOption.Unspecified, sut.SelectedMonitor);
    }

    [Fact]
    public async Task SaveAsync_PersistsTheSelectedMonitorId()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        sut.SetAvailableMonitors([new MonitorOption("monitor-1", "Built-in")]);
        await sut.LoadAsync();
        sut.SelectedMonitor = sut.Monitors.Single(m => m.Id == "monitor-1");

        await sut.SaveCommand.ExecuteAsync(null);

        settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => a.PreferredMonitorId == "monitor-1"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithUnspecifiedMonitor_PersistsNull()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { PreferredMonitorId = "monitor-1" });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        sut.SetAvailableMonitors([new MonitorOption("monitor-1", "Built-in")]);
        await sut.LoadAsync();
        sut.SelectedMonitor = MonitorOption.Unspecified;

        await sut.SaveCommand.ExecuteAsync(null);

        settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => a.PreferredMonitorId == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_PopulatesPomodoroAndReminderSettings()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings
        {
            PomodoroWorkMinutes = 50,
            PomodoroBreakMinutes = 10,
            BreakReminderEnabled = true,
            BreakReminderIntervalMinutes = 20,
            WaterReminderEnabled = true,
            WaterReminderIntervalMinutes = 30,
            StretchReminderEnabled = true,
            StretchReminderIntervalMinutes = 40,
        });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);

        await sut.LoadAsync();

        Assert.Equal(50m, sut.PomodoroWorkMinutes);
        Assert.Equal(10m, sut.PomodoroBreakMinutes);
        Assert.True(sut.BreakReminderEnabled);
        Assert.Equal(20m, sut.BreakReminderIntervalMinutes);
        Assert.True(sut.WaterReminderEnabled);
        Assert.Equal(30m, sut.WaterReminderIntervalMinutes);
        Assert.True(sut.StretchReminderEnabled);
        Assert.Equal(40m, sut.StretchReminderIntervalMinutes);
    }

    [Fact]
    public async Task SaveAsync_PersistsPomodoroAndReminderSettings()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();
        sut.PomodoroWorkMinutes = 45m;
        sut.PomodoroBreakMinutes = 15m;
        sut.BreakReminderEnabled = true;
        sut.BreakReminderIntervalMinutes = 25m;

        await sut.SaveCommand.ExecuteAsync(null);

        settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a =>
            a.PomodoroWorkMinutes == 45 &&
            a.PomodoroBreakMinutes == 15 &&
            a.BreakReminderEnabled &&
            a.BreakReminderIntervalMinutes == 25),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_PopulatesAppVersion_FromTheRunningAssembly()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);

        await sut.LoadAsync();

        Assert.False(string.IsNullOrEmpty(sut.AppVersion));
    }

    [Fact]
    public async Task CheckForUpdatesCommand_WhenAnUpdateIsAvailable_SetsStatusAndUrl()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var updateCheckService = new Mock<IUpdateCheckService>();
        updateCheckService.Setup(s => s.CheckForUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(true, "9.9.9", "https://example.com/release", null));
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), updateCheckService.Object, NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();

        await sut.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.Contains("9.9.9", sut.UpdateStatusMessage);
        Assert.Equal("https://example.com/release", sut.AvailableUpdateUrl);
        Assert.False(sut.IsCheckingForUpdate);
    }

    [Fact]
    public async Task CheckForUpdatesCommand_WhenAlreadyCurrent_ClearsAnyPreviousUrl()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var updateCheckService = new Mock<IUpdateCheckService>();
        updateCheckService.Setup(s => s.CheckForUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(false, null, null, null));
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), updateCheckService.Object, NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();

        await sut.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.Equal("You're on the latest version.", sut.UpdateStatusMessage);
        Assert.Null(sut.AvailableUpdateUrl);
    }

    [Fact]
    public async Task CheckForUpdatesCommand_OnError_ShowsTheErrorMessage_NotACrash()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var updateCheckService = new Mock<IUpdateCheckService>();
        updateCheckService.Setup(s => s.CheckForUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(false, null, null, "Couldn't check for updates — check your internet connection."));
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), updateCheckService.Object, NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();

        await sut.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.Equal("Couldn't check for updates — check your internet connection.", sut.UpdateStatusMessage);
        Assert.Null(sut.AvailableUpdateUrl);
    }

    [Fact]
    public async Task OpenReleasePageCommand_WithAnAvailableUpdateUrl_RaisesOpenUrlRequested()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var updateCheckService = new Mock<IUpdateCheckService>();
        updateCheckService.Setup(s => s.CheckForUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(true, "9.9.9", "https://example.com/release", null));
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), updateCheckService.Object, NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();
        await sut.CheckForUpdatesCommand.ExecuteAsync(null);

        string? openedUrl = null;
        sut.OpenUrlRequested += (_, url) => openedUrl = url;
        sut.OpenReleasePageCommand.Execute(null);

        Assert.Equal("https://example.com/release", openedUrl);
    }

    [Fact]
    public void OpenReleasePageCommand_WithNoAvailableUpdate_DoesNotRaiseOpenUrlRequested()
    {
        var settingsService = new Mock<ISettingsService>();
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), CreateStubUpdateCheckService(), NullLogger<SettingsViewModel>.Instance);

        var raised = false;
        sut.OpenUrlRequested += (_, _) => raised = true;
        sut.OpenReleasePageCommand.Execute(null);

        Assert.False(raised);
    }
}
