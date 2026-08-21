using System.Windows;
using SDSM_App.InjectableServices;
using SDSM_App.ViewModels;

namespace SDSM_App.Views.Windows;

/// <summary>
/// 조사지점 관리 (ADMIN_TASKS §2-4).
/// 지점 마스터(`sites.json`) 등록·수정·삭제·병합과 조사자 배포용 내보내기를 담당한다.
/// ⚠ 이 화면은 DB에 쓰지 않는다.
/// </summary>
public partial class SiteMasterWindow : Window, ITransientService
{
    public SiteMasterWindow(SiteMasterWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
