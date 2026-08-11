using System.Globalization;
using Avalonia.Data.Converters;

namespace DeskTodo.App.Converters;

/// <summary>Swaps the header's mini-widget toggle glyph depending on which direction it currently toggles.</summary>
public sealed class MiniWidgetModeToGlyphConverter : IValueConverter
{
    public static readonly MiniWidgetModeToGlyphConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "⬚" : "−"; // dotted square (expand back) : minus (collapse to mini)

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
