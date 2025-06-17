using CommunityToolkit.Mvvm.ComponentModel;
using Telerik.Windows.Controls.Map;

namespace SDSM_App.Models;

public partial class MapModel : ObservableObject
{
    public MapModel(Location location, string caption ="", string knpName = "", string no = "", string colonyName = "", bool isSelected = false, bool isVisible = true, bool isVisibleLabel = false)
    {
        Location = location;
        KnpName = knpName;
        Caption = caption;
        NO = no;
        ColonyName = colonyName;
        IsSelected = isSelected;
        IsVisible = isVisible;
        IsVisibleLabel = isVisibleLabel;
    }

    [ObservableProperty] private Location _location;
    [ObservableProperty] private string _knpName;
    [ObservableProperty] private string _caption;
    [ObservableProperty] private string _nO;
    [ObservableProperty] private string _colonyName;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private bool _isVisibleLabel;
}
