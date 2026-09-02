using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class KeyboardShortcutsViewModelTests
{
    private readonly Mock<ISettingsService> _settingsService = new();
    private readonly KeyboardShortcutsViewModel _sut;

    public KeyboardShortcutsViewModelTests()
    {
        _sut = new KeyboardShortcutsViewModel(_settingsService.Object, NullLogger<KeyboardShortcutsViewModel>.Instance);
    }

    private void SetupSettings(AppSettings settings) =>
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

    [Fact]
    public async Task LoadAsync_WithNoOverrides_ShowsEveryDefinitionAtItsDefault()
    {
        SetupSettings(new AppSettings());

        await _sut.LoadAsync();

        Assert.Equal(KeyboardShortcutDefinition.All.Count, _sut.Shortcuts.Count);
        Assert.All(_sut.Shortcuts, s => Assert.False(s.IsCustomized));
        Assert.Contains(_sut.Shortcuts, s => s.CommandId == "Undo" && s.EffectiveCombo == "Mod+Z");
    }

    [Fact]
    public async Task LoadAsync_WithAnOverride_ShowsItAsCustomized()
    {
        SetupSettings(new AppSettings { KeyboardShortcutOverrides = new Dictionary<string, string> { ["Undo"] = "Mod+U" } });

        await _sut.LoadAsync();

        var undo = _sut.Shortcuts.Single(s => s.CommandId == "Undo");
        Assert.True(undo.IsCustomized);
        Assert.Equal("Mod+U", undo.EffectiveCombo);
    }

    [Fact]
    public async Task LoadAsync_WithTwoShortcutsSharingACombo_FlagsBothAsConflicting()
    {
        SetupSettings(new AppSettings { KeyboardShortcutOverrides = new Dictionary<string, string> { ["Undo"] = "Mod+K" } });

        await _sut.LoadAsync();

        Assert.True(_sut.Shortcuts.Single(s => s.CommandId == "Undo").HasConflict);
        Assert.True(_sut.Shortcuts.Single(s => s.CommandId == "CommandPalette").HasConflict);
        Assert.False(_sut.Shortcuts.Single(s => s.CommandId == "Redo").HasConflict);
    }

    [Fact]
    public async Task ApplyCapturedComboAsync_SavesTheOverride_AndClearsCapturing()
    {
        var settings = new AppSettings();
        SetupSettings(settings);
        _sut.BeginCaptureCommand.Execute("Undo");

        await _sut.ApplyCapturedComboAsync("Undo", "Mod+U");

        Assert.Null(_sut.CapturingCommandId);
        _settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => a.KeyboardShortcutOverrides["Undo"] == "Mod+U"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetToDefaultAsync_RemovesTheOverride()
    {
        var settings = new AppSettings { KeyboardShortcutOverrides = new Dictionary<string, string> { ["Undo"] = "Mod+U" } };
        SetupSettings(settings);

        await _sut.ResetToDefaultCommand.ExecuteAsync("Undo");

        _settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => !a.KeyboardShortcutOverrides.ContainsKey("Undo")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CancelCaptureCommand_ClearsCapturingCommandId()
    {
        _sut.BeginCaptureCommand.Execute("Undo");

        _sut.CancelCaptureCommand.Execute(null);

        Assert.Null(_sut.CapturingCommandId);
    }

    [Fact]
    public async Task ExportAsync_ProducesValidJsonOfTheOverrides()
    {
        SetupSettings(new AppSettings { KeyboardShortcutOverrides = new Dictionary<string, string> { ["Undo"] = "Mod+U" } });

        await _sut.ExportCommand.ExecuteAsync(null);

        Assert.Contains("Mod+U", _sut.ExportImportText);
    }

    [Fact]
    public async Task ImportAsync_WithValidJson_ReplacesTheOverrides()
    {
        var settings = new AppSettings();
        SetupSettings(settings);
        _sut.ExportImportText = """{"Undo": "Mod+U"}""";

        await _sut.ImportCommand.ExecuteAsync(null);

        _settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => a.KeyboardShortcutOverrides["Undo"] == "Mod+U"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_IgnoresUnknownCommandIds()
    {
        var settings = new AppSettings();
        SetupSettings(settings);
        _sut.ExportImportText = """{"Undo": "Mod+U", "NotARealCommand": "Mod+X"}""";

        await _sut.ImportCommand.ExecuteAsync(null);

        _settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a =>
            a.KeyboardShortcutOverrides.ContainsKey("Undo") && !a.KeyboardShortcutOverrides.ContainsKey("NotARealCommand")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WithInvalidJson_SetsAStatusMessage_AndDoesNotThrow()
    {
        SetupSettings(new AppSettings());
        _sut.ExportImportText = "not json";

        await _sut.ImportCommand.ExecuteAsync(null);

        Assert.NotNull(_sut.StatusMessage);
    }
}
