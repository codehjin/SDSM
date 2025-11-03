using FMRRC_App.Helpers;
using SDSM_App.InjectableServices;
using SDSM_App.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// 일괄등록 식생단면
/// </summary>
public partial class BulkVegetationENVControl : UserControl, ITransientService
{
    private readonly BulkVegetationENVControlViewModel _vm;

    private ScrollViewer? _importScrollViewer;
    private ScrollViewer? _convertedScrollViewer;

    private ScrollViewer? _importSpeciesScrollViewer;
    private ScrollViewer? _convertedSpeciesScrollViewer;

    public BulkVegetationENVControl(BulkVegetationENVControlViewModel vm)
    {
        InitializeComponent();

        InitializeComponent();

        _vm = vm;
        DataContext = _vm;

        DataRadGridView.Loaded += DataRadGridView_Loaded;
        ConvertedDataRadGridView.Loaded += ConvertedDataRadGridView_Loaded;
        SpeciesDataRadGridView.Loaded += SpeciesDataRadGridView_Loaded;
        ConvertedSpeciesDataRadGridView.Loaded += ConvertedSpeciesDataRadGridView_Loaded;

        DataRadGridView.Sorted += DataRadGridView_Sorted;
        ConvertedDataRadGridView.Sorted += ConvertedDataRadGridView_Sorted;
        SpeciesDataRadGridView.Sorted += SpeciesDataRadGridView_Sorted;
        ConvertedSpeciesDataRadGridView.Sorted += ConvertedSpeciesDataRadGridView_Sorted;

        SizeChanged += Page_SizeChanged;
    }

    #region This 이벤트

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var height = e.NewSize.Height - (SettingLayout.ActualHeight + 180);
        var heightSpecies = e.NewSize.Height - 60;

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

        if (heightSpecies > 60)
        {
            SpeciesDataRadGridView.Height = heightSpecies;
            ConvertedSpeciesDataRadGridView.Height = heightSpecies;
        }
        else
        {
            SpeciesDataRadGridView.Height = 60;
            ConvertedSpeciesDataRadGridView.Height = 60;
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

    private void SpeciesDataRadGridView_Loaded(object sender, RoutedEventArgs e)
    {
        if (SpeciesLayout.Visibility == Visibility.Collapsed)
            SpeciesLayout.Visibility = Visibility.Visible;

        if (sender is RadGridView gridView)
        {
            _importSpeciesScrollViewer = gridView.ChildrenOfType<ScrollViewer>().FirstOrDefault();
            if (_importSpeciesScrollViewer != null)
                _importSpeciesScrollViewer.ScrollChanged += ImportSpeciesScrollViewer_ScrollChanged;
        }
        if (SpeciesLayout.Visibility == Visibility.Visible)
            SpeciesLayout.Visibility = Visibility.Collapsed;
    }

    private void ConvertedSpeciesDataRadGridView_Loaded(object sender, RoutedEventArgs e)
    {
        if (SpeciesLayout.Visibility == Visibility.Collapsed)
            SpeciesLayout.Visibility = Visibility.Visible;

        if (sender is RadGridView gridView)
        {
            _convertedSpeciesScrollViewer = gridView.ChildrenOfType<ScrollViewer>().FirstOrDefault();
            if (_convertedSpeciesScrollViewer != null)
                _convertedSpeciesScrollViewer.ScrollChanged += ConvertedSpeciesScrollViewer_ScrollChanged;
        }

        if (SpeciesLayout.Visibility == Visibility.Visible)
            SpeciesLayout.Visibility = Visibility.Collapsed;
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

    private void SpeciesDataRadGridView_Sorted(object? sender, GridViewSortedEventArgs e)
    {
        var sorts = SpeciesDataRadGridView.SortDescriptors;

        ConvertedSpeciesDataRadGridView.SortDescriptors.Clear();

        if (sorts.Count != 0)
        {
            ConvertedSpeciesDataRadGridView.SortDescriptors.AddRange(sorts);
        }
    }

    private void ConvertedSpeciesDataRadGridView_Sorted(object? sender, GridViewSortedEventArgs e)
    {
        var sorts = ConvertedSpeciesDataRadGridView.SortDescriptors;

        SpeciesDataRadGridView.SortDescriptors.Clear();

        if (sorts.Count != 0)
        {
            SpeciesDataRadGridView.SortDescriptors.AddRange(sorts);
        }
    }

    //종목록 보기
    private void SpeciesVisibleRadButton_Click(object sender, RoutedEventArgs e)
    {
        SpeciesLayout.Visibility = Visibility.Visible;
    }

    //종목록 숨기기
    private void SpeciesCollapsedRadButton_Click(object sender, RoutedEventArgs e)
    {
        SpeciesLayout.Visibility = Visibility.Collapsed;
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
                    "식생단면_환경_가져온자료",
                    progress => DataRadGridView.BusyIndicatorProgressValue = progress,
                    () =>
                    {
                        //추가 완료 처리 필요시 구현 
                        // UI는 콜백에서 변경

                        DataRadGridView.BusyIndicatorProgressValue = 0;
                    }
                );
            }
            else if (tag == "converted")
            {
                DataExportHelper.ExportGridViewToExcel(
                    ConvertedDataRadGridView,
                    "식생단면_환경_변환된자료",
                    progress => ConvertedDataRadGridView.BusyIndicatorProgressValue = progress,
                    () =>
                    {
                        //추가 완료 처리 필요시 구현 
                        // UI는 콜백에서 변경

                        ConvertedDataRadGridView.BusyIndicatorProgressValue = 0;
                    }
                );
            }
            else if (tag == "importspecies")
            {
                DataExportHelper.ExportGridViewToExcel(
                    SpeciesDataRadGridView,
                    "식생단면_군락_가져온자료",
                    progress => SpeciesDataRadGridView.BusyIndicatorProgressValue = progress,
                    () =>
                    {
                        //추가 완료 처리 필요시 구현 
                        // UI는 콜백에서 변경

                        SpeciesDataRadGridView.BusyIndicatorProgressValue = 0;
                    }
                );
            }
            else if (tag == "convertedspecies")
            {
                DataExportHelper.ExportGridViewToExcel(
                   ConvertedSpeciesDataRadGridView,
                   "식생단면_군락_변환된자료",
                   progress => ConvertedSpeciesDataRadGridView.BusyIndicatorProgressValue = progress,
                   () =>
                   {
                       //추가 완료 처리 필요시 구현 
                       // UI는 콜백에서 변경

                       ConvertedSpeciesDataRadGridView.BusyIndicatorProgressValue = 0;
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

    private void ImportSpeciesScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _convertedSpeciesScrollViewer?.ScrollToVerticalOffset(e.VerticalOffset);
        _convertedSpeciesScrollViewer?.ScrollToHorizontalOffset(e.HorizontalOffset);
    }

    private void ConvertedSpeciesScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _importSpeciesScrollViewer?.ScrollToVerticalOffset(e.VerticalOffset);
        _importSpeciesScrollViewer?.ScrollToHorizontalOffset(e.HorizontalOffset);
    }

    #endregion
}
