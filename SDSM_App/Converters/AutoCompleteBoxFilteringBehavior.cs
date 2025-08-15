using System.Collections;
using Telerik.Windows.Controls;

namespace SDSM_App.Converters;

public class AutoCompleteBoxFilteringBehavior : FilteringBehavior
{
    //일치하는 항목이 없어도 드롭다운 목록이 채워짐
    public override IEnumerable<object> FindMatchingItems(string searchText, IList items, IEnumerable<object> escapedItems, string textSearchPath, TextSearchMode textSearchMode)
    {
        var result = base.FindMatchingItems(searchText, items, escapedItems, textSearchPath, textSearchMode) as IEnumerable<object>;

        if (string.IsNullOrEmpty(searchText) || !result.Any())
        {
            return ((IEnumerable<object>)items).Where(x => !escapedItems.Contains(x));
        }
        return result;
    }
}
