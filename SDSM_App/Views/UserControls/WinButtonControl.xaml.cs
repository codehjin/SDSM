using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.IconPacks;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// Window Control
/// </summary>
public partial class WinButtonControl : UserControl
{
    public WinButtonControl()
    {
        InitializeComponent();
    }

    //Default Foreground
    public Brush DefaultForegroundBrush
    {
        get { return (Brush)GetValue(DefaultForegroundBrushProperty); }
        set { SetValue(DefaultForegroundBrushProperty, value); }
    }
    public static readonly DependencyProperty DefaultForegroundBrushProperty = DependencyProperty.Register("DefaultForegroundBrush", typeof(Brush), typeof(WinButtonControl));

    //MouseOver Foreground
    public Brush MouseOverForegroundBrush
    {
        get { return (Brush)GetValue(MouseOverForegroundBrushProperty); }
        set { SetValue(MouseOverForegroundBrushProperty, value); }
    }
    public static readonly DependencyProperty MouseOverForegroundBrushProperty = DependencyProperty.Register("MouseOverForegroundBrush", typeof(Brush), typeof(WinButtonControl));

    //MouseOver Background
    public Brush MouseOverBackgroundBrush
    {
        get { return (Brush)GetValue(MouseOverBackgroundBrushProperty); }
        set { SetValue(MouseOverBackgroundBrushProperty, value); }
    }
    public static readonly DependencyProperty MouseOverBackgroundBrushProperty = DependencyProperty.Register("MouseOverBackgroundBrush", typeof(Brush), typeof(WinButtonControl));

    //CornerRadius
    public CornerRadius MouseOverBorderCornerRadius
    {
        get { return (CornerRadius)GetValue(MouseOverBorderCornerRadiusProperty); }
        set { SetValue(MouseOverBorderCornerRadiusProperty, value); }
    }
    public static readonly DependencyProperty MouseOverBorderCornerRadiusProperty = DependencyProperty.Register("MouseOverBorderCornerRadius", typeof(CornerRadius), typeof(WinButtonControl));

    //아이콘너비
    public double IconWidth
    {
        get { return (double)GetValue(IconWidthProperty); }
        set { SetValue(IconWidthProperty, value); }
    }
    public static readonly DependencyProperty IconWidthProperty = DependencyProperty.Register("IconWidth", typeof(double), typeof(WinButtonControl), new PropertyMetadata(12.0));

    //아이콘 높이
    public double IconHeight
    {
        get { return (double)GetValue(IconHeightProperty); }
        set { SetValue(IconHeightProperty, value); }
    }
    public static readonly DependencyProperty IconHeightProperty = DependencyProperty.Register("IconHeight", typeof(double), typeof(WinButtonControl), new PropertyMetadata(12.0));

    public PackIconCodiconsKind Icon
    {
        get { return (PackIconCodiconsKind)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }
            public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(PackIconCodiconsKind), typeof(WinButtonControl));

    public event RoutedEventHandler ElementClick
    {
        add { AddHandler(ElementClickEvent, value); }
        remove { RemoveHandler(ElementClickEvent, value); }
    }
    public static readonly RoutedEvent ElementClickEvent = EventManager.RegisterRoutedEvent("ElementClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(WinButtonControl));

    private void Button_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(ElementClickEvent));
}