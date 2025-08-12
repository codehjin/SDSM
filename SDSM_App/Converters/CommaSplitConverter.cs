using System.Globalization;
using System.Windows.Data;

namespace SDSM_App.Converters;

public class CommaSplitConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrWhiteSpace(str))
        {
            // 콤마로 분리, 공백 제거, 빈 값 제외
            return str.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // 필요시 구현, 일반적으로 사용하지 않음
        return Binding.DoNothing;
    }
}
