using SDSM_App.InjectableServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// 페이지 사이즈
/// </summary>
public partial class PageSizeCommandControl : UserControl
{
    public PageSizeCommandControl()
    {
        InitializeComponent();
    }

    public ICommand MinCommand
    {
        get { return (ICommand)GetValue(MinCommandProperty); }
        set { SetValue(MinCommandProperty, value); }
    }
    public static readonly DependencyProperty MinCommandProperty = DependencyProperty.Register("MinCommand", typeof(ICommand), typeof(PageSizeCommandControl));

    public ICommand AvgCommand
    {
        get { return (ICommand)GetValue(AvgCommandProperty); }
        set { SetValue(AvgCommandProperty, value); }
    }
    public static readonly DependencyProperty AvgCommandProperty = DependencyProperty.Register("AvgCommand", typeof(ICommand), typeof(PageSizeCommandControl));

    public ICommand MaxCommand
    {
        get { return (ICommand)GetValue(MaxCommandProperty); }
        set { SetValue(MaxCommandProperty, value); }
    }
    public static readonly DependencyProperty MaxCommandProperty = DependencyProperty.Register("MaxCommand", typeof(ICommand), typeof(PageSizeCommandControl));
}
