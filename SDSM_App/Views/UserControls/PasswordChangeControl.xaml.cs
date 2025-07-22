using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.Models;
using SDSM_App.Models.Repositories.Interfases;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// 비밀번호 변경
/// </summary>
public partial class PasswordChangeControl : UserControl, ITransientService
{
    public PasswordChangeControl()
    {
        InitializeComponent();

        IsEnabledSaveButton();
    }

    //현재비밀번호
    private void CurrentPasswordBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        IsEnabledSaveButton();
    }

    //새비밀번호
    private void NewPasswordBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        IsEnabledSaveButton();
    }

    //새 비밀번호 확인
    private void NewCheckPasswordBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        IsEnabledSaveButton();
    }

    private void IsEnabledSaveButton()
    {
        SaveButton.IsEnabled = !string.IsNullOrWhiteSpace(CurrentPasswordBox.Password)
                            && !string.IsNullOrWhiteSpace(NewPasswordBox.Password)
                            && !string.IsNullOrWhiteSpace(NewCheckPasswordBox.Password);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var currentPassword = CurrentPasswordBox.Password;
        var newPassword = NewPasswordBox.Password;
        var newCheckPassword = NewCheckPasswordBox.Password;

        var context = new SDSMDbContext();
        var adminModel = context.SelfUsers.OrderBy(m => m.SelfUserId).FirstOrDefault();

        if (adminModel != null)
        {
            if (adminModel.UserPassword != currentPassword)
            {
                ErrorTextBlock.Text = "현재 비밀번호가 잘못되었습니다.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            if (newPassword != newCheckPassword)
            {
                ErrorTextBlock.Text = "새 비밀번호가 잘못되었습니다.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            ErrorTextBlock.Visibility = Visibility.Collapsed;
            ErrorTextBlock.Text = string.Empty;

            var repository = App.Current.Services.GetRequiredService<ISelfUserRepository>();

            adminModel.UserPassword = newPassword;

            if (await repository.UpdateAsync(adminModel))
            {
                //비밀번호 
                GB.UserPassword = newPassword;

                var toast = (GB.SaveMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

                CurrentPasswordBox.Password = string.Empty;
                NewPasswordBox.Password = string.Empty;
                NewCheckPasswordBox.Password = string.Empty;

                if (Window.GetWindow(App.Current.MainWindow) is MainWindow mainWin)
                {
                    mainWin.UserDropDownButton.IsOpen = false;
                }
            }
        }
        else
        {
            var message = $"관리자 계정이 존재하지 않습니다.{Environment.NewLine}관리자에게 문의 하세요.";
            MessageBox.Show(message, "사용자 인증 실패", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
