using Microsoft.Extensions.DependencyInjection;
using SDSM_App.InjectableServices;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;

namespace SDSM_App.Views.Pages;

/// <summary>
/// 통계어류
/// </summary>
public partial class StatsPage : Page, ITransientService
{
    public StatsPage()
    {
        InitializeComponent();

        LoadContent();
    }

    #region Control 이벤트

    private void RadRadioButton_Click(object sender, RoutedEventArgs e)
    {
        if(sender is RadRadioButton button && button.Tag is string tag)
        {
            SetContent(tag);
        }
    }

    #endregion

    #region 메서드

    private void LoadContent()
    {
        var statsControl = Properties.Settings.Default.StatsControl;

        //null값은 첫번째 버튼
        if (string.IsNullOrEmpty(statsControl))
            statsControl = "StatsFishControl";

        var radioButton = MenuUniformGrid.Children.OfType<RadRadioButton>().FirstOrDefault(x => x.Tag?.ToString() == statsControl);
        if (radioButton != null)
            radioButton.IsChecked = true;

        SetContent(statsControl);
    }

    private void SetContent(string statsControl)
    {
        Type? control = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).FirstOrDefault(x => x.Name == statsControl);
        if (control != null)
        {
            SubContentControl.Content = App.Current.Services.GetRequiredService(control);

            Properties.Settings.Default.StatsControl = statsControl;
            Properties.Settings.Default.Save();
        }
    }

    #endregion

}
