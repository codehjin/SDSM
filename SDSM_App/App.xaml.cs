using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using RestoreWindowPlace;
using SDSM_App.Commons;
using SDSM_App.InjectableServices;
using SDSM_App.Views.UserControls;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Telerik.Windows.Automation.Peers;
using Telerik.Windows.Controls;

namespace SDSM_App;

/// <summary>
/// 삼성DS 모니터링 플랫폼
/// </summary>
public partial class App : Application
{
    public WindowPlace WindowPlace { get; }
    private Mutex? mutex;

    public App()
    {
        StyleManager.ApplicationTheme = new Windows11Theme();
        LocalizationManager.Manager = new CustomLocalizationManaber();
        AutomationManager.AutomationMode = AutomationMode.Disabled;

        Services = ConfigureServices();

        InitializeComponent();

        Windows11Palette.Palette.FontFamily = (FontFamily)FindResource("DefaultFontFamily");
        Windows11Palette.Palette.FontSize = (double)TryFindResource("DefaultFontSize");
        Windows11Palette.Palette.FocusVisualMargin = new Thickness(0);
        Windows11Palette.Palette.PrimaryBorderColor = (Color)ColorConverter.ConvertFromString("#20000000");
        Windows11Palette.Palette.SecondaryForegroundColor = (Color)ColorConverter.ConvertFromString("#BB000000");
        Windows11Palette.Palette.PrimarySolidBorderColor = (Color)ColorConverter.ConvertFromString("#FFE5E5E5");
        Windows11Palette.Palette.SelectedColor = (Color)ColorConverter.ConvertFromString("#FFE5E5E5");

        //창 상태 생성
        WindowPlace = new WindowPlace("RestoreWindowPlace.config");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        //어셈블리 버전
        Version? appVersion = Assembly.GetExecutingAssembly().GetName().Version;
        if (appVersion != null)
        {
            GB.Version = $"{appVersion.Major}.{appVersion.Minor}";
        }

        //로딩화면
        RadSplashScreenManager.Show<SplashScreenControl>();

        base.OnStartup(e);

        //싱글 윈도우
        string mutexName = "SDSM_App";
        mutex = new Mutex(true, mutexName, out bool bCreateNew);
        if (!bCreateNew) { Shutdown(); }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        //창 상태 저장
        WindowPlace?.Save();
    }

    #region DependencyInjection

    public new static App Current => (App)Application.Current;

    public System.IServiceProvider Services { get; }

    private static System.IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.Scan(scan => scan
                .FromAssemblyOf<IInjectablesService>()
                .AddClasses(classes => classes.AssignableTo<ITransientService>())
                .AsSelfWithInterfaces()
                .WithTransientLifetime()
                .AddClasses(classes => classes.AssignableTo<IScopedService>())
                .AsSelfWithInterfaces()
                .WithScopedLifetime()
                .AddClasses(classes => classes.AssignableTo<ISingletonService>())
                .AsSelfWithInterfaces()
                .WithSingletonLifetime()
        );

        return services.BuildServiceProvider();
    }

    #endregion

    #region Telerik Theme

    public static void SetLightModeThemes()
    {
        Windows11Palette.Palette.PrimaryBorderColor = (Color)ColorConverter.ConvertFromString("#28000000");
        Windows11Palette.Palette.SecondaryForegroundColor = (Color)ColorConverter.ConvertFromString("#BB000000");
        Windows11Palette.Palette.PrimarySolidBorderColor = (Color)ColorConverter.ConvertFromString("#FFE5E5E5");
        Windows11Palette.Palette.SelectedColor = (Color)ColorConverter.ConvertFromString("#FFE5E5E5");
    }

    public static void SetDarkModeThemes()
    {
        //Windows11Palette.Palette.ValidationColor = (Color)ColorConverter.ConvertFromString("#FFFF5163");
        Windows11Palette.Palette.AccentColor = (Color)ColorConverter.ConvertFromString("#FF00AFFF");
        Windows11Palette.Palette.AccentMouseOverColor = (Color)ColorConverter.ConvertFromString("#E600AFFF");
        Windows11Palette.Palette.AccentPressedColor = (Color)ColorConverter.ConvertFromString("#CC00AFFF");
        Windows11Palette.Palette.MouseOverBackgroundColor = (Color)ColorConverter.ConvertFromString("#25FFFFFF");
        Windows11Palette.Palette.PrimarySolidBorderColor = (Color)ColorConverter.ConvertFromString("#FF353535");
        Windows11Palette.Palette.SelectedColor = (Color)ColorConverter.ConvertFromString("#FF353535");
    }

    #endregion
}
