using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 식생도
/// </summary>
[Table("Vegetation")]
public class Vegetation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int VegetationId { get; set; }

    [DisplayName("연도")][Required] public int SurveyYear { get; set; }
    [DisplayName("연도차수")][Required] public string YearChsu { get; set; } = string.Empty;
    [DisplayName("지점명")][Required] public string Site { get; set; } = string.Empty;
    [DisplayName("조사일")] public DateTime? SurveyDate { get; set; }  //필수
    [DisplayName("대권역명")] public string? MajorRegion { get; set; }
    [DisplayName("중권역명")] public string? MiddleRegion { get; set; }
    [DisplayName("하천명")][Required] public string River { get; set; } = string.Empty;
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("군락명")][Required] public string Colony { get; set; } = string.Empty;
    //면적 소수점 4자리
    [DisplayName("군락 면적")] public double? Area { get; set; }

    [NotMapped][DisplayName("오류내용")] public string ErrorContent { get; set; } = string.Empty;
}
