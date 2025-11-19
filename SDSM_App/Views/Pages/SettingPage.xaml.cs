using SDSM_App.InjectableServices;
using SDSM_App.ViewModels;
using SDSM_Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Telerik.Windows.Controls;

namespace SDSM_App.Views.Pages;

/// <summary>
/// 설정
/// </summary>
public partial class SettingPage : Page, ITransientService
{
    private readonly SettingPageViewModel _vm;

    public SettingPage(SettingPageViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        DataContext = _vm;
    }

    #region TextBox Event

    //공백제거
    private void EmptyClear_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is RadWatermarkTextBox watermarkTextBox)
        {
            if (!string.IsNullOrWhiteSpace(watermarkTextBox.Text))
            {
                watermarkTextBox.Text = watermarkTextBox.Text.Replace(" ", string.Empty).ToUpper();
            }
            else
            {
                watermarkTextBox.Text = null;
            }
        }
        else if (sender is TextBox textBox)
        {
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = textBox.Text.Replace(" ", string.Empty).ToUpper();
            }
            else
            {
                textBox.Text = null;
            }
        }
    }

    //앞뒤공백제거
    private void Trim_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is RadWatermarkTextBox watermarkTextBox)
        {
            if (!string.IsNullOrWhiteSpace(watermarkTextBox.Text))
            {
                watermarkTextBox.Text = watermarkTextBox.Text.Trim();
            }
            else
            {
                watermarkTextBox.Text = null;
            }
        }
        else if (sender is TextBox textBox)
        {
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = textBox.Text.Trim();
            }
            else
            {
                textBox.Text = null;
            }
        }
    }

    //실수값 소수점 자리맞춤
    private void DoubleValueToRound_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is RadNumericUpDown upDown && upDown.Value != null)
        {
            if (double.TryParse(upDown.Value.ToString(), out double value))
            {
                upDown.Value = Math.Round(value, upDown.NumberDecimalDigits, MidpointRounding.AwayFromZero);
            }
        }
    }

    #endregion

    private void EditRadButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if(sender is RadButton button && button.DataContext is SiteDivision item)
        {
            _vm.SiteDivisionEditSelectionExecutedCommand.Execute(item);
            e.Handled = true;
        }
    }
}
