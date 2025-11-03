using System.ComponentModel;

namespace SDSM_Models;

public class ImportVegetation
{
    [Description("오류내용")]
    public string ErrorContent { get; set; } = string.Empty;

    [DisplayName("연도차수")] public string? YearChsu { get; set; }
    [DisplayName("지점명")] public string? Site { get; set; }
    [DisplayName("조사일")] public string? SurveyDate { get; set; }  //필수
    [DisplayName("대권역명")] public string? MajorRegion { get; set; }
    [DisplayName("중권역명")] public string? MiddleRegion { get; set; }
    [DisplayName("하천명")] public string? River { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("군락명")] public string? Colony { get; set; }
    [DisplayName("군락면적")] public string? Area { get; set; }
}
