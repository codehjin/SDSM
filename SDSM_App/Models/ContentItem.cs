using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace SDSM_App.Models;

public partial class ContentItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isVisibleLegend;
    public string OpacityName { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string MbTilesName { get; set; } = string.Empty;
    public List<ContentSubItem> ContentSubItems { get; set; } = [];

    public bool IsBiotopesRarity { get; set; }
    public bool IsInvasiveSpecies { get; set; }
}

public class ContentSubItem
{
    public string Grade { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Area { get; set; }
    public Brush MarkerFill { get; set; } = new SolidColorBrush(Colors.Transparent);

    public bool IsBiotopesRarity { get; set; }
    public bool IsInvasiveSpecies { get; set; }
}
