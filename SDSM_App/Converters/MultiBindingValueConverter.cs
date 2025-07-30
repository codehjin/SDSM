using System.Globalization;
using System.Windows.Data;

namespace SDSM_App.Converters;

public class MultiBindingValueConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // Tuple로 반환
        return Tuple.Create(values[0], values[1]);
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return null!;
    }
}
