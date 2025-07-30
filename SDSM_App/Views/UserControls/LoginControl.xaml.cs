using SDSM_App.InjectableServices;
using SDSM_App.Models;
using System.Windows;
using System.Windows.Controls;

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
        var password = PasswordTextBox.Password;

        if (string.IsNullOrWhiteSpace(password))
        {
            PasswordTextBox.Focus();
            return;
        }

        using var context = new SDSMDbContext();

        var admin = context.SelfUsers.Where(m => m.UserName == "관리자").FirstOrDefault();
        if (admin != null)
        {
            if (admin.UserPassword == password)
            {
                ErrorTextBlock.Text = string.Empty;
                Button_Click(null!, null!);
            }
            else
            {
                ErrorTextBlock.Text = "비밀번호가 잘못되었습니다.";
            }
        }
        else
        {
            ErrorTextBlock.Text = "관리자 계정이 존재하지 않습니다.";
        }
    }

    private void MainCloseRadButton_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = Window.GetWindow(App.Current.MainWindow) as MainWindow;
        mainWindow?.Close();
    }
}
