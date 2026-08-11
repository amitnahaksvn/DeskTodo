using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Settings;
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

    [Fact]
    public async Task LoadAsync_PopulatesAccentColorAndOpacityPercentFromSettings()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings { AccentColorHex = "#10B981", WidgetOpacity = 0.8 });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), NullLogger<SettingsViewModel>.Instance);

        await sut.LoadAsync();

        Assert.Equal("#10B981", sut.AccentColorHex);
        Assert.Equal(80, sut.OpacityPercent);
        Assert.True(sut.IsLoaded);
    }

    [Fact]
    public void SelectAccentColorCommand_UpdatesAccentColorHex()
    {
        var settingsService = new Mock<ISettingsService>();
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), NullLogger<SettingsViewModel>.Instance);

        sut.SelectAccentColorCommand.Execute("#8B5CF6");

        Assert.Equal("#8B5CF6", sut.AccentColorHex);
    }

    [Fact]
    public async Task SaveAsync_PersistsCurrentAccentAndOpacity_AndRaisesSaved()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), NullLogger<SettingsViewModel>.Instance);
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
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), NullLogger<SettingsViewModel>.Instance);
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
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), NullLogger<SettingsViewModel>.Instance);
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
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), NullLogger<SettingsViewModel>.Instance);

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
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(isEnabled: true), NullLogger<SettingsViewModel>.Instance);

        await sut.LoadAsync();

        Assert.False(sut.NotificationsEnabled);
        Assert.True(sut.AutoStartEnabled); // From IAutoStartService.IsEnabled, not a persisted flag — see the property's doc comment.
    }

    [Fact]
    public async Task SaveAsync_PersistsNotificationsEnabled()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { NotificationsEnabled = true });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), NullLogger<SettingsViewModel>.Instance);
        await sut.LoadAsync();
        sut.NotificationsEnabled = false;

        await sut.SaveCommand.ExecuteAsync(null);

        settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => !a.NotificationsEnabled), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WhenAutoStartToggledOn_CallsEnable()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var autoStartService = new Mock<IAutoStartService>();
        var sut = new SettingsViewModel(settingsService.Object, autoStartService.Object, NullLogger<SettingsViewModel>.Instance);
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
        var sut = new SettingsViewModel(settingsService.Object, autoStartService.Object, NullLogger<SettingsViewModel>.Instance);
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
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), NullLogger<SettingsViewModel>.Instance);

        await sut.LoadAsync();

        Assert.False(sut.ShowInTaskbar);
    }

    [Fact]
    public async Task SaveAsync_PersistsShowInTaskbar()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { ShowInTaskbar = true });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), NullLogger<SettingsViewModel>.Instance);
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
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), NullLogger<SettingsViewModel>.Instance);

        await sut.LoadAsync();

        Assert.True(sut.AutoRescheduleOverdueTasks);
    }

    [Fact]
    public async Task SaveAsync_PersistsAutoRescheduleOverdueTasks()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { AutoRescheduleOverdueTasks = false });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), NullLogger<SettingsViewModel>.Instance);
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
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), NullLogger<SettingsViewModel>.Instance);

        await sut.LoadAsync();

        Assert.Equal(MonitorOption.Unspecified, sut.SelectedMonitor);
        Assert.Single(sut.Monitors); // Just Unspecified — SetAvailableMonitors was never called.
    }

    [Fact]
    public async Task SetAvailableMonitors_ThenLoadAsync_SelectsThePersistedMonitor_WhenStillConnected()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { PreferredMonitorId = "monitor-2" });
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), NullLogger<SettingsViewModel>.Instance);
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
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), NullLogger<SettingsViewModel>.Instance);
        sut.SetAvailableMonitors([new MonitorOption("monitor-1", "Built-in")]);

        await sut.LoadAsync();

        Assert.Equal(MonitorOption.Unspecified, sut.SelectedMonitor);
    }

    [Fact]
    public async Task SaveAsync_PersistsTheSelectedMonitorId()
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), NullLogger<SettingsViewModel>.Instance);
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
        var sut = new SettingsViewModel(settingsService.Object, CreateAutoStartService(), NullLogger<SettingsViewModel>.Instance);
        sut.SetAvailableMonitors([new MonitorOption("monitor-1", "Built-in")]);
        await sut.LoadAsync();
        sut.SelectedMonitor = MonitorOption.Unspecified;

        await sut.SaveCommand.ExecuteAsync(null);

        settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => a.PreferredMonitorId == null), It.IsAny<CancellationToken>()), Times.Once);
    }
}
