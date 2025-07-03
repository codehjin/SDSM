using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using Telerik.Documents.SpreadsheetStreaming;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView.SpreadsheetStreamingExport;

namespace SDSM_App.Views.Pages;

/// <summary>
/// 일괄등록
/// </summary>
public partial class BulkRegisterPage : Page, ITransientService
{
    private readonly BulkRegisterPageViewModel _vm;

    public BulkRegisterPage(BulkRegisterPageViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        DataContext = _vm;
    }

    #region This 이벤트

    #endregion

    #region Control 이벤트    

    private void OpenRadButton_Click(object sender, RoutedEventArgs e)
    {
        if(sender is RadButton button && button.Content is string content && button.Tag is string tag)
        {
            Type? type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).FirstOrDefault(x => x.Name == tag);
            if (type != null)
            {
                CaptionTextBlock.Text = content;
                ContentLayout.Visibility = Visibility.Visible;
                ContentControl.Content = App.Current.Services.GetService(type);
            }
        }
    }

    private void CloseRadButton_Click(object sender, RoutedEventArgs e)
    {
        ContentLayout.Visibility = Visibility.Collapsed;
        ContentControl.Content = null;
    }


    #endregion

    #region 메서드

    #endregion

}
