using SDSM_App.Models;
using System.Windows;
using System.Windows.Controls;

namespace SDSM_App.Commons;

public class CountSelectorStyle : StyleSelector
{
    public Style? ZeroStyle { get; set; }

    public override Style? SelectStyle(object item, DependencyObject container)
    {
        if (item is ViewItem viewItem)
        {
            if (viewItem.Count == 0)
            {
                return ZeroStyle;
            }
        }
        return null;
    }    
}
