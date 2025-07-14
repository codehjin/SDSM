using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Documents.SpreadsheetStreaming;
using Telerik.Windows.Controls.GridView.SpreadsheetStreamingExport;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// 저서동물 출현종
/// </summary>
public partial class BenthosSpeciesControl : UserControl, ITransientService
{
    private readonly BenthosSpeciesControlViewModel _vm;

    public BenthosSpeciesControl(BenthosSpeciesControlViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        DataContext = _vm;
      
        SizeChanged += UserControl_SizeChanged;
    }

    #region This 이벤트

    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var height = e.NewSize.Height - (SearchLayout.ActualHeight + 130);

        if (height > 60)
        {
            SpeciesDataRadGridView.Height = height;
        }
        else
        {
            SpeciesDataRadGridView.Height = 60;
        }
    }

    #endregion

    #region Control 이벤트    

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


    //초기화
    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.SearchText = string.Empty;
        SearchQuery();
    }

    //검색
    private void SearchBox_QuerySubmitted(object sender, Telerik.Windows.Controls.AutoSuggestBox.QuerySubmittedEventArgs e)
    {
        SearchQuery();
    }

    //검색
    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SearchQuery();
        }
    }

    #endregion

    #region 메서드

    //검색
    private void SearchQuery()
    {
        SpeciesDataRadGridView.FilterDescriptors.SuspendNotifications();
        foreach (Telerik.Windows.Controls.GridViewColumn column in SpeciesDataRadGridView.Columns)
        {
            column.ClearFilters();
        }
        SpeciesDataRadGridView.FilterDescriptors.ResumeNotifications();

        SpeciesDataRadGridView.SortDescriptors.Clear();

        _vm.SearchExecutedCommand.Execute(null);
    }

    #endregion

    #region 출력메서드

    //자료 출력
    private void DataExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (SpeciesDataRadGridView.Items.Count == 0) return;

        ExcelExportAsync();
    }

    private void ExcelExportAsync()
    {
        try
        {
            string fileName = $"저서동물_출현종_{DateTime.Now:yyyyMMddHHmmss}";
            string downloadPath = string.Empty;
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

            var spreadStreamExport = new GridViewSpreadStreamExport(SpeciesDataRadGridView)
            {
                ExportFormat = SpreadStreamExportFormat.Xlsx,
                ShowLoadingIndicatorWhileAsyncExport = true,
                SheetName = "저서동물_출현종",
            };

            spreadStreamExport.ElementExportingToDocument += (s, e) =>
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

            spreadStreamExport.AsyncExportProgressChanged += SpreadStreamExport_AsyncExportProgressChanged!;
            spreadStreamExport.AsyncExportCompleted += SpreadStreamExport_AsyncExportCompleted!;
            spreadStreamExport.RunExportAsync(fileFullName,
                                              new SpreadStreamExportRenderer(),
                                              new GridViewSpreadStreamExportOptions
                                              {
                                                  ShowColumnHeaders = true,
                                                  ColumnWidth = 180,
                                                  ShowColumnGroups = true,
                                              });
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(ExcelExportAsync), ex.Message);
            var toast = (ex.Message, false);
            WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

            GB.IsBusy = false;
            GB.BusyIndicatorVisibility = Visibility.Visible;
        }
    }

    //출력완료
    private void SpreadStreamExport_AsyncExportCompleted(object sender, AsyncCompletedEventArgs e)
    {
        var toast = (GB.ExportMessage, true);
        WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

        SpeciesDataRadGridView.BusyIndicatorProgressValue = 0;

        GB.IsBusy = false;
        GB.BusyIndicatorVisibility = Visibility.Visible;
    }

    //출력진행률
    private void SpreadStreamExport_AsyncExportProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        SpeciesDataRadGridView.BusyIndicatorProgressValue = e.ProgressPercentage;
    }

    #endregion
}
