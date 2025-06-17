using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 조류 조사
/// </summary>
[Table("BirdSurvey")]
public class BirdSurvey
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int BirdSurveyId { get; set; }

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
    [DisplayName("대항목")] public string? Category { get; set; }
    [DisplayName("세부항목")] public string? CategoryDetail { get; set; }
    [DisplayName("서식유형")] public string? HabitatType { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("특이사항")] public string? Note { get; set; }
}
