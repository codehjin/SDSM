using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAPICodePack.Dialogs;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Telerik.Windows.Controls;
using Telerik.Windows.Documents.Spreadsheet.FormatProviders.OpenXml.Xlsx;
using Telerik.Windows.Documents.Spreadsheet.Model;
using Telerik.Windows.Documents.Spreadsheet.PropertySystem;
using Telerik.Windows.Documents.Spreadsheet.Utilities;

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
