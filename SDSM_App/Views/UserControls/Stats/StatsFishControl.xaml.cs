using CommunityToolkit.Mvvm.Messaging;
using Microsoft.WindowsAPICodePack.Dialogs;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.ViewModels;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Telerik.Windows.Controls;
using Telerik.Windows.Documents.Spreadsheet.FormatProviders.OpenXml.Xlsx;
using Telerik.Windows.Documents.Spreadsheet.Model;
using Telerik.Windows.Documents.Spreadsheet.PropertySystem;
using Telerik.Windows.Documents.Spreadsheet.Utilities;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// 통계어류
/// </summary>
public partial class StatsFishControl : UserControl, ITransientService
{
    private readonly StatsFishControlViewModel _vm;
    private readonly string _caption = "어류";
    private readonly Style? _cellLeftStyle = Application.Current.FindResource("GridViewCellLeftStyle") as Style;

    public StatsFishControl(StatsFishControlViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        DataContext = _vm;

        //컬럼 동적 생성용
        _vm.ProjectRadGridView = ProjectRadGridView;
    }

    #region Control 이벤트

    private void RadGridView_ElementExportingToDocument(object sender, GridViewElementExportingToDocumentEventArgs e)
    {
        if ((e.Element == ExportElement.Cell && e.Value != null) || (e.Element == ExportElement.FooterCell && e.Value != null))
        {
            if (int.TryParse(e.Value.ToString(), out var i))
            {
                e.Value = i;
            }
            else if (double.TryParse(e.Value.ToString(), out var d))
            {
                e.Value = d;
            }
        }
    }

    private void ProjectRadGridView_AutoGeneratingColumn(object sender, GridViewAutoGeneratingColumnEventArgs e)
    {
        var column = (GridViewDataColumn)e.Column;

        if (column != null)
        {
            switch (column.Header.ToString())
            {
                case "사업장":
                    column.MinWidth = 80;
                    column.MaxWidth = 100;
                    break;
                case "과업대표하천":
                    column.MinWidth = 80;
                    column.MaxWidth = 120;
                    break;
                default:
                    var header = column.Header.ToString() ?? "";
                    if (header.Contains('_'))
                    {
                        //컬럼 그룹
                        column.ColumnGroupName = header[..(header.IndexOf('_'))];

                        // 컬럼명
                        column.Header = header[(header.IndexOf('_') + 1)..];
                    }

                    if (column.Header.ToString() == "종수")
                    {
                        column.MinWidth = 80;
                        column.Width = GridViewLength.Auto;
                        column.MaxWidth = 80;
                        column.DataType = typeof(string);
                    }
                    else
                    {
                        column.MinWidth = 60;
                        column.Width = GridViewLength.Auto;
                        column.MaxWidth = 160;
                        column.TextTrimming = TextTrimming.CharacterEllipsis;
                        if (_cellLeftStyle != null)
                            column.CellStyle = _cellLeftStyle;
                        column.DataType = typeof(string);
                    }
                    break;
            }
        }
    }

    private void ViewerOpenRadButton_Click(object sender, RoutedEventArgs e)
    {
        if (App.Current.Services.GetService(typeof(ProjectSpeciesViewerControl)) is ProjectSpeciesViewerControl viewer)
        {
            viewer.CloseClick += (s, e) =>
            {
                ProjectSpeciesViewerContentControl.Content = null;
            };
            viewer.DataDataTable = _vm.ProjectDataTable.Copy();
            ProjectSpeciesViewerContentControl.Content = viewer;
        }
    }

    //전체출력
    private void AllExportRadButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
            Title = "폴더 선택",
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            var folder = dialog.FileName;

            if (string.IsNullOrWhiteSpace(folder))
                return;

            var date = DateTime.Now.ToString("yyyyMMddHHmmss");

            ChartToPng(YearChart, Path.Combine(folder, $"{_caption}_연도별자료_{date}.png"));

            var gridViews = new Dictionary<string, RadGridView>
            {
                { "연도별 자료", YearRadGridView },
                { "사업장 출현종", ProjectRadGridView },
                { "전체 출현종", ProjectSpeciesRadGridView },
            };

            MultiExport(folder, date, gridViews);
        }
    }

    #endregion

    #region 출력

    private async void MultiExport(string folder, string date, Dictionary<string, RadGridView> gridViews)
    {
        try
        {
            GB.IsBusy = true;
            GB.ProgressValue = 0;
            GB.IsIndeterminate = true;
            GB.BusyContent = "자료 저장 중...";

            var excelFileName = $"통계_{_caption}_{date}.xlsx";

            var formatProvider = new XlsxFormatProvider();
            Workbook workbook = new();

            CellStyle wbStyle = workbook.Styles["Normal"];
            wbStyle.FontSize = UnitHelper.PointToDip(12);
            wbStyle.HorizontalAlignment = RadHorizontalAlignment.Center;
            wbStyle.VerticalAlignment = RadVerticalAlignment.Center;

            var gridWorkbooks = new Dictionary<string, Workbook>();

            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var gridView in gridViews)
                {
                    Workbook wb = gridView.Value.ExportToWorkbook();
                    gridWorkbooks.Add(gridView.Key, wb);
                }
            });

            await Task.Run(() =>
            {
                foreach (var gridView in gridViews)
                {
                    var gridWorkbook = gridWorkbooks.Where(m => m.Key == gridView.Key).First().Value;
                    Worksheet clonedSheet = gridWorkbook.ActiveWorksheet;

                    Worksheet ws = workbook.Worksheets.Add();
                    ws.CopyFrom(clonedSheet);
                    ws.Name = gridView.Key.ToString();

                    RowSelection rowSelection = ws.Rows[0, ws.UsedCellRange.RowCount];
                    double rowsHeight = rowSelection.GetHeight().Value.Value;
                    rowSelection.SetHeight(new RowHeight(rowsHeight + 10, true));

                    ColumnSelection columnSelection = ws.Columns[0, ws.UsedCellRange.ColumnCount];
                    double columnWidth = columnSelection.GetWidth().Value.Value;
                    columnSelection.SetWidth(new ColumnWidth(columnWidth + 160, true));
                }
                using Stream output = new FileStream(Path.Combine(folder, $"{excelFileName}"), FileMode.Create);
                formatProvider.Export(workbook, output, null);
            });

            var toast = (GB.ExportMessage, true);
            WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(MultiExport), ex.Message);


        }
        finally
        {
            GB.IsBusy = false;
            GB.BusyContent = string.Empty;
        }
    }

    private void ChartToPng(FrameworkElement chart, string path)
    {
        try
        {
            if (chart == null || string.IsNullOrWhiteSpace(path))
                return;

            using Stream stream = new FileStream(path, FileMode.Create);
            Telerik.Windows.Media.Imaging.ExportExtensions.ExportToImage(chart, stream, new PngBitmapEncoder());
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(ChartToPng), ex.Message);
        }
    }

    #endregion
}
