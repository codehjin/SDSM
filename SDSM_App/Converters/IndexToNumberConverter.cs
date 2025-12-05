using System.Globalization;
using System.Windows.Data;

namespace SDSM_App.Converters;

/// <summary>
/// 페이지 인덱스를 +1 숫자로
/// </summary>
public class IndexToNumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (int)value + 1;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (int)value - 1;
    }
}
