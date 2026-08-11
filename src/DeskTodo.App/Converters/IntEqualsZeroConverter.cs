using System.Globalization;
using Avalonia.Data.Converters;

namespace DeskTodo.App.Converters;

/// <summary>Drives the Dashboard's "No completed or in-progress tasks yet" empty-state text — visible only when the bound count is zero.</summary>
public sealed class IntEqualsZeroConverter : IValueConverter
{
    public static readonly IntEqualsZeroConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int count && count == 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
