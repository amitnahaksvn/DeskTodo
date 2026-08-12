using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Security;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs Phase 29's lock screen — shown by <c>App.OnFrameworkInitializationCompleted</c>
/// instead of the widget when <see cref="Application.Settings.AppSettings.PinLockEnabled"/>
/// is on, gating everything else behind a correct PIN. Deliberately has no dependency on
/// <c>WidgetViewModel</c> — it only needs to verify a PIN and announce success, the same
/// "give the item what it needs directly" scoping <see cref="CommandPaletteViewModel"/> uses.
/// </summary>
public sealed partial class LockScreenViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<LockScreenViewModel> _logger;

    public LockScreenViewModel(ISettingsService settingsService, ILogger<LockScreenViewModel> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    [ObservableProperty]
    public partial string EnteredPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Raised once the entered PIN verifies — the view closes itself and shows the widget in response.</summary>
    public event EventHandler? Unlocked;

    [RelayCommand]
    private async Task UnlockAsync()
    {
        try
        {
            var settings = await _settingsService.LoadAsync();
            if (PinHasher.Verify(EnteredPin, settings.PinSalt, settings.PinHash))
            {
                ErrorMessage = string.Empty;
                Unlocked?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ErrorMessage = "Incorrect PIN.";
                EnteredPin = string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify the lock screen PIN");
            ErrorMessage = "Something went wrong — try again.";
        }
    }
}
