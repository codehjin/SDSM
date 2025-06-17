using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 어류 출현종
/// </summary>
[Table("FishSurveySpecies")]
public class FishSurveySpecies
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int FishSurveySpeciesId { get; set; }

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

    [DisplayName("개체수")]
    public int? NoU { get; set; }

    //기본정보
    [Description("ID")]
    public int FishSurveyId { get; set; }

    [ForeignKey(nameof(FishSurveyId))]
    public virtual FishSurvey FishSurvey { get; set; } = null!;
}