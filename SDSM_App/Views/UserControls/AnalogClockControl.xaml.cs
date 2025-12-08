using SDSM_App.InjectableServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// 아날로그 시계
/// </summary>
public partial class AnalogClockControl : UserControl, ITransientService
{
    private readonly object _lock = new();
    private System.Timers.Timer? _aTimer;

    public AnalogClockControl()
    {
        InitializeComponent();

        _aTimer = new System.Timers.Timer(new TimeSpan(0, 0, 1));
        //이벤트 핸들러 연결
        _aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

        //타이머에서 Elapsed 이벤트를 반복실행
        _aTimer.AutoReset = true;
        _aTimer.Enabled = true;

        this.Unloaded += Control_Unloaded;
    }

    public double Hours
    {
        get { return (double)GetValue(HoursProperty); }
        set { SetValue(HoursProperty, value); }
    }
    public static readonly DependencyProperty HoursProperty = DependencyProperty.Register("Hours", typeof(double), typeof(AnalogClockControl));

    public double Minutes
    {
        get { return (double)GetValue(MinutesProperty); }
        set { SetValue(MinutesProperty, value); }
    }
    public static readonly DependencyProperty MinutesProperty = DependencyProperty.Register("Minutes", typeof(double), typeof(AnalogClockControl));

    public double Seconds
    {
        get { return (double)GetValue(SecondsProperty); }
        set { SetValue(SecondsProperty, value); }
    }
    public static readonly DependencyProperty SecondsProperty = DependencyProperty.Register("Seconds", typeof(double), typeof(AnalogClockControl));

    public string TT
    {
        get { return (string)GetValue(TTProperty); }
        set { SetValue(TTProperty, value); }
    }
    public static readonly DependencyProperty TTProperty = DependencyProperty.Register("TT", typeof(string), typeof(AnalogClockControl));

    public Brush HoursForeground
    {
        get { return (Brush)GetValue(HoursForegroundProperty); }
        set { SetValue(HoursForegroundProperty, value); }
    }
    public static readonly DependencyProperty HoursForegroundProperty = DependencyProperty.Register("HoursForeground", typeof(Brush), typeof(AnalogClockControl), 
        new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAffffff"))));

    private void Control_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_aTimer != null)
        {
            _aTimer.Stop();
            _aTimer.Elapsed -= OnTimedEvent;
            _aTimer.Dispose();
            _aTimer = null;
        }
    }

    private void OnTimedEvent(object? sender, ElapsedEventArgs e)
    {
        lock (_lock)
        {
            App.Current.Dispatcher.InvokeAsync(() =>
            {
                
                DateTime now = DateTime.Now;
                double seconds = now.Second;
                double minutes = now.Minute + (seconds / 60.0);
                double hours = now.Hour + (minutes / 60.0);

                Seconds = seconds;
                Minutes = minutes;
                Hours = hours;
                TT = now.ToString("tt");
            });
        }
    }
}
