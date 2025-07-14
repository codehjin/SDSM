using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 어류 출현종
/// </summary>
[Table("FishSpecies")]
public class FishSpecies
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int FishSpeciesId { get; set; }

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
    [DisplayName("개체수")] public double? IndividualCount { get; set; }

    //조사자료
    [Description("ID")]
    public int FishId { get; set; }

    [ForeignKey(nameof(FishId))]
    public virtual Fish Fish { get; set; } = null!;

    [NotMapped]
    [DisplayName("오류내용")] public string ErrorContent { get; set; } = string.Empty;

    //일괄입력시 종목록과 매치
    [NotMapped]
    [DisplayName("번호")] public int MacthNumber { get; set; }
}