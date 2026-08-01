using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace DeskTodo.App.Converters;

/// <summary>Indents a subtask's row further than a top-level task's — the widget's only visual cue that a row is nested under a parent.</summary>
public sealed class IsSubtaskToRowMarginConverter : IValueConverter
{
    public static readonly IsSubtaskToRowMarginConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? new Thickness(26, 7, 6, 7) : new Thickness(6, 7, 6, 7);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
