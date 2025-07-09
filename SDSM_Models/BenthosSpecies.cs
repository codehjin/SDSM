using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 저서동물 종목록
/// </summary>

[Table("BenthosSpecies")]
public class BenthosSpecies
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int BenthosSpeciesId { get; set; }

    [DisplayName("문")] public string? PhylumKo { get; set; }
    [DisplayName("강")] public string? ClassKo { get; set; }
    [DisplayName("목")] public string? OrderKo { get; set; }
    [DisplayName("과")] public string? FamilyKo { get; set; }
    [DisplayName("국명")] public string? SpeciesKo { get; set; }
    [DisplayName("학명")] public string? SpeciesEn { get; set; }
    [DisplayName("개체수")] public double? IndividualCount { get; set; }

    //조사자료
    [Required][Description("ID")] public int BenthosId { get; set; }

    [ForeignKey(nameof(BenthosId))]
    public virtual Benthos Benthos { get; set; } = null!;

    [NotMapped][DisplayName("오류내용")] public string ErrorContent { get; set; } = string.Empty;
}
