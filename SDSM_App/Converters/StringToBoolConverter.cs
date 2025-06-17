using System.Globalization;
using System.Windows.Data;

namespace SDSM_App.Converters;

public class StringToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null)
        {
            return !string.IsNullOrWhiteSpace(value.ToString());
        }
        else
        {
            return false;
        }
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}
