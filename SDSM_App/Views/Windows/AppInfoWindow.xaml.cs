using System.Windows;
using SDSM_App.InjectableServices;
using SDSM_App.ViewModels;

namespace SDSM_App.Views.Windows;

/// <summary>
/// 프로그램 정보 — 이름 · 판 · 빌드 날짜 · 적용 지침([지침 원본 보기]) · 종목록 기준 · 개발사(2026-10-22 §21-5).
/// </summary>
public partial class AppInfoWindow : Window, ITransientService
{
    public AppInfoWindow(AppInfoWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
