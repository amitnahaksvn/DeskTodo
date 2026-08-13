using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace DeskTodo.App.Converters;

/// <summary>
/// Highlights today's cell in the calendar month grid; every other cell stays transparent
/// (the row's own border still shows). Picks its color from Avalonia's
/// <c>Application.Current.ActualThemeVariant</c> at each convert — unlike XAML's own
/// <c>DynamicResource</c>, a converter's output doesn't get automatically re-resolved on a
/// live theme change, so this only updates the next time <c>IsToday</c> itself changes (i.e.
/// the next day, or the window being reopened) — a known, narrow limitation, not something
/// every other themed color in this app shares.
/// </summary>
public sealed class BoolToTodayBackgroundConverter : IValueConverter
{
    public static readonly BoolToTodayBackgroundConverter Instance = new();

    private static readonly IBrush TodayBrushLight = new SolidColorBrush(Color.Parse("#DBEAFE"));
    private static readonly IBrush TodayBrushDark = new SolidColorBrush(Color.Parse("#1E3A5F"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not true)
        {
            return Brushes.Transparent;
        }

        var isDark = global::Avalonia.Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        return isDark ? TodayBrushDark : TodayBrushLight;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
