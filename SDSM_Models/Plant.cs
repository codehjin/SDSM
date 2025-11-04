using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 식물상
/// </summary>
[Table("Plant")]
public class Plant
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int PlantId { get; set; }

    [DisplayName("연도")][Required] public int SurveyYear { get; set; }
    [DisplayName("차수")][Required] public string Chsu { get; set; } = string.Empty;// 빈값없음
    [DisplayName("조사일")] public DateTime? SurveyDate { get; set; }  //필수
    [DisplayName("날씨")] public string? Weather { get; set; }// 빈값없음
    [DisplayName("조사지점")][Required] public string Site { get; set; } = string.Empty;// 빈값없음
    [DisplayName("하천명")][Required] public string River { get; set; } = string.Empty;// 빈값없음
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("위도")] public double? Lat { get; set; }//소수점 6자리
    [DisplayName("경도")] public double? Lng { get; set; }//소수점 6자리
    [DisplayName("위치")] public string? SurveyLocation { get; set; }
    [DisplayName("종명")][Required] public string SpeciesKo { get; set; } = string.Empty;   
    [DisplayName("학명")] public string? SpeciesEn { get; set; }
    [DisplayName("외래식물구분")] public string? AlienPlant { get; set; }
    [DisplayName("생태교란종")] public string? InvasiveSpecies { get; set; }

    [NotMapped][DisplayName("오류내용")] public string ErrorContent { get; set; } = string.Empty;
}
