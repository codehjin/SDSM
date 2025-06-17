using SDSM_App.InjectableServices;
using SDSM_App.ViewModels;
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
using System.Windows.Threading;

namespace SDSM_App.Views.Pages;

/// <summary>
/// 홈
/// </summary>
public partial class HomePage : Page, ITransientService
{
    private readonly HomePageViewModel _vm;

    public HomePage(HomePageViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        DataContext = _vm;
    }
}
