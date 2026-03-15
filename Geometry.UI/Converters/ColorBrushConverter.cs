using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Geometry.UI.Converters
{
    public class ColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color color;

            if (value is Color c)
            {
                color = c;
            }
            else
            {
                return new SolidColorBrush(Colors.Black);
            }

            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color;
            }
            return Colors.Black;
        }
    }
}
