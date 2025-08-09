using Microsoft.Extensions.DependencyInjection;
using SDSM_App.InjectableServices;
using SDSM_App.ViewModels;
using SDSM_App.Views.UserControls;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;

namespace SDSM_App;

/// <summary>
/// 메인
/// </summary>
public partial class MainWindow : Window, ISingletonService
{
    private readonly MainWindowViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();

        _vm = App.Current.Services.GetRequiredService<MainWindowViewModel>();
        DataContext = _vm;

        //창 복원
        ((App)Application.Current).WindowPlace.Register(this);

        Loaded += Window_Loaded;
        StateChanged += Window_StateChanged;

        ContentFrame.NavigationService.Navigated += NavigationService_Navigated;
    }

    #region window

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        //첫 로딩화면
        if (RadSplashScreenManager.IsSplashScreenActive)
        {
            RadSplashScreenManager.Close();
        }

        Activate();
        Topmost = true;
        Topmost = false;

        //홈 페이지 로드
        MenuItem_Click(FirstButton, null!);
        _vm.IsCheckedHome = true;

        //LoginControlLoad();
    }

    //minimized button
    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    //maximized button
    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        if (this.WindowState != WindowState.Normal)
        {
            this.WindowState = WindowState.Normal;
        }
        else
        {
            this.WindowState = WindowState.Maximized;
        }
    }

    //close button
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Properties.Settings.Default.PageSize = GB.CurrentPageSize;
        Properties.Settings.Default.Save();
    }

    //WindowStyle None때문
    private void Window_StateChanged(object? sender, EventArgs e)
    {
        HandleWinMaximized();
    }

    private void HandleWinMaximized()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
        }
    }

    #endregion

    #region Control 이벤트

    //Frame 되돌리기 방지 
    private void NavigationService_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        ContentFrame.NavigationService.RemoveBackEntry();
    }

    //화면잠금 패스워드
    private void ScreenLockRadPasswordBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (ScreenLockRadPasswordBox.Password == GB.UserPassword)
        {
            ScreenLockLayout.Visibility = Visibility.Collapsed;
            ScreenLockRadPasswordBox.Password = string.Empty;

            //아날로그 시계
            AnalogClockContentControl.Content = null;
        }
    }

    //화면 잠금
    private void ScreenLockRadButton_Click(object sender, RoutedEventArgs e)
    {
        ScreenLockRadPasswordBox.Password = string.Empty;
        ScreenLockLayout.Visibility = Visibility.Visible;

        UserDropDownButton.IsOpen = false;

        //아날로그 시계
        AnalogClockContentControl.Content = App.Current.Services.GetRequiredService<AnalogClockControl>();
    }

    //로그아웃
    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        LoginControlLoad();
    }

    //사용자 정보
    private void UserDropDownButton_DropDownOpened(object sender, RoutedEventArgs e)
    {
        var control = App.Current.Services.GetRequiredService<PasswordChangeControl>();
        if (control != null)
        {
            PasswordChangeContentControl.Content = control;
        }
    }

    //사용자 정보
    private void UserDropDownButton_DropDownClosed(object sender, RoutedEventArgs e)
    {
        PasswordChangeContentControl.Content = null;
    }

    #endregion

    #region 메뉴아이템

    //메뉴아이템
    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadRadioButton button)
        {
            if (button != null && button.Tag is string tag)
            {
                if (!tag.Contains("Window", StringComparison.OrdinalIgnoreCase))
                {
                    GoNavigate(tag);
                }
            }
        }
    }

    //페이지 이동
    private void GoNavigate(string name)
    {
        Type? page = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).FirstOrDefault(x => x.Name == name);
        if (page != null)
        {
            ContentFrame.Navigate(App.Current.Services.GetService(page));
        }
        else
        {
            MessageBox.Show("페이지를 찾을수 없습니다.", "페이지 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            GB.LogWrite(GetType().Name, nameof(GoNavigate), "페이지 오류");
        }
    }

    #endregion

    #region 메서드

    //로그인창 보기
    private void LoginControlLoad()
    {
        var loginControl = App.Current.Services.GetRequiredService<LoginControl>();
        if (loginControl != null)
        {
            loginControl.ElementClick += (s, e) =>
            {
                if (e.Source is LoginControl loginControl)
                {
                    LoginLayout.Visibility = Visibility.Collapsed;
                    UserDropDownButton.Visibility = Visibility.Visible;
                }
            };
            LoginContentControl.Content = loginControl;
            LoginLayout.Visibility = Visibility.Visible;
            UserDropDownButton.Visibility = Visibility.Collapsed;

            UserDropDownButton.IsOpen = false;
        }
    }

    #endregion
}