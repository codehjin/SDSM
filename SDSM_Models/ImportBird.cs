using System.ComponentModel;

namespace SDSM_Models;

public class ImportBird
{
    [Description("오류내용")]
    public string ErrorContent { get; set; } = string.Empty;

    [DisplayName("연도차수")] public string? YearChsu { get; set; }
    [DisplayName("조사일\n(2010-01-01~오늘)")] public string? SurveyDate { get; set; }
    [DisplayName("날씨")] public string? Weather { get; set; }
    [DisplayName("하천명")] public string? River { get; set; }
    [DisplayName("지점명")] public string? Site { get; set; }
    [DisplayName("일반명")] public string? SpeciesKo { get; set; }
    [DisplayName("학명")] public string? SpeciesEn { get; set; }
    [DisplayName("특징")] public string? Feature { get; set; }
    [DisplayName("위도_도")] public string? Lat_Do { get; set; }
    [DisplayName("위도_분")] public string? Lat_Bun { get; set; }
    [DisplayName("위도_초")] public string? Lat_Cho { get; set; }
    [DisplayName("경도_도")] public string? Lng_Do { get; set; }
    [DisplayName("경도_분")] public string? Lng_Bun { get; set; }
    [DisplayName("경도_초")] public string? Lng_Cho { get; set; }
    [DisplayName("위도")] public string? Lat { get; set; }
    [DisplayName("경도")] public string? Lng { get; set; }
    [DisplayName("개체수")] public string? IndividualCount { get; set; }
    [DisplayName("도래유형")] public string? MigratoryType { get; set; }
    [DisplayName("대항목")] public string? Category { get; set; }
    [DisplayName("세부항목")] public string? CategoryDetail { get; set; }
    [DisplayName("서식유형")] public string? HabitatType { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("특이사항")] public string? Note { get; set; }
}
