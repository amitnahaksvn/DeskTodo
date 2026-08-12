using System.Globalization;
using Avalonia.Data.Converters;

namespace DeskTodo.App.Converters;

/// <summary>Describes the App Lock section's current PIN state — see <see cref="ViewModels.SettingsViewModel.HasPinSet"/>.</summary>
public sealed class PinStatusTextConverter : IValueConverter
{
    public static readonly PinStatusTextConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "A PIN is set. Leave the fields below blank to keep it." : "No PIN set yet — enter one below to turn this on.";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
