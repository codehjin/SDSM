using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 저서동물 출현종(개체수 => 실수)
/// </summary>

[Table("BenthosSpecies")]
public class BenthosSpecies
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int BenthosSpeciesId { get; set; }

    [DisplayName("오탁치(s)")] public double? SaprobicValue { get; set; }
    [DisplayName("지표가중치(g)")] public int? IndicatorWeight { get; set; }
    [DisplayName("멸종위기 I급")] public string? Endangered1 { get; set; }
    [DisplayName("멸종위기 II급")] public string? Endangered2 { get; set; }
    [DisplayName("한국고유종")] public string? Endemic { get; set; }
    [DisplayName("문")] public string? PhylumKo { get; set; }
    [DisplayName("강")] public string? ClassKo { get; set; }
    [DisplayName("목")] public string? OrderKo { get; set; }
    [DisplayName("과")] public string? FamilyKo { get; set; }
    [DisplayName("국명")] public string? SpeciesKo { get; set; }
    [DisplayName("학명")] public string? SpeciesEn { get; set; }
    [DisplayName("개체수")] public double? IndividualCount { get; set; }

    //조사자료
    [Description("ID")]
    [Required]
    public int BenthosId { get; set; }

    [ForeignKey(nameof(BenthosId))]
    public virtual Benthos Benthos { get; set; } = null!;

    [NotMapped]
    [DisplayName("오류내용")] public string ErrorContent { get; set; } = string.Empty;

    //일괄입력시 종목록과 매치
    [NotMapped]
    [DisplayName("번호")] public int MacthNumber { get; set; }
}
