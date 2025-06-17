using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 어류종
/// Benthic Macroinvertebrate
/// </summary>

[Table("FishSpecies")]
public class FishSpecies
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int FishSpeciesId { get; set; }

    [DisplayName("종 번호")]
    [Description("종 번호")]
    public int SpeciesNumber { get; set; }

    [DisplayName("목")]
    [Description("목")]
    public string? OrderKo { get; set; }

    [DisplayName("과")]
    [Description("과")]
    public string? FamilyKo { get; set; }

    [DisplayName("국명")]
    [Description("국명")]
    public string? SpeciesKo { get; set; }

    [DisplayName("학명")]
    [Description("학명")]
    public string? SpeciesEn { get; set; }
}
