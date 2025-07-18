using System.ComponentModel;

namespace SDSM_Models;

public class ImportMammal
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
    [DisplayName("관찰지 유형")] public string? ObservationSite { get; set; }
    [DisplayName("포획")] public string? Trace1 { get; set; }
    [DisplayName("관찰")] public string? Trace2 { get; set; }
    [DisplayName("울음")] public string? Trace3 { get; set; }
    [DisplayName("사체")] public string? Trace4 { get; set; }
    [DisplayName("족적")] public string? Trace5 { get; set; }
    [DisplayName("털")] public string? Trace6 { get; set; }
    [DisplayName("식흔")] public string? Trace7 { get; set; }
    [DisplayName("굴")] public string? Trace8 { get; set; }
    [DisplayName("번식지")] public string? Trace9 { get; set; }
    [DisplayName("배설물")] public string? Trace10 { get; set; }
    [DisplayName("카메라")] public string? Trace11 { get; set; }
    [DisplayName("기타(탐문 등)")] public string? Trace12 { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("특이사항")] public string? Note { get; set; }
}
