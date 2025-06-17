using System.Windows;
using System.Windows.Controls;
using SDSM_App.InjectableServices;
using Telerik.Windows.Controls;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// 로그인
/// </summary>
public partial class LoginControl : UserControl, ITransientService
{
    public LoginControl()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler ElementClick
    {
        add { AddHandler(ElementClickEvent, value); }
        remove { RemoveHandler(ElementClickEvent, value); }
    }
    public static readonly RoutedEvent ElementClickEvent = EventManager.RegisterRoutedEvent("ElementClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(LoginControl));

    private void Button_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(ElementClickEvent));

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if(radComboBox.SelectedValue != null && radComboBox.SelectedValue is string name)
        {

            Button_Click(null!, null!);
        }
    }

    private void MainCloseRadButton_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = Window.GetWindow(App.Current.MainWindow) as MainWindow;
        mainWindow?.Close();
    }
}
