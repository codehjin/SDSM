using System.ComponentModel;

namespace SDSM_Models;

public class ImportBenthosSpecies
{
    [Description("오류내용")]
    public string ErrorContent { get; set; } = string.Empty;

    [DisplayName("문")] public string? PhylumKo { get; set; }
    [DisplayName("강")] public string? ClassKo { get; set; }
    [DisplayName("목")] public string? OrderKo { get; set; }
    [DisplayName("과")] public string? FamilyKo { get; set; }
    [DisplayName("국명")] public string? SpeciesKo { get; set; }
    [DisplayName("학명")] public string? SpeciesEn { get; set; }
    [DisplayName("개체수")] public double? IndividualCount { get; set; }

    //일괄입력시 기존정보과 매치
    [DisplayName("번호")] public int MacthNumber { get; set; }
}
