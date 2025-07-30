using FMRRC_App.Helpers;
using SDSM_App.InjectableServices;
using SDSM_App.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// 일괄등록 양서파충류
/// </summary>
public partial class BulkAmphibianReptileControl : UserControl, ITransientService
{
    private readonly BulkAmphibianReptileControlViewModel _vm;

    private ScrollViewer? _importScrollViewer;
    private ScrollViewer? _convertedScrollViewer;

    public BulkAmphibianReptileControl(BulkAmphibianReptileControlViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        DataContext = _vm;

        DataRadGridView.Loaded += DataRadGridView_Loaded;
        ConvertedDataRadGridView.Loaded += ConvertedDataRadGridView_Loaded;

        DataRadGridView.Sorted += DataRadGridView_Sorted;
        ConvertedDataRadGridView.Sorted += ConvertedDataRadGridView_Sorted;

        SizeChanged += Page_SizeChanged;
    }

    #region This 이벤트

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var height = e.NewSize.Height - (SettingLayout.ActualHeight + 180);

        if (height > 60)
        {
            DataRadGridView.Height = height;
            ConvertedDataRadGridView.Height = height;
        }
        else
        {
            DataRadGridView.Height = 60;
            ConvertedDataRadGridView.Height = 60;
        }
    }

    #endregion

    #region 컨트롤 이벤트

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            if (e.Delta > 0)
                scrollViewer.LineLeft();
            else
                scrollViewer.LineRight();
        }
    }

    private void DataRadGridView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is RadGridView gridView)
        {
            _importScrollViewer = gridView.ChildrenOfType<ScrollViewer>().FirstOrDefault();
            if (_importScrollViewer != null)
                _importScrollViewer.ScrollChanged += ImportScrollViewer_ScrollChanged;
        }
    }

    private void ConvertedDataRadGridView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is RadGridView gridView)
        {
            _convertedScrollViewer = gridView.ChildrenOfType<ScrollViewer>().FirstOrDefault();
            if (_convertedScrollViewer != null)
                _convertedScrollViewer.ScrollChanged += ConvertedScrollViewer_ScrollChanged;
        }

        //RadGridViewr가 화면에 보여야 ScrollViewer 대입이 가능 => 대입 후 변환된 자료 숨기기
        ConvertedRadToggleButton.IsChecked = false;
    }

    private void DataRadGridView_Sorted(object? sender, GridViewSortedEventArgs e)
    {
        var sorts = DataRadGridView.SortDescriptors;

        ConvertedDataRadGridView.SortDescriptors.Clear();

        if (sorts.Count != 0)
        {
            ConvertedDataRadGridView.SortDescriptors.AddRange(sorts);
        }
    }

    private void ConvertedDataRadGridView_Sorted(object? sender, GridViewSortedEventArgs e)
    {
        var sorts = ConvertedDataRadGridView.SortDescriptors;

        DataRadGridView.SortDescriptors.Clear();

        if (sorts.Count != 0)
        {
            DataRadGridView.SortDescriptors.AddRange(sorts);
        }
    }


    //엑셀출력
    private void DataExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadButton button && button.Tag is string tag)
        {
            if (tag == "import")
            {
                DataExportHelper.ExportGridViewToExcel(
                    DataRadGridView,
                    "양서파충류_가져온자료",
                    progress => DataRadGridView.BusyIndicatorProgressValue = progress,
                    () =>
                    {
                        //추가 완료 처리 필요시 구현 
                        // UI는 콜백에서 변경

                        DataRadGridView.BusyIndicatorProgressValue = 0;
                    }
                );
            }
            else
            {
                DataExportHelper.ExportGridViewToExcel(
                    ConvertedDataRadGridView,
                    "양서파충류_변환된자료",
                    progress => ConvertedDataRadGridView.BusyIndicatorProgressValue = progress,
                    () =>
                    {
                        //추가 완료 처리 필요시 구현 
                        // UI는 콜백에서 변경

                        ConvertedDataRadGridView.BusyIndicatorProgressValue = 0;
                    }
                );
            }
        }
    }

    #endregion

    #region 메서드

    private void ImportScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _convertedScrollViewer?.ScrollToVerticalOffset(e.VerticalOffset);
        _convertedScrollViewer?.ScrollToHorizontalOffset(e.HorizontalOffset);
    }

    private void ConvertedScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _importScrollViewer?.ScrollToVerticalOffset(e.VerticalOffset);
        _importScrollViewer?.ScrollToHorizontalOffset(e.HorizontalOffset);
    }

    #endregion
}
