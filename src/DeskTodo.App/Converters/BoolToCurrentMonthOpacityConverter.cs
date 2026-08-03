using System.Globalization;
using Avalonia.Data.Converters;

namespace DeskTodo.App.Converters;

/// <summary>Dims the leading/trailing days from adjacent months that pad the calendar's fixed 7x6 grid — visually present (so the grid stays a stable rectangle) but clearly secondary.</summary>
public sealed class BoolToCurrentMonthOpacityConverter : IValueConverter
{
    public static readonly BoolToCurrentMonthOpacityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 1.0 : 0.35;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
