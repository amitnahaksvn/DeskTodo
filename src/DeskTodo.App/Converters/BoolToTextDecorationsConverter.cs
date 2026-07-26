using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DeskTodo.App.Converters;

/// <summary>Strikes through completed tasks' titles.</summary>
public sealed class BoolToTextDecorationsConverter : IValueConverter
{
    public static readonly BoolToTextDecorationsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? TextDecorations.Strikethrough : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
