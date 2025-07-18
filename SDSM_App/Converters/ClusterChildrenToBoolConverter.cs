using SDSM_App.Models;
using System.Globalization;
using System.Windows.Data;

namespace SDSM_App.Converters;
public class ClusterChildrenToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null)
        {
            IEnumerable<object> data = (IEnumerable<object>)value;

            try
            {
                foreach (MapModel item in data.Cast<MapModel>())
                {
                    if (item.IsSelected)
                        return true;
                }
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null!;
    }
}
