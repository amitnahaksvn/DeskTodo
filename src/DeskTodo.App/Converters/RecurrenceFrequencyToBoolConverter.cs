using System.Globalization;
using Avalonia.Data.Converters;
using DeskTodo.Domain.Enums;

namespace DeskTodo.App.Converters;

/// <summary>Enables the editor's "Every N" interval and "Ends" date controls only once a recurrence frequency is actually chosen.</summary>
public sealed class RecurrenceFrequencyToBoolConverter : IValueConverter
{
    public static readonly RecurrenceFrequencyToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is RecurrenceFrequency and not RecurrenceFrequency.None;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
