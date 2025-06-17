using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;

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
    [ObservableProperty] private bool _isCheckedVeSurvey;
    [ObservableProperty] private bool _isCheckedPlantSurvey;
    [ObservableProperty] private bool _isCheckedARSurvey;
    [ObservableProperty] private bool _isCheckedSurveyFish;
    [ObservableProperty] private bool _isCheckedBirdSurvey;
    [ObservableProperty] private bool _isCheckedMammalSurvey;

    public static bool FloatingWindowOpend { get; set; }

    #endregion

    #region ICommand 구현

    //새창 열기
    [RelayCommand]
    private void FloatingWindowOpenExecuted(string paneName)
    {
        //if (FloatingWindowOpend == false)
        //{
        //    Window win = App.Current.Services.GetRequiredService<FloatingWindow>();
        //    win.Owner = Application.Current.MainWindow;
        //    win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        //    win.Closed += (s, e) =>
        //    {
        //        FloatingWindowOpend = false;
        //    };
        //    FloatingWindowOpend = true;
        //    win.Show();
        //}
        //else
        //{
        //    var window = Application.Current.Windows.OfType<FloatingWindow>().FirstOrDefault();
        //    if (window != null)
        //    {
        //        if (window.WindowState == WindowState.Minimized)
        //        {
        //            window.WindowState = WindowState.Normal;
        //        }
        //        window.Topmost = true;
        //        window.Topmost = false;
        //    }
        //}

        WeakReferenceMessenger.Default.Send(new FloatingTargetMessage(paneName));
    }

    #endregion

    #region 메서드

   

    #endregion

    #region Messages

    #endregion
}
