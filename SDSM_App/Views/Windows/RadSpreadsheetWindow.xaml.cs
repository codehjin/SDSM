using CommunityToolkit.Mvvm.Messaging;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_Models;
using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Spreadsheet.Worksheets;
using Telerik.Windows.Documents.Spreadsheet.Model;

namespace SDSM_App.Views.Windows;

/// <summary>
/// 일괄등록 엑셀시트
/// </summary>
public partial class RadSpreadsheetWindow : Window, ITransientService
{
    public RadSpreadsheetWindow()
    {
        InitializeComponent();

        Loaded += Window_Loaded;
    }

    #region window

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Activate();
        Topmost = true;
        Topmost = false;
    }

    //minimized button
    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    //maximized button
    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState != WindowState.Normal)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = WindowState.Maximized;
        }
    }

    //close button
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    //WindowStyle None때문
    private void Window_StateChanged(object? sender, EventArgs e)
    {
        HandleWinMaximized();
        CalloutPopupService.CloseAll();
    }
    private void HandleWinMaximized()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowStyle = WindowStyle.None;
        }
    }

    #endregion

    #region Control 이벤트

    // 1행1열에 붙여넣기
    private void PasteRadButton_Click(object sender, RoutedEventArgs e)
    {
        AllCellClear();

        Selection selection = RadSpreadsheet.ActiveWorksheetEditor.Selection;
        selection.Select(new CellIndex(0, 0));

        RadSpreadsheet.ActiveWorksheetEditor.Paste();
    }

    // 시트 값 지우기
    private void ClearRadButton_Click(object sender, RoutedEventArgs e)
    {
        AllCellClear();
    }

    // 선택셀에 붙여넣기
    private void RadMenuItem_Click(object sender, Telerik.Windows.RadRoutedEventArgs e)
    {
        RadSpreadsheet.ActiveWorksheetEditor.Paste();
    }
    
    // 자료 보내기
    private void SendRadButton_Click(object sender, RoutedEventArgs e)
    {
        // 현재 시트의 데이터 전송
        WeakReferenceMessenger.Default.Send(new WorksheetMessage(RadSpreadsheet.ActiveWorksheet));
    }

    #endregion

    #region 메서드

    private void AllCellClear()
    {
        CellRange usedCellRange = RadSpreadsheet.ActiveWorksheet.UsedCellRange;
        CellSelection selection = RadSpreadsheet.ActiveWorksheet.Cells[usedCellRange];
        selection.Clear(ClearType.All);
    }

    #endregion
}
