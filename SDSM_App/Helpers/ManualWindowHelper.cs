using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SDSM_App.Views.Windows;

namespace SDSM_App.Helpers;

/// <summary>
/// PDF 보기 창(ManualWindow) 열기 — 사용 설명서 · 지침 원본이 같은 창을 쓴다(2026-10-22 §21-5).
/// 같은 PDF 창이 이미 열려 있으면 새로 띄우지 않고 앞으로 가져온다.
/// </summary>
public static class ManualWindowHelper
{
    public static void Open(string pdfUri, string caption)
    {
        var window = App.Current.Windows.OfType<ManualWindow>().FirstOrDefault(w => w.PdfUri == pdfUri);
        if (window != null)
        {
            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }
            window.Topmost = true;
            window.Topmost = false;
        }
        else
        {
            var win = App.Current.Services.GetRequiredService<ManualWindow>();
            win.PdfUri = pdfUri;
            win.Caption = caption;
            win.Show();
        }
    }
}
