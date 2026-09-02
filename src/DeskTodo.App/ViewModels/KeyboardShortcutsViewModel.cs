using System.Collections.ObjectModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Keyboard Shortcut Manager window (Feature 77, Roadmap-39-100.md) — display
/// defaults, edit a shortcut (capture the next key press), detect conflicts, restore default,
/// export/import the override set as JSON.
/// </summary>
public sealed partial class KeyboardShortcutsViewModel(ISettingsService settingsService, ILogger<KeyboardShortcutsViewModel> logger) : ViewModelBase
{
    public ObservableCollection<KeyboardShortcutOption> Shortcuts { get; } = [];

    [ObservableProperty]
    public partial string? CapturingCommandId { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial string ExportImportText { get; set; } = string.Empty;

    // System.Text.Json's default encoder unicode-escapes '+' (HTML-safety escaping meant for
    // embedding JSON inside a script tag) — this JSON is meant for a user to read and
    // copy/paste, where a plain, unescaped '+' is far more legible.
    private static readonly JsonSerializerOptions ExportOptions = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await settingsService.LoadAsync(cancellationToken);
            var effectiveCombos = KeyboardShortcutDefinition.All
                .ToDictionary(d => d.CommandId, d => settings.KeyboardShortcutOverrides.GetValueOrDefault(d.CommandId, d.DefaultCombo));

            Shortcuts.Clear();
            foreach (var definition in KeyboardShortcutDefinition.All)
            {
                var effective = effectiveCombos[definition.CommandId];
                var hasConflict = effectiveCombos.Any(kv => kv.Key != definition.CommandId && kv.Value == effective);
                Shortcuts.Add(new KeyboardShortcutOption(
                    definition.CommandId,
                    definition.DisplayName,
                    definition.Scope,
                    effective,
                    settings.KeyboardShortcutOverrides.ContainsKey(definition.CommandId),
                    hasConflict));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load keyboard shortcuts");
        }
    }

    [RelayCommand]
    private void BeginCapture(string commandId) => CapturingCommandId = commandId;

    [RelayCommand]
    private void CancelCapture() => CapturingCommandId = null;

    /// <summary>Called by the window's code-behind once it's captured a key press while <see cref="CapturingCommandId"/> is set.</summary>
    public async Task ApplyCapturedComboAsync(string commandId, string combo, CancellationToken cancellationToken = default)
    {
        CapturingCommandId = null;

        try
        {
            var settings = await settingsService.LoadAsync(cancellationToken);
            settings.KeyboardShortcutOverrides[commandId] = combo;
            await settingsService.SaveAsync(settings, cancellationToken);
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to rebind shortcut '{CommandId}'", commandId);
        }
    }

    [RelayCommand]
    private async Task ResetToDefaultAsync(string commandId)
    {
        try
        {
            var settings = await settingsService.LoadAsync();
            settings.KeyboardShortcutOverrides.Remove(commandId);
            await settingsService.SaveAsync(settings);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reset shortcut '{CommandId}' to default", commandId);
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        try
        {
            var settings = await settingsService.LoadAsync();
            ExportImportText = JsonSerializer.Serialize(settings.KeyboardShortcutOverrides, ExportOptions);
            StatusMessage = "Exported below — copy it to save.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to export keyboard shortcuts");
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        try
        {
            var overrides = JsonSerializer.Deserialize<Dictionary<string, string>>(ExportImportText);
            if (overrides is null)
            {
                StatusMessage = "Nothing to import.";
                return;
            }

            var validCommandIds = KeyboardShortcutDefinition.All.Select(d => d.CommandId).ToHashSet();
            var settings = await settingsService.LoadAsync();
            settings.KeyboardShortcutOverrides = overrides.Where(kv => validCommandIds.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
            await settingsService.SaveAsync(settings);
            await LoadAsync();
            StatusMessage = "Imported.";
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse imported keyboard shortcut JSON");
            StatusMessage = "That isn't valid JSON.";
        }
    }
}
