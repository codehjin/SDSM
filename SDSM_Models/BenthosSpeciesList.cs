using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 저서동물 생물종 목록
/// </summary>

[Table("BenthosSpeciesList")]
public class BenthosSpeciesList
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int BenthosSpeciesListId { get; set; }

    [DisplayName("오탁치(s)")] public double? SaprobicValue { get; set; }
    [DisplayName("지표가중치(g)")] public int? IndicatorWeight { get; set; }
    [DisplayName("멸종위기\n야생생물 I급")] public string? Endangered1 { get; set; }
    [DisplayName("멸종위기\n야생생물 II급")] public string? Endangered2 { get; set; }
    [DisplayName("한국고유종")] public string? Endemic { get; set; }
    [DisplayName("문")] public string? PhylumKo { get; set; }
    [DisplayName("강")] public string? ClassKo { get; set; }
    [DisplayName("목")] public string? OrderKo { get; set; }
    [DisplayName("과")] public string? FamilyKo { get; set; }
    [DisplayName("국명")] public string? SpeciesKo { get; set; }
    [DisplayName("학명")] public string? SpeciesEn { get; set; }

    [NotMapped]
    [DisplayName("개체수")] public double? IndividualCount { get; set; }

    [NotMapped] 
    public string SearchText => $"{SpeciesKo} {SpeciesEn}";
}
