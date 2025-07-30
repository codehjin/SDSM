using CommunityToolkit.Mvvm.Messaging;
using SDSM_App.Messengers;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Windows;
using Telerik.Documents.SpreadsheetStreaming;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView.SpreadsheetStreamingExport;
using SDSM_App;

namespace FMRRC_App.Helpers;

public static class DataExportHelper
{
    // 리소스 엑셀 데이터를 내보내는 메서드
    public static void ExportResourceData(string tag, Window owner = null!)
    {
        try
        {
            CommonOpenFileDialog dialog = new()
            {
                IsFolderPicker = true,
                Title = "다운로드 - 폴더선택"
            };

            if (dialog.ShowDialog(owner) == CommonFileDialogResult.Ok)
            {
                if (string.IsNullOrWhiteSpace(dialog.FileName))
                    return;

                var sri = Application.GetResourceStream(new Uri($"pack://application:,,,/FMRRC_App;component/Assets/Excels/{tag}_표준양식.xlsx")).Stream;

                if (sri != null)
                {
                    string path = Path.Combine(dialog.FileName, $"{tag}_표준양식_{DateTime.Now:yyyyMMddHHmmss}.xlsx");

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
            GB.LogWrite(nameof(DataExportHelper), nameof(ExportResourceData), ex.Message);

            var toast = (ex.Message, false);
            WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
        }
    }

    // 데이터 그리드 자료 내보내기 메서드
    public static void ExportGridViewToExcel(
        RadGridView gridView, 
        string caption, 
        Action<int>? progressCallback = null, 
        Action? completedCallback = null, 
        bool showColumnGroups = false, 
        bool showColumnFooters = false
        )
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

            var spreadStreamExport = new GridViewSpreadStreamExport(gridView)
            {
                ExportFormat = SpreadStreamExportFormat.Xlsx,
                ShowLoadingIndicatorWhileAsyncExport = true,
                SheetName = caption,
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

            spreadStreamExport.AsyncExportProgressChanged += (s, e) =>
            {
                progressCallback?.Invoke(e.ProgressPercentage);
            };

            spreadStreamExport.AsyncExportCompleted += (s, e) =>
            {
                completedCallback?.Invoke();
                
                var toast = (GB.ExportMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

                GB.IsBusy = false;
                GB.BusyIndicatorVisibility = Visibility.Visible;
            };

            spreadStreamExport.RunExportAsync(fileFullName,
                                              new SpreadStreamExportRenderer(),
                                              new GridViewSpreadStreamExportOptions
                                              {
                                                  ShowColumnHeaders = true,
                                                  ColumnWidth = 180,
                                                  ShowColumnGroups = showColumnGroups,
                                                  ShowColumnFooters = showColumnFooters,
                                              });
        }
        catch (Exception ex)
        {
            GB.LogWrite(nameof(DataExportHelper), nameof(ExportGridViewToExcel), ex.Message);
            var toast = (ex.Message, false);
            WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
        }
    }
}
