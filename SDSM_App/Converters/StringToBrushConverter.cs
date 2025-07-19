using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SDSM_App.Converters;

public class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string str && !string.IsNullOrEmpty(str)
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(str))
            : new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null!;    
    }
}
