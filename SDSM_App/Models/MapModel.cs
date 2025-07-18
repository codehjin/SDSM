using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;
using Telerik.Windows.Controls.Map;

namespace SDSM_App.Models;

public partial class MapModel : ObservableObject
{
    [ObservableProperty] private Location _location;
    [ObservableProperty] private string _caption = string.Empty;
    [ObservableProperty] private string _speciesType = string.Empty;
    [ObservableProperty] private int _year;
    [ObservableProperty] private string _label = string.Empty;
    [ObservableProperty] private string _label2 = string.Empty;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private Brush? _markerFill;
}
