using System.Globalization;
using Avalonia.Data.Converters;

namespace DeskTodo.App.Converters;

/// <summary>Drives visibility/enabled-state for controls that only make sense once a string value exists — e.g. the Dashboard's report text area and its Copy/Save buttons, empty until a report is generated.</summary>
public sealed class StringNotEmptyConverter : IValueConverter
{
    public static readonly StringNotEmptyConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && !string.IsNullOrEmpty(s);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
