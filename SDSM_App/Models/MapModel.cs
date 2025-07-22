using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Windows.Media;
using Telerik.Windows.Controls.Map;

namespace SDSM_App.Models;

public partial class MapModel : ObservableObject
{
    [ObservableProperty] private Location _location;
    [ObservableProperty] private string _caption = string.Empty;
    [ObservableProperty] private string _label = string.Empty;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private Brush? _markerFill;

    [DisplayName("연도차수")] public string? YearChsu { get; set; }
    [DisplayName("조사일")] public DateTime? SurveyDate { get; set; }
    [DisplayName("하천명")] public string? River { get; set; }
    [DisplayName("지점명")] public string? Site { get; set; }

    [DisplayName("일반명")] public string? SpeciesKo { get; set; }
    [DisplayName("학명")] public string? SpeciesEn { get; set; }
    public bool IsSpecies { get; set; }
    [DisplayName("위도")] public double? Lat { get; set; }
    [DisplayName("경도")] public double? Lng { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }

    [DisplayName("점수")] public double? Score { get; set; }
    [DisplayName("등급")] public string? Grade { get; set; }
    public bool IsScoreGrade { get; set; }

    [DisplayName("종수")] public int? SpeciesCount { get; set; }
    public bool IsSpeciesCount { get; set; }
    [DisplayName("개체수")] public double? IndividualCount { get; set; }
    public bool IsIndividualCount { get; set; }

    [DisplayName("pH 등급")] public int? PHGrade { get; set; }
    [DisplayName("BOD 등급")] public string? BODGrade { get; set; }
    [DisplayName("COD 등급")] public string? CODGrade { get; set; }
    [DisplayName("TOC 등급")] public string? TOCGrade { get; set; }
    [DisplayName("SS 등급")] public int? SSGrade { get; set; }
    [DisplayName("DO 등급")] public string? DOGrade { get; set; }
    [DisplayName("T-P 등급")] public string? TPGrade { get; set; }
    [DisplayName("대장균군 등급")] public string? EColiGrade { get; set; }
    public bool IsWaterQuality { get; set; }
}
