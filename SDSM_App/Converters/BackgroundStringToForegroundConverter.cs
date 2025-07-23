using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SDSM_App.Converters;

public class BackgroundStringToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrEmpty(str))
        {
            var color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(str)).Color;
            // 명도 계산 (YIQ 공식)
            double yiq = ((color.R * 299) + (color.G * 587) + (color.B * 114)) / 1000;
            return yiq >= 128 ? Brushes.Black : Brushes.White;
        }
        return Brushes.Black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null!;
    }
}
