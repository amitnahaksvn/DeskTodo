using System.Globalization;
using Avalonia.Data.Converters;

namespace DeskTodo.App.Converters;

/// <summary>Labels the "New PIN" field's watermark as "Change PIN" once one already exists, so it doesn't read as "you have no PIN" when one is already set.</summary>
public sealed class PinFieldWatermarkConverter : IValueConverter
{
    public static readonly PinFieldWatermarkConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "New PIN (leave blank to keep current)" : "New PIN (4+ digits)";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
