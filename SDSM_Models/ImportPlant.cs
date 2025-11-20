using System.ComponentModel;

namespace SDSM_Models;

public class ImportPlant
{
    [Description("오류내용")]
    public string ErrorContent { get; set; } = string.Empty;

    [DisplayName("연도")] public string? SurveyYear { get; set; }
    [DisplayName("차수")] public string? Chsu { get; set; }
    [DisplayName("조사일")] public string? SurveyDate { get; set; }  //필수
    [DisplayName("날씨")] public string? Weather { get; set; }
    [DisplayName("지점명")] public string? Site { get; set; }
    [DisplayName("하천명")] public string? River { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("출현위도")] public string? Lat { get; set; }
    [DisplayName("출현경도")] public string? Lng { get; set; }
    [DisplayName("위치")] public string? SurveyLocation { get; set; }
    [DisplayName("종명")] public string? SpeciesKo { get; set; }
    [DisplayName("학명")] public string? SpeciesEn { get; set; }
    [DisplayName("외래식물구분")] public string? AlienPlant { get; set; }
    [DisplayName("생태교란종")] public string? InvasiveSpecies { get; set; }
}
