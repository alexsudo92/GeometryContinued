using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace Geometry.UI.Avalonia.Converters;

public class ColorBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Color color ? new SolidColorBrush(color) : new SolidColorBrush(Colors.Black);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is SolidColorBrush brush ? brush.Color : Colors.Black;
}
