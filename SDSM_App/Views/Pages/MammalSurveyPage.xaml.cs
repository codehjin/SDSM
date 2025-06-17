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
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;
using Telerik.Windows.Controls.GridView.SearchPanel;
using Telerik.Windows.Controls.GridView.SpreadsheetStreamingExport;

namespace SDSM_App.Views.Pages;

/// <summary>
/// 생물조사 포유류
/// </summary>
public partial class MammalSurveyPage : Page, ITransientService
{
    private readonly RoutedUICommand _searchByTextCommand = (RoutedUICommand)RadGridViewCommands.SearchByText;
    private readonly RoutedUICommand _clearSerachValueCommand = (RoutedUICommand)GridViewSearchPanelCommands.ClearSearchValue;

    private readonly MammalSurveyPageViewModel _vm;

    public MammalSurveyPage(MammalSurveyPageViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        DataContext = _vm;

        SizeChanged += Page_SizeChanged;
    }

    #region This 이벤트

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var height = e.NewSize.Height - (SerachLayout.ActualHeight + 120);

        if (height > 60)
        {
            DataRadGridView.Height = height;
        }
        else
        {
            DataRadGridView.Height = 60;
        }
    }

    #endregion

    #region Control 이벤트    

    //새로고침
    private void ReloadRadButton_Click(object sender, RoutedEventArgs e)
    {
        DataRadGridView.FilterDescriptors.SuspendNotifications();
        foreach (Telerik.Windows.Controls.GridViewColumn column in DataRadGridView.Columns)
        {
            column.ClearFilters();
        }
        DataRadGridView.FilterDescriptors.ResumeNotifications();

        DataRadGridView.SortDescriptors.Clear();

        SearchRadWatermarkTextBox.Text = "";
        _clearSerachValueCommand.Execute(null, DataRadGridView.ChildrenOfType<GridViewSearchPanel>().FirstOrDefault());
    }

    #endregion

    #region GridView SearchText

    private void SearchRadWatermarkTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(SearchRadWatermarkTextBox.Text))
        {
            _clearSerachValueCommand.Execute(null, DataRadGridView.ChildrenOfType<GridViewSearchPanel>().FirstOrDefault());
        }
    }

    private void SearchRadWatermarkTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (!string.IsNullOrEmpty(SearchRadWatermarkTextBox.Text))
            {
                _searchByTextCommand.Execute(SearchRadWatermarkTextBox.Text, DataRadGridView);
            }
            else
            {
                _clearSerachValueCommand.Execute(null, DataRadGridView.ChildrenOfType<GridViewSearchPanel>().FirstOrDefault());
            }
        }
    }

    private void SearchRadButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(SearchRadWatermarkTextBox.Text))
        {
            _searchByTextCommand.Execute(SearchRadWatermarkTextBox.Text, DataRadGridView);
        }
        else
        {
            _clearSerachValueCommand.Execute(null, DataRadGridView.ChildrenOfType<GridViewSearchPanel>().FirstOrDefault());
        }
    }

    #endregion

    #region 출력메서드

    //자료 출력
    private void DataExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataRadGridView.Items.Count == 0) return;

        ExcelExportAsync();
    }

    private void ExcelExportAsync()
    {
        try
        {
            string fileName = $"포유류_{DateTime.Now:yyyyMMddHHmmss}";
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

            var spreadStreamExport = new GridViewSpreadStreamExport(DataRadGridView)
            {
                ExportFormat = SpreadStreamExportFormat.Xlsx,
                ShowLoadingIndicatorWhileAsyncExport = true,
                SheetName = "포유류",
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

        DataRadGridView.BusyIndicatorProgressValue = 0;

        GB.IsBusy = false;
        GB.BusyIndicatorVisibility = Visibility.Visible;
    }

    //출력진행률
    private void SpreadStreamExport_AsyncExportProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        DataRadGridView.BusyIndicatorProgressValue = e.ProgressPercentage;
    }

    #endregion
}
