using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.ViewModels;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Telerik.Documents.SpreadsheetStreaming;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView.SpreadsheetStreamingExport;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// 일괄등록 수질
/// </summary>
public partial class BulkWaterQualityControl : UserControl, ITransientService
{
    private readonly BulkWaterQualityControlViewModel _vm;

    private ScrollViewer? _importScrollViewer;
    private ScrollViewer? _convertedScrollViewer;

    private GridViewSpreadStreamExport? _spreadStreamExport;
    private RadGridView _exportRadGridView = new();

    public BulkWaterQualityControl(BulkWaterQualityControlViewModel vm)
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

    //표준양식 출력
    private void FormExportRadButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            CommonOpenFileDialog dialog = new()
            {
                IsFolderPicker = true,
                Title = "다운로드 - 폴더선택"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (string.IsNullOrWhiteSpace(dialog.FileName))
                    return;

                var sri = Application.GetResourceStream(new Uri("pack://application:,,,/SDSM_App;component/Assets/Excels/수질_표준양식.xlsx")).Stream;

                if (sri != null)
                {
                    string path = Path.Combine(dialog.FileName, $"수질_표준양식_{DateTime.Now:yyyyMMddHHmmss}.xlsx");

                    using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
                    sri.CopyTo(fileStream);

                    var toast = (GB.SaveMessage, true);
                    WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
                }
                else
                {
                    MessageBox.Show("파일을 찾을 수 없습니다.", "파일 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(FormExportRadButton_Click), ex.Message);

            var toast = (ex.Message, false);
            WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
        }
    }

    //엑셀출력
    private void DataExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadButton button && button.Tag is string tag)
        {
            if (tag == "import")
            {
                _exportRadGridView = DataRadGridView;
                ExcelExportAsync("수질_가져온자료");
            }
            else
            {
                _exportRadGridView = ConvertedDataRadGridView;
                ExcelExportAsync("수질_변환된자료");
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

    #region 출력메서드

    private void ExcelExportAsync(string caption)
    {
        try
        {
            string fileName = $"{caption}_{DateTime.Now:yyyyMMddHHmmss}";
            string fileFullName = string.Empty;

            SaveFileDialog dialog = new()
            {
                DefaultExt = "xlsx",
                Filter = $"엑셀파일|*.xlsx",
                FileName = fileName,
            };

            if (dialog.ShowDialog() == true)
            {
                fileFullName = dialog.FileName;
            }

            if (string.IsNullOrWhiteSpace(fileFullName))
                return;

            GB.IsBusy = true;
            GB.BusyIndicatorVisibility = Visibility.Collapsed;

            _spreadStreamExport = new GridViewSpreadStreamExport(_exportRadGridView)
            {
                ExportFormat = SpreadStreamExportFormat.Xlsx,
                ShowLoadingIndicatorWhileAsyncExport = true,
                SheetName = caption,
            };
            _spreadStreamExport.ElementExportingToDocument += (s, e) =>
            {
                if (e.Element == SpreadStreamExportElement.Cell)
                {
                    if (e.Value == null)
                        return;

                    if (int.TryParse(e.Value.ToString(), out var i))
                    {
                        e.Value = i;

                        e.Style = new SpreadStreamCellStyle()
                        {
                            HorizontalAlignment = SpreadHorizontalAlignment.Center,
                            VerticalAlignment = SpreadVerticalAlignment.Center,
                        };
                    }
                    else if (double.TryParse(e.Value.ToString(), out var d))
                    {
                        e.Value = d;

                        e.Style = new SpreadStreamCellStyle()
                        {
                            HorizontalAlignment = SpreadHorizontalAlignment.Center,
                            VerticalAlignment = SpreadVerticalAlignment.Center,
                        };
                    }
                    else
                    {
                        e.Style = new SpreadStreamCellStyle()
                        {
                            HorizontalAlignment = SpreadHorizontalAlignment.Center,
                            VerticalAlignment = SpreadVerticalAlignment.Center,
                        };
                    }
                }
                else
                {
                    e.Style = new SpreadStreamCellStyle()
                    {
                        HorizontalAlignment = SpreadHorizontalAlignment.Center,
                        VerticalAlignment = SpreadVerticalAlignment.Center,
                    };
                }
            };

            _spreadStreamExport.AsyncExportProgressChanged += SpreadStreamExport_AsyncExportProgressChanged!;
            _spreadStreamExport.AsyncExportCompleted += SpreadStreamExport_AsyncExportCompleted!;
            _spreadStreamExport.RunExportAsync(fileFullName,
                                              new SpreadStreamExportRenderer(),
                                              new GridViewSpreadStreamExportOptions
                                              {
                                                  ShowColumnHeaders = true,
                                                  ColumnWidth = 180,
                                              });
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(ExcelExportAsync), ex.Message);
            var toast = (ex.Message, false);
            WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
        }
    }

    //출력완료
    private void SpreadStreamExport_AsyncExportCompleted(object sender, AsyncCompletedEventArgs e)
    {
        _exportRadGridView.BusyIndicatorProgressValue = 0;

        var toast = (GB.ExportMessage, true);
        WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

        GB.IsBusy = false;
        GB.BusyIndicatorVisibility = Visibility.Visible;
    }

    //출력진행률
    private void SpreadStreamExport_AsyncExportProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        _exportRadGridView.BusyIndicatorProgressValue = e.ProgressPercentage;

        GB.IsBusy = false;
        GB.BusyIndicatorVisibility = Visibility.Visible;
    }

    #endregion
}
