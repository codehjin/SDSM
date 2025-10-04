using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SDSM_App.InjectableServices;
using SDSM_App.Views.Windows;
using System.Windows;
using System.Windows.Forms;

namespace SDSM_App.ViewModels;

public partial class MainWindowViewModel : ObservableObject, ISingletonService
{
    #region 필드

    #endregion

    #region 생성자

    public MainWindowViewModel()
    {
        
    }

    #endregion

    #region 속성

    //주메뉴
    [ObservableProperty] private bool _isCheckedHome;

    //통계
    [ObservableProperty] private bool _isCheckedStatsFish;
    [ObservableProperty] private bool _isCheckedStatsBenthos;
    [ObservableProperty] private bool _isCheckedStatsBird;
    [ObservableProperty] private bool _isCheckedStatsMammal;
    [ObservableProperty] private bool _isCheckedStatsVegetation;
    [ObservableProperty] private bool _isCheckedStatsPlant;
    [ObservableProperty] private bool _isCheckedStatsAmphibianReptile;
    [ObservableProperty] private bool _isCheckedStatsHabitatWaterEdge;
    [ObservableProperty] private bool _isCheckedStatsWaterQuality;

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


    #endregion

    #region Messages

    #endregion
}
