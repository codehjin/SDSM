using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 어류종
/// Benthic Macroinvertebrate
/// </summary>

[Table("FishSpeciesList")]
public class FishSpeciesList
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int FishSpeciesListId { get; set; }

    [DisplayName("어류특성확정")] public string? FishTrait { get; set; }
    [DisplayName("내성도 길드")] public string? ToleranceGuild { get; set; }
    [DisplayName("섭식 길드")] public string? FeedingGuild { get; set; }
    [DisplayName("서식지 길드")] public string? HabitatGuild { get; set; }
    [DisplayName("외래종")] public string? Exotic { get; set; }
    [DisplayName("고유종")] public string? Endemic { get; set; }
    [DisplayName("멸종위기 I급")] public string? Endangered1 { get; set; }
    [DisplayName("멸종위기 II급")] public string? Endangered2 { get; set; }
    [DisplayName("천연기념물")] public string? NaturalMonument { get; set; }
    [DisplayName("계통별 순서")] public int? LineageOrder { get; set; }
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
