using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using SDSM_App.Views.Windows;
using SDSM_Models;
using System.Windows;

namespace SDSM_App.ViewModels;

public partial class MainWindowViewModel : ObservableObject, ISingletonService
{
    #region 필드

    #endregion

    #region 생성자

    public MainWindowViewModel()
    {
        SetProjectSiteNames();
    }

    #endregion

    #region 속성

    //주메뉴
    [ObservableProperty] private bool _isCheckedHome;

    //통계
    [ObservableProperty] private bool _isCheckedStats;

    //생물조사
    [ObservableProperty] private bool _isCheckedFish;
    [ObservableProperty] private bool _isCheckedBenthos;
    [ObservableProperty] private bool _isCheckedBird;
    [ObservableProperty] private bool _isCheckedMammal;
    [ObservableProperty] private bool _isCheckedVegetation;
    [ObservableProperty] private bool _isCheckedPlant;
    [ObservableProperty] private bool _isCheckedAmphibianReptile;
    
    //환경자료
    [ObservableProperty] private bool _isCheckedHabitatWaterEdge;
    [ObservableProperty] private bool _isCheckedWaterQuality;

    //일괄등록
    [ObservableProperty] private bool _IsCheckedBulkRegister;

    //설정
    [ObservableProperty] private bool _isCheckedSetting;

    #endregion

    #region ICommand 구현

    //메뉴얼
    [RelayCommand]
    public void ManualWindowOpen()
    {
        var window = App.Current.Windows.OfType<ManualWindow>().FirstOrDefault();
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
            Window win = App.Current.Services.GetRequiredService<ManualWindow>();
            win.Show();
        }
    }

    #endregion

    #region 메서드

    private void SetProjectSiteNames()
    {
        using var context = new SDSMDbContext();

        //전역변수에 저장
        GB.ProjectSiteNames = [.. context.SiteDivisions];
    }

    #endregion

    #region Messages

    #endregion
}
