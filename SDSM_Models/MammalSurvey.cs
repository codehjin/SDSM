using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 포유류 조사
/// </summary>
[Table("MammalSurvey")]
public class MammalSurvey
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int MammalSurveyId { get; set; }

    [DisplayName("연도/차수")][Required] public string SurveyYearChsu { get; set; } = string.Empty;
    [DisplayName("조사일")][Required] public DateTime SurveyDate { get; set; }
    [DisplayName("날씨")] public string? Weather { get; set; }
    [DisplayName("하천명")] public string? River { get; set; }
    [DisplayName("지점명")] public string? Site { get; set; }
    [DisplayName("일반명")] public string? SpeciesKo { get; set; }
    [DisplayName("학명")] public string? SpeciesEn { get; set; }
    [DisplayName("특징")] public string? Feature { get; set; }
    [DisplayName("위도")] public double? Lat { get; set; }
    [DisplayName("경도")] public double? Lng { get; set; }
    [DisplayName("관찰지 유형")] public string? ObservationSite { get; set; }
    [DisplayName("포획")] public int? Trace1 { get; set; }
    [DisplayName("관찰")] public int? Trace2 { get; set; }
    [DisplayName("울음")] public int? Trace3 { get; set; }
    [DisplayName("사체")] public int? Trace4 { get; set; }
    [DisplayName("족적")] public int? Trace5 { get; set; }
    [DisplayName("털")] public int? Trace6 { get; set; }
    [DisplayName("식흔")] public int? Trace7 { get; set; }
    [DisplayName("굴")] public int? Trace8 { get; set; }
    [DisplayName("번식지")] public int? Trace9 { get; set; }
    [DisplayName("배설물")] public int? Trace10 { get; set; }
    [DisplayName("카메라")] public int? Trace11 { get; set; }
    [DisplayName("기타(탐문 등)")] public int? Trace12 { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("특이사항")] public string? Note { get; set; }
}
