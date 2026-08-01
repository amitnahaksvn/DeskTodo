using System.Globalization;
using Avalonia.Data.Converters;

namespace DeskTodo.App.Converters;

/// <summary>Labels the Notes section's preview/edit toggle button with the action it performs next, not the current state.</summary>
public sealed class BoolToNotesToggleLabelConverter : IValueConverter
{
    public static readonly BoolToNotesToggleLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "Edit" : "Preview";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
