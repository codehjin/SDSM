using CommunityToolkit.Mvvm.ComponentModel;

namespace SDSM_App.Models;

public partial class SearchItem : ObservableObject
{
    public string Caption { get; set; } = string.Empty;
    public string? SearchText { get; set; }

    [ObservableProperty] private bool _isCheckedTable;
    [ObservableProperty] private bool _isCheckedChart;
}