using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 어류 뷰
/// </summary>
[Table("View_FishSurvey")]
public class FishSurveyView
{
    [DisplayName("ID")] public int FishSurveyId { get; set; }
    [DisplayName("연도/차수")] public string SurveyYearChsu { get; set; } = string.Empty;
    [DisplayName("지점명")] public string Site { get; set; } = string.Empty;
    [DisplayName("조사일")] public DateTime SurveyDate { get; set; }
    [DisplayName("대권역명")] public string? BigZone { get; set; }
    [DisplayName("중권역명")] public string? MiddleZone { get; set; }
    [DisplayName("하천명")] public string? River { get; set; }
    [DisplayName("하천유형")] public string? RiverType { get; set; }
    [DisplayName("위도")] public double? Lat { get; set; }
    [DisplayName("경도")] public double? Lng { get; set; }
    [DisplayName("날씨")] public int? Item1 { get; set; }
    [DisplayName("조사기관")] public string? Item2 { get; set; }
    [DisplayName("조사자")] public string? Item3 { get; set; }
    [DisplayName("채집소요시간(분)")] public double? Item4 { get; set; }
    [DisplayName("채집도구")] public string? Item5 { get; set; }
    [DisplayName("하천차수")] public int? ARiverChsu { get; set; }
    [DisplayName("흐름상태")] public string? Item6 { get; set; }
    [DisplayName("암반")] public string? Item7 { get; set; }
    [DisplayName("콘크리트")] public int? Item8 { get; set; }
    [DisplayName("진흙이하(<0.063mm)")] public int? Item9 { get; set; }
    [DisplayName("모래(0.063-2mm)")] public int? Item10 { get; set; }
    [DisplayName("잔자갈(2-16mm)")] public int? Item11 { get; set; }
    [DisplayName("자갈(16-64mm)")] public int? Item12 { get; set; }
    [DisplayName("작은돌(64-256mm)")] public int? Item13 { get; set; }
    [DisplayName("큰돌(>256mm)")] public int? Item14 { get; set; }
    [DisplayName("총합 확인(%)")] public double? ATotalSum { get; set; }
    [DisplayName("하천형태")] public int? Item15 { get; set; }
    [DisplayName("흐름상태")] public int? Item16 { get; set; }
    [DisplayName("특이사항")] public string? Item17 { get; set; }
    [DisplayName("비고")] public string? Item18 { get; set; }
    [DisplayName("총 개체수(어류특성미확정종포함)")] public int? AItem19 { get; set; }
    [DisplayName("총 종수(어류특성미확정종포함)")] public int? AItem20 { get; set; }
    [DisplayName("총 개체수")] public int? AItem21 { get; set; }
    [DisplayName("총 종수")] public int? AItem22 { get; set; }
    [DisplayName("국내종 총 종수")] public int? AItem23 { get; set; }
    [DisplayName("여울성저서종수")] public int? AItem24 { get; set; }
    [DisplayName("민감종수")] public int? AItem25 { get; set; }
    [DisplayName("내성종개체수비율(%)")] public double? AItem26 { get; set; }
    [DisplayName("잡식종개체수비율(%)")] public double? AItem27 { get; set; }
    [DisplayName("충식종개체수비율(%)")] public double? AItem28 { get; set; }
    [DisplayName("국내종총개체수")] public double? AItem29 { get; set; }
    [DisplayName("비정상개체수(총합)")] public int? AItem30 { get; set; }
    [DisplayName("기형(deformity) DE 종명(개체수)")] public int? Item31 { get; set; }
    [DisplayName("피부손상(lesions) LE 종명(개체수)")] public int? Item32 { get; set; }
    [DisplayName("종양(tumors) TU 종명(개체수)")] public int? Item33 { get; set; }
    [DisplayName("종양(tumors) TU 종명(개체수)")] public int? Item34 { get; set; }
    [DisplayName("비정상개체수비율 (%)")] public double? AItem35 { get; set; }
    [DisplayName("M1 (국내종의 총 종수)")] public int? AItem36 { get; set; }
    [DisplayName("M2 (여울성 저서종수)")] public int? AItem37 { get; set; }
    [DisplayName("M3 (민감종수)")] public int? AItem38 { get; set; }
    [DisplayName("M4 (내성종의 개체수 비율)")] public double? AItem39 { get; set; }
    [DisplayName("M5 (잡식종의 개체수 비율)")] public double? AItem40 { get; set; }
    [DisplayName("M6 (국내종의 충식종 개체수 비율)")] public double? AItem41 { get; set; }
    [DisplayName("M7 (채집된 국내종의 총 개체수)")] public double? AItem42 { get; set; }
    [DisplayName("M8 (비정상종의 개체수 비율)")] public double? AItem43 { get; set; }
    [DisplayName("메트릭모델값")] public int? AItem44 { get; set; }
    [DisplayName("메트릭모델값")] public int? AItem45 { get; set; }
    [DisplayName("등급(A-D)")] public string? AItem46 { get; set; }
    [DisplayName("M1 (국내종의 총 종수)")] public int? AItem47 { get; set; }
    [DisplayName("M2 (여울성 저서종수)")] public int? AItem48 { get; set; }
    [DisplayName("M3 (민감종수)")] public int? AItem49 { get; set; }
    [DisplayName("M4 (내성종의 개체수 비율)")] public double? AItem50 { get; set; }
    [DisplayName("M5 (잡식종의 개체수 비율)")] public double? AItem51 { get; set; }
    [DisplayName("M6 (국내종의 충식종 개체수 비율)")] public double? AItem52 { get; set; }
    [DisplayName("M7 (채집된 국내종의 총 개체수)")] public int? AItem53 { get; set; }
    [DisplayName("M8 (비정상종의 개체수 비율)")] public double? AItem54 { get; set; }
    [DisplayName("M1 (국내종의 총 종수)")] public int? AItem55 { get; set; }
    [DisplayName("M2 (여울성 저서종수)")] public int? AItem56 { get; set; }
    [DisplayName("M3 (민감종수)")] public int? AItem57 { get; set; }
    [DisplayName("M4 (내성종의 개체수 비율)")] public double? AItem58 { get; set; }
    [DisplayName("M5 (잡식종의 개체수 비율)")] public double? AItem59 { get; set; }
    [DisplayName("M6 (국내종의 충식종 개체수 비율)")] public double? AItem60 { get; set; }
    [DisplayName("M7 (채집된 국내종의 총 개체수)")] public int? AItem61 { get; set; }
    [DisplayName("M8 (비정상종의 개체수 비율)")] public double? AItem62 { get; set; }
    [DisplayName("메트릭모델값")] public int? AItem63 { get; set; }
    [DisplayName("메트릭모델값(특이사항 반영)")] public string? AItem64 { get; set; }
    [DisplayName("FAI 등급(A-E)")] public string? AItem65 { get; set; }

    [DisplayName("종수")] public int? SpeciesCount { get; set; }
    [DisplayName("개체수")] public int? NoUSum { get; set; }
}
