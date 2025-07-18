using System.ComponentModel;

namespace SDSM_Models;

public class ImportBenthosSpecies
{
    [Description("오류내용")]
    public string ErrorContent { get; set; } = string.Empty;

    [DisplayName("오탁치(s)")] public string? SaprobicValue { get; set; }
    [DisplayName("지표가중치(g)")] public string? IndicatorWeight { get; set; }
    [DisplayName("멸종위기야생생물 I급")] public string? Endangered1 { get; set; }
    [DisplayName("멸종위기야생생물 II급")] public string? Endangered2 { get; set; }
    [DisplayName("한국고유종")] public string? Endemic { get; set; }
    [DisplayName("문")] public string? PhylumKo { get; set; }
    [DisplayName("강")] public string? ClassKo { get; set; }
    [DisplayName("목")] public string? OrderKo { get; set; }
    [DisplayName("과")] public string? FamilyKo { get; set; }
    [DisplayName("국명")] public string? SpeciesKo { get; set; }
    [DisplayName("학명")] public string? SpeciesEn { get; set; }

    //검수에서 string -> double 형식으로 변경됨
    [DisplayName("개체수")] public double? IndividualCount { get; set; }

    //일괄입력시 기존정보과 매치
    [DisplayName("번호")] public int MacthNumber { get; set; }
    [DisplayName("순위점수")] public int RankScorer { get; set; }
}
