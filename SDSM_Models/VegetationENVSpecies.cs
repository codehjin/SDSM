using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 식생단면군락
/// </summary>
[Table("VegetationENVSpecies")]
public class VegetationENVSpecies
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int VegetationENVSpeciesId { get; set; }

    [DisplayName("계층")] public string Layer { get; set; } = string.Empty;// 빈값없음
    [DisplayName("종명")] public string SpeciesKo { get; set; } = string.Empty;
    [DisplayName("피도")] public double? CoverDegree { get; set; }// 소수점 1자리

    //식생단면환경
    [DisplayName("ID")]
    [Required]
    public int VegetationENVId { get; set; }

    [ForeignKey(nameof(VegetationENVId))]
    public virtual VegetationENV VegetationENV { get; set; } = null!;

    [NotMapped][DisplayName("오류내용")] public string ErrorContent { get; set; } = string.Empty;

    //일괄입력시 종목록과 매치
    [NotMapped][DisplayName("ID")] public string? ID { get; set; }
}
