using Microsoft.Extensions.DependencyInjection;
using SDSM_App.InjectableServices;
using SDSM_App.Views.UserControls;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;

namespace SDSM_App.Views.Pages;

/// <summary>
/// 식생
/// </summary>
public partial class VegetationPage : Page, ITransientService
{
    public VegetationPage()
    {
        InitializeComponent();

        LoadContent();
    }

    #region Control 이벤트

    private void RadRadioButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadRadioButton button && button.Tag is string tag)
        {
            SetContent(tag);
        }
    }

    #endregion

    #region 메서드

    private void LoadContent()
    {
        SetContent(nameof(VegetationControl));
    }

    private void SetContent(string controlName)
    {
        Type? control = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).FirstOrDefault(x => x.Name == controlName);
        if (control != null)
        {
            SubContentControl.Content = App.Current.Services.GetRequiredService(control);
        }
    }

    #endregion
}
