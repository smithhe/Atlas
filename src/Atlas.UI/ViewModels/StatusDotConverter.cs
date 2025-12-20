using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Atlas.UI.ViewModels;

public sealed class StatusDotConverter : IValueConverter
{
    public static StatusDotConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = (value as string ?? "").Trim();

        // accepts: Critical/High, Warning/Medium, Ok/Low, Info
        return s.ToLowerInvariant() switch
        {
            "critical" => GetBrush("DotRedBrush"),
            "high" => GetBrush("DotRedBrush"),
            "warning" => GetBrush("DotYellowBrush"),
            "medium" => GetBrush("DotYellowBrush"),
            "ok" => GetBrush("DotGreenBrush"),
            "low" => GetBrush("DotGreenBrush"),
            "info" => GetBrush("DotBlueBrush"),
            _ => GetBrush("DotBlueBrush")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static IBrush GetBrush(string key)
        => (IBrush)(Avalonia.Application.Current?.Resources[key] ?? Brushes.Gray);
}


