using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DeskTodo.App.Converters;

/// <summary>Highlights today's cell in the calendar month grid; every other cell stays transparent (the row's own border still shows).</summary>
public sealed class BoolToTodayBackgroundConverter : IValueConverter
{
    public static readonly BoolToTodayBackgroundConverter Instance = new();

    private static readonly IBrush TodayBrush = new SolidColorBrush(Color.Parse("#DBEAFE"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? TodayBrush : Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
