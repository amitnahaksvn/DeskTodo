using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DeskTodo.App.Converters;

/// <summary>Maps a day's completed-task count to a GitHub-contribution-graph-style green intensity for the Dashboard's Heat Map — 5 levels (0, 1–2, 3–4, 5–6, 7+), the same "a glance should read as density" convention that graph made familiar.</summary>
public sealed class CompletionCountToHeatColorConverter : IValueConverter
{
    public static readonly CompletionCountToHeatColorConverter Instance = new();

    private static readonly IBrush[] Levels =
    [
        new SolidColorBrush(Color.Parse("#E2E8F0")),
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
        return Levels[level];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
