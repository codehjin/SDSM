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
/// 통계수질
/// </summary>
public partial class StatsWaterQualityControl : UserControl, ITransientService
{
    private readonly StatsWaterQualityControlViewModel _vm;
    private readonly string _caption = "수질";

    public StatsWaterQualityControl(StatsWaterQualityControlViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        DataContext = _vm;
    }

    #region Control 이벤트

    private void GradeDataRadGridView_Loaded(object sender, RoutedEventArgs e)
    {
        var dt = _vm.GradeCollection;
        if (dt == null || dt.Rows.Count == 0)
            return;

        var rivers = new List<string>();

        foreach (DataColumn column in dt.Columns)
        {
            var header = column.ColumnName ?? "";
            if (header.Contains('_'))
            {
                // "_" 다음 문자열만 남김
                rivers.Add(header[..(header.IndexOf('_'))]);
            }
        }

        foreach (var river in rivers.Distinct())
        {
            GradeDataRadGridView.ColumnGroups.Add(new GridViewColumnGroup { Name = river, Header = river });
        }
    }

    private void RadGridView_ElementExportingToDocument(object sender, GridViewElementExportingToDocumentEventArgs e)
    {
        if (e.Element == ExportElement.Cell && e.Value != null)
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

    private void GradeDataRadGridView_AutoGeneratingColumn(object sender, GridViewAutoGeneratingColumnEventArgs e)
    {
        var column = (GridViewDataColumn)e.Column;

        if (column != null)
        {
            switch (column.Header.ToString())
            {
                case "연도":
                    column.MinWidth = 120;
                    column.Width = 120;
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
                    column.MinWidth = 52;
                    column.Width = 52;
                    column.MaxWidth = 52;
                    column.DataType = typeof(string);
                    break;
            }
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
                { "연도별자료", YearDataRadGridView },
                { "수질등급", GradeDataRadGridView },
            };

            MultiExport(folder, date, gridViews);
        }
    }

    //개별출력
    private void EachExportRadButton_Click(object sender, RoutedEventArgs e)
    {
        var chartItems = _vm.ExportCollection.Where(m => m.IsCheckedChart).ToList();
        var tableItems = _vm.ExportCollection.Where(m => m.IsCheckedTable).ToList();

        if (chartItems.Count == 0 && tableItems.Count == 0)
            return;

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

            if (chartItems.Count != 0)
            {
                foreach (var item in chartItems)
                {
                    switch (item.Caption)
                    {
                        case "연도별자료":
                            {
                                ChartToPng(YearChart, Path.Combine(folder, $"{_caption}_{item.Caption}_{date}.png"));
                            }
                            break;
                        case "수질등급":
                            {
                                //그래프 없음
                            }
                            break;
                    }
                }
            }

            if (tableItems.Count != 0)
            {
                var gridViews = new Dictionary<string, RadGridView>();

                foreach (var item in _vm.ExportCollection.Where(m => m.IsCheckedTable))
                {
                    switch (item.Caption)
                    {
                        case "연도별자료":
                            gridViews.Add(item.Caption, YearDataRadGridView);
                            break;
                        case "수질등급":
                            gridViews.Add(item.Caption, GradeDataRadGridView);
                            break;
                    }
                }
                MultiExport(folder, date, gridViews);
            }

            if (chartItems.Count != 0 && tableItems.Count == 0)
            {
                var toast = (GB.ExportMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
            }
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

                    if (gridView.Key == "수질등급")
                    {
                        columnSelection.SetWidth(new ColumnWidth(columnWidth + 40, true));

                        var columnCount = ws.UsedCellRange.ColumnCount;
                        for (int i = 1; i < columnCount; i += 8)
                        {
                            ws.Cells[new CellIndex(0, i), new CellIndex(0, i + 7)].Merge();
                        }
                    }
                    else
                    {
                        columnSelection.SetWidth(new ColumnWidth(columnWidth + 160, true));
                    }
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
