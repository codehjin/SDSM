using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Windows.Media;
using Telerik.Windows.Controls.Map;
using Telerik.Windows.Data;

namespace SDSM_App.Models;

public partial class MapModel : ObservableObject
{
    [ObservableProperty] private Location _location;
    [ObservableProperty] private string _division = string.Empty;
    [ObservableProperty] private string _project = string.Empty;
    [ObservableProperty] private string _caption = string.Empty;
    [ObservableProperty] private string _label = string.Empty;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private Brush? _markerFill;
    [ObservableProperty] private int _count;
    [ObservableProperty] private int _pointCount;
    //검색에 사용되면 True, 검색에 사용되지 않으면 False
    [ObservableProperty] private bool _isSearch;

    public bool IsSpecies { get; set; }
    public bool IsScoreGrade { get; set; }
    public bool IsSpeciesCount { get; set; }
    public bool IsIndividualCount { get; set; }
    public bool IsSubSpecies { get; set; }
    public bool IsWaterQuality { get; set; }

    //검색어 : 하위가 없는 경우는 국명, 하위가 있는 경우는 출현종 국명 목록
    public string SearchText { get; set; } = string.Empty;
    public List<MapData> Data { get; set; } = [];
}

public partial class MapDivision : ObservableObject
{
    public string Project { get; set; } = string.Empty;
    public string Division { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public int Year { get; set; }
    public int DataCount { get; set; }
    [ObservableProperty] private bool _isSelected;
    public RadObservableCollection<MapModel> Subs { get; set; } = [];
}

public class MapData
{
    [DisplayName("ID")] public int Id { get; set; }
    [DisplayName("연도")] public int SurveyYear { get; set; }
    [DisplayName("구분")] public string? Caption { get; set; }
    [DisplayName("프로젝트명")] public string? Project { get; set; }
    [DisplayName("사업장")] public string? Workplace { get; set; }
    [DisplayName("과업 대표하천")] public string? WorkplaceRiver { get; set; }
    [DisplayName("연도차수")] public string? YearChsu { get; set; }
    [DisplayName("조사일")] public DateTime? SurveyDate { get; set; }
    [DisplayName("하천명")] public string? River { get; set; }
    [DisplayName("지점명")] public string? Site { get; set; }

    [DisplayName("국명")] public string? SpeciesKo { get; set; }
    [DisplayName("학명")] public string? SpeciesEn { get; set; }
    [DisplayName("위도")] public double? Lat { get; set; }
    [DisplayName("경도")] public double? Lng { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }

    [DisplayName("점수")] public double? Score { get; set; }
    [DisplayName("등급")] public string? Grade { get; set; }

    [DisplayName("종수")] public int? SpeciesCount { get; set; }
    [DisplayName("개체수")] public double? IndividualCount { get; set; }

    //어류, 저서동물 하위 국명을 "," 묶음
    [DisplayName("출현종")] public string SubSpecies { get; set; } = string.Empty;

    [DisplayName("pH 등급")] public int? PHGrade { get; set; }
    [DisplayName("BOD 등급")] public string? BODGrade { get; set; }
    [DisplayName("COD 등급")] public string? CODGrade { get; set; }
    [DisplayName("TOC 등급")] public string? TOCGrade { get; set; }
    [DisplayName("SS 등급")] public int? SSGrade { get; set; }
    [DisplayName("DO 등급")] public string? DOGrade { get; set; }
    [DisplayName("T-P 등급")] public string? TPGrade { get; set; }
    [DisplayName("대장균군 등급")] public string? EColiGrade { get; set; }

    //출현종 셀에 마우스 오버시 상세정보 자료
    public List<NameValue>? SubSpeciesData { get; set; }
}
