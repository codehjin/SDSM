using System.Windows;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace SDSM_App.Behaviors;

public class MouseWheelBehavior
{
    public static bool GetIsEnabled(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(IsEnabledProperty, value);
    }

    public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(MouseWheelBehavior), new PropertyMetadata(true, OnIsEnabledChanged));

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(bool)e.NewValue)
        {
            if(d is RadNumericUpDown upDown)
            {
                upDown.AddHandler(RadNumericUpDown.PreviewMouseWheelEvent, new MouseWheelEventHandler((s, a) => { a.Handled = true; }), true);
            }
        }
    }
}