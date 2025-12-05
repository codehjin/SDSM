using System.ComponentModel;

namespace SDSM_Models;

public class ImportHabitatWaterEdge
{
    [Description("오류내용")]
    public string ErrorContent { get; set; } = string.Empty;

    [DisplayName("연도차수")] public string? YearChsu { get; set; }
    [DisplayName("지점명")] public string? Site { get; set; }
    [DisplayName("조사일\n(2010-01-01~오늘)")] public string? SurveyDate { get; set; }
    [DisplayName("대권역명")] public string? MajorRegion { get; set; }
    [DisplayName("중권역명")] public string? MiddleRegion { get; set; }
    [DisplayName("하천명")] public string? River { get; set; }
    [DisplayName("하천유형")] public string? RiverType { get; set; }
    [DisplayName("위도")] public string? Lat { get; set; }
    [DisplayName("경도")] public string? Lng { get; set; }
    [DisplayName("날씨")] public string? Weather { get; set; }
    [DisplayName("조사기관")] public string? SurveyAgency { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("조사불가시")] public string? SurveyUnavailableReason { get; set; }
    [DisplayName("특이사항")] public string? Note { get; set; }
    [DisplayName("자연적\n 종횡사주")] public string? EvaluationItem1 { get; set; }
    [DisplayName("하도정비/\n하도 자연성")] public string? EvaluationItem2 { get; set; }
    [DisplayName("유속\n 다양성")] public string? EvaluationItem3 { get; set; }
    [DisplayName("하천변 폭")] public string? EvaluationItem4 { get; set; }
    [DisplayName("저수로\n 하안공")] public string? EvaluationItem5 { get; set; }
    [DisplayName("제방하안\n 재료")] public string? EvaluationItem6 { get; set; }
    [DisplayName("저질 상태")] public string? EvaluationItem7 { get; set; }
    [DisplayName("횡구조물")] public string? EvaluationItem8 { get; set; }
    [DisplayName("제외지\n 토지 이용")] public string? EvaluationItem9 { get; set; }
    [DisplayName("제내지\n 토지 이용")] public string? EvaluationItem10 { get; set; }

    //자동계산
    [DisplayName("평가점수")] public double? EvaluationScore { get; set; }
    //자동계산
    [DisplayName("평가등급")] public string? EvaluationGrade { get; set; }   
}
