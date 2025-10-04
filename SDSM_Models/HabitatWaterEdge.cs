using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 서식수변
/// </summary>
[Table("HabitatWaterEdge")]
public class HabitatWaterEdge
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int HabitatWaterEdgeId { get; set; }

    [DisplayName("연도")][Required] public int SurveyYear { get; set; }
    [DisplayName("연도차수")][Required] public string YearChsu { get; set; } = string.Empty;
    [DisplayName("지점명")][Required] public string Site { get; set; } = string.Empty;
    [DisplayName("조사일")] public DateTime? SurveyDate { get; set; }  //필수
    [DisplayName("대권역명")] public string? MajorRegion { get; set; }
    [DisplayName("중권역명")] public string? MiddleRegion { get; set; }
    [DisplayName("하천명")] public string? River { get; set; }
    [DisplayName("하천유형")] public string? RiverType { get; set; }
    [DisplayName("위도")] public double? Lat { get; set; }
    [DisplayName("경도")] public double? Lng { get; set; }
    [DisplayName("날씨")] public string? Weather { get; set; }
    [DisplayName("조사기관")] public string? SurveyAgency { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("조사불가시")] public string? SurveyUnavailableReason { get; set; }
    [DisplayName("특이사항")] public string? Note { get; set; }
    [DisplayName("자연적 종횡사주")] public double? EvaluationItem1 { get; set; }
    [DisplayName("하도정비/하도 자연성")] public double? EvaluationItem2 { get; set; }
    [DisplayName("유속 다양성")] public double? EvaluationItem3 { get; set; }
    [DisplayName("하천변 폭")] public double? EvaluationItem4 { get; set; }
    [DisplayName("저수로 하안공")] public double? EvaluationItem5 { get; set; }
    [DisplayName("제방하안 재료")] public double? EvaluationItem6 { get; set; }
    [DisplayName("저질 상태")] public double? EvaluationItem7 { get; set; }
    [DisplayName("횡구조물")] public double? EvaluationItem8 { get; set; }
    [DisplayName("제외지 토지 이용")] public double? EvaluationItem9 { get; set; }
    [DisplayName("제내지 토지 이용")] public double? EvaluationItem10 { get; set; }
    [DisplayName("평가점수")] public double? EvaluationScore { get; set; }
    [DisplayName("평가등급")] public string? EvaluationGrade { get; set; }

    [NotMapped][DisplayName("오류내용")] public string ErrorContent { get; set; } = string.Empty;
}
