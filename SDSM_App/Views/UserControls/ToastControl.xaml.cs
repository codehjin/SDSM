using CommunityToolkit.Mvvm.Messaging;
using SDSM_App.Messengers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Media;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// 알림
/// </summary>
public partial class ToastControl : UserControl
{
    private Storyboard aniSb;
    private DoubleAnimation aniStart;
    private DoubleAnimation aniEnd;

    public ToastControl()
    {
        InitializeComponent();

        aniSb = new Storyboard();
        aniStart = new DoubleAnimation();
        aniEnd = new DoubleAnimation();

        WeakReferenceMessenger.Default.Register<NotifyMessage>(this, (r, m) =>
        {
            NotifyReceive(m);
        });
    }

    public Visibility RootVisibility
    {
        get { return (Visibility)GetValue(RootVisibilityProperty); }
        set { SetValue(RootVisibilityProperty, value); }
    }
    public static readonly DependencyProperty RootVisibilityProperty = DependencyProperty.Register(
        "RootVisibility",
        typeof(Visibility),
        typeof(ToastControl),
        new PropertyMetadata(Visibility.Collapsed));

    public double RootOpacity
    {
        get { return (double)GetValue(RootOpacityProperty); }
        set { SetValue(RootOpacityProperty, value); }
    }
    public static readonly DependencyProperty RootOpacityProperty = DependencyProperty.Register(
        "RootOpacity",
        typeof(double),
        typeof(ToastControl),
        new PropertyMetadata(0.0));

    public string ToastMessage
    {
        get { return (string)GetValue(ToastMessageProperty); }
        set { SetValue(ToastMessageProperty, value); }
    }
    public static readonly DependencyProperty ToastMessageProperty = DependencyProperty.Register("ToastMessage", typeof(string), typeof(ToastControl));

    public double TimeValue
    {
        get { return (double)GetValue(TimeValueProperty); }
        set { SetValue(TimeValueProperty, value); }
    }
    public static readonly DependencyProperty TimeValueProperty = DependencyProperty.Register("TimeValue", typeof(double), typeof(ToastControl));

    public bool IsSucess
    {
        get { return (bool)GetValue(IsSucessProperty); }
        set { SetValue(IsSucessProperty, value); }
    }
    public static readonly DependencyProperty IsSucessProperty = DependencyProperty.Register("IsSucess", typeof(bool), typeof(ToastControl));

    private void NotifyReceive(NotifyMessage message)
    {
        try
        {
            ToastMessage = message.Message;
            //시간설정
            TimeValue = message.IsSucess ? 2.0 : 3.0;
            IsSucess = message.IsSucess;

            aniSb = new Storyboard();
            aniStart = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.5)));
            aniEnd = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(0.5)))
            {
                BeginTime = TimeSpan.FromSeconds(TimeValue)
            };

            SystemSounds.Hand.Play();

            ToastMessageChanged();
        }
        catch { }
    }

    private void ToastMessageChanged()
    {
        if (RootVisibility == Visibility.Collapsed)
        {
            RootVisibility = Visibility.Visible;
        }

        if (aniSb.Children.Any())
        {
            aniSb.Stop(this);
            aniSb.Children.Clear();
        }

        Storyboard.SetTargetName(aniStart, "Root");
        Storyboard.SetTargetProperty(aniStart, new PropertyPath(OpacityProperty));

        Storyboard.SetTargetName(aniEnd, "Root");
        Storyboard.SetTargetProperty(aniEnd, new PropertyPath(OpacityProperty));

        aniSb.Children.Add(aniStart);
        aniSb.Children.Add(aniEnd);

        aniSb.Completed += (sender, e) =>
        {
            aniSb.Stop(this);
            aniSb.Children.Clear();
            RootVisibility = Visibility.Collapsed;
        };

        aniSb.Begin(this, true);
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (aniSb.Children.Any())
        {
            aniSb.Stop(this);
            aniSb.Children.Clear();
        }
        RootOpacity = 0;
        RootVisibility = Visibility.Collapsed;
    }
}
