using System.ComponentModel;

namespace SDSM_Models;

public class ImportVegetationENVSpecies
{
    [Description("오류내용")]
    public string ErrorContent { get; set; } = string.Empty;

    [DisplayName("계층")] public string? Layer { get; set; }
    [DisplayName("종명")] public string? SpeciesKo { get; set; }
    [DisplayName("피도")] public string? CoverDegree { get; set; }

    //일괄입력시 기존정보과 매치
    [DisplayName("ID")] public string? ID { get; set; }
}
