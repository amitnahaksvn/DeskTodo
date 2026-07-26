using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DeskTodo.App.Converters;

/// <summary>Converts a "#RRGGBB"/"#AARRGGBB" hex string (as stored on <c>TaskItem</c>/<c>Category</c>) into a brush for binding.</summary>
public sealed class HexColorToBrushConverter : IValueConverter
{
    public static readonly HexColorToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string hex && !string.IsNullOrWhiteSpace(hex) && Color.TryParse(hex, out var color)
            ? new SolidColorBrush(color)
            : Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
