using System.Globalization;
using Avalonia.Data.Converters;

namespace DeskTodo.App.Converters;

/// <summary>Dims an archived project's row so it reads as "put away" without disappearing entirely — same idea as <see cref="BoolToCurrentMonthOpacityConverter"/>, different trigger.</summary>
public sealed class BoolToArchivedOpacityConverter : IValueConverter
{
    public static readonly BoolToArchivedOpacityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 0.5 : 1.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
