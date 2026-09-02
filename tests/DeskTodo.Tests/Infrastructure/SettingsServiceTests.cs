using DeskTodo.Application.Options;
using DeskTodo.Application.Settings;
using DeskTodo.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DeskTodo.Tests.Infrastructure;

public class SettingsServiceTests : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), "DeskTodoTests", Guid.NewGuid().ToString("N"));

    public SettingsServiceTests() => Directory.CreateDirectory(_rootDirectory);

    public void Dispose() => Directory.Delete(_rootDirectory, recursive: true);

    private SettingsService CreateSut() =>
        new(Options.Create(new AppStorageOptions { RootDirectory = _rootDirectory, SettingsFileName = "settings.json" }), NullLogger<SettingsService>.Instance);

    [Fact]
    public async Task LoadAsync_WithNoFileOnDisk_ReturnsDefaults()
    {
        var sut = CreateSut();

        var settings = await sut.LoadAsync();

        Assert.Equal(new AppSettings().AccentColorHex, settings.AccentColorHex);
        Assert.Equal(new AppSettings().WidgetOpacity, settings.WidgetOpacity);
        Assert.Null(settings.WindowLeft);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsAllFields()
    {
        var sut = CreateSut();
        var settings = new AppSettings
        {
            AccentColorHex = "#EC4899",
            WidgetOpacity = 0.7,
            WindowLeft = 100,
            WindowTop = 200,
            WindowWidth = 340,
            WindowHeight = 560,
            NotificationsEnabled = false,
            ShowInTaskbar = false,
            AutoRescheduleOverdueTasks = true,
            HiddenGridColumns = ["Notes", "Due"],
            GridColumnsFrozen = false,
            GridSavedViews = [new GridSavedView { Name = "Compact", HiddenColumns = ["Category", "Notes"] }],
            WorkingHoursPerDay = 6.5,
            HourlyRate = 125.50m,
        };

        await sut.SaveAsync(settings);
        var loaded = await sut.LoadAsync();

        Assert.Equal(settings.AccentColorHex, loaded.AccentColorHex);
        Assert.Equal(settings.WidgetOpacity, loaded.WidgetOpacity);
        Assert.Equal(settings.WindowLeft, loaded.WindowLeft);
        Assert.Equal(settings.WindowTop, loaded.WindowTop);
        Assert.Equal(settings.WindowWidth, loaded.WindowWidth);
        Assert.Equal(settings.WindowHeight, loaded.WindowHeight);
        Assert.Equal(settings.NotificationsEnabled, loaded.NotificationsEnabled);
        Assert.Equal(settings.ShowInTaskbar, loaded.ShowInTaskbar);
        Assert.Equal(settings.AutoRescheduleOverdueTasks, loaded.AutoRescheduleOverdueTasks);
        Assert.Equal(settings.HiddenGridColumns, loaded.HiddenGridColumns);
        Assert.Equal(settings.GridColumnsFrozen, loaded.GridColumnsFrozen);
        Assert.Single(loaded.GridSavedViews);
        Assert.Equal("Compact", loaded.GridSavedViews[0].Name);
        Assert.Equal(settings.GridSavedViews[0].HiddenColumns, loaded.GridSavedViews[0].HiddenColumns);
        Assert.Equal(settings.WorkingHoursPerDay, loaded.WorkingHoursPerDay);
        Assert.Equal(settings.HourlyRate, loaded.HourlyRate);
    }

    [Fact]
    public async Task LoadAsync_WithCorruptFile_FallsBackToDefaultsInsteadOfThrowing()
    {
        var sut = CreateSut();
        var path = Path.Combine(_rootDirectory, "settings.json");
        await File.WriteAllTextAsync(path, "{ not valid json ");

        var settings = await sut.LoadAsync();

        Assert.Equal(new AppSettings().AccentColorHex, settings.AccentColorHex);
    }

    [Fact]
    public async Task SaveAsync_OverwritesPreviouslySavedFile()
    {
        var sut = CreateSut();
        await sut.SaveAsync(new AppSettings { AccentColorHex = "#3B82F6" });

        await sut.SaveAsync(new AppSettings { AccentColorHex = "#10B981" });
        var loaded = await sut.LoadAsync();

        Assert.Equal("#10B981", loaded.AccentColorHex);
    }
}
