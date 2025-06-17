using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace SDSM_App.Behaviors;

public class RadGridView_ScrollSelectedIntoViewBehavior
{
    public static readonly DependencyProperty ScrollSelectedIntoViewProperty =
        DependencyProperty.Register("ScrollSelectedIntoView", typeof(bool), typeof(RadGridView), new FrameworkPropertyMetadata(ScrollSelectedIntoViewPropertyChanged));

    public static bool GetScrollSelectedIntoView(DependencyObject obj)
    {
        return (bool)obj.GetValue(ScrollSelectedIntoViewProperty);
    }

    public static void SetScrollSelectedIntoView(DependencyObject obj, bool value)
    {
        obj.SetValue(ScrollSelectedIntoViewProperty, value);
    }

    private static void ScrollSelectedIntoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RadGridView grid)
        {
            var shouldScrollSelectedItemIntoView = (bool)e.NewValue;
            if (grid != null && shouldScrollSelectedItemIntoView)
            {
                grid.SelectionChanged += Grid_SelectionChanged!;
            }
        }
    }

    private static void Grid_SelectionChanged(object sender, SelectionChangeEventArgs e)
    {
        if(sender is RadGridView grid)
        {
            object selectedItem = grid.SelectedItem;

            if (selectedItem != null)
            {
                var scrollOffset = grid.Items.IndexOf(selectedItem) * grid.RowHeight;
                var scrollViewer = grid.ChildrenOfType<GridViewScrollViewer>().First();
                var verticalOffset = scrollViewer.VerticalOffset;
                var viewportHeight = scrollViewer.ViewportHeight;

                if (scrollOffset < verticalOffset || scrollOffset > (verticalOffset + viewportHeight))
                {
                    scrollViewer.ScrollToVerticalOffset(scrollOffset);
                }
            }
        }
    }
}
