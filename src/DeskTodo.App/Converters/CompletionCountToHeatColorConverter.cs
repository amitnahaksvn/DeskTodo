using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace DeskTodo.App.Converters;

/// <summary>
/// Maps a day's completed-task count to a GitHub-contribution-graph-style green intensity
/// for the Dashboard's Heat Map — 5 levels (0, 1–2, 3–4, 5–6, 7+), the same "a glance should
/// read as density" convention that graph made familiar. The zero-level base color is
/// theme-aware (Avalonia's <c>Application.Current.ActualThemeVariant</c>, checked at each
/// convert — see <see cref="BoolToTodayBackgroundConverter"/>'s doc comment for why that's a
/// per-convert check rather than a live <c>DynamicResource</c>-style update); the four green
/// levels stay literal in both themes, the same "a spot color stays itself" choice this
/// phase made for the app's accent/preset colors generally.
/// </summary>
public sealed class CompletionCountToHeatColorConverter : IValueConverter
{
    public static readonly CompletionCountToHeatColorConverter Instance = new();

    private static readonly IBrush ZeroLevelLight = new SolidColorBrush(Color.Parse("#E2E8F0"));
    private static readonly IBrush ZeroLevelDark = new SolidColorBrush(Color.Parse("#334155"));

    private static readonly IBrush[] GreenLevels =
    [
        new SolidColorBrush(Color.Parse("#BBF7D0")),
        new SolidColorBrush(Color.Parse("#86EFAC")),
        new SolidColorBrush(Color.Parse("#4ADE80")),
        new SolidColorBrush(Color.Parse("#16A34A")),
    ];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var count = value is int i ? i : 0;
        var level = count switch
        {
            0 => 0,
            1 or 2 => 1,
            3 or 4 => 2,
            5 or 6 => 3,
            _ => 4,
        };

        if (level == 0)
        {
            var isDark = global::Avalonia.Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
            return isDark ? ZeroLevelDark : ZeroLevelLight;
        }

        return GreenLevels[level - 1];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
