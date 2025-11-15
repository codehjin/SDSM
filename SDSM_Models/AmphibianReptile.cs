using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 양서파충류
/// </summary>
[Table("AmphibianReptile")]
public class AmphibianReptile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int AmphibianReptileId { get; set; }

    [DisplayName("연도")][Required] public int SurveyYear { get; set; }
    [DisplayName("연도차수")][Required] public string YearChsu { get; set; } = string.Empty;
    [DisplayName("조사일")] public DateTime? SurveyDate { get; set; } //필수
    [DisplayName("날씨")] public string? Weather { get; set; }
    [DisplayName("하천명")][Required] public string River { get; set; } = string.Empty;
    [DisplayName("지점명")] public string? Site { get; set; }
    [DisplayName("일반명")][Required] public string SpeciesKo { get; set; } = string.Empty;
    [DisplayName("학명")] public string? SpeciesEn { get; set; }
    [DisplayName("특징")] public string? Feature { get; set; }
    [DisplayName("성체")] public int? Trace1 { get; set; }
    [DisplayName("유생")] public int? Trace2 { get; set; }
    [DisplayName("알")] public int? Trace3 { get; set; }
    [DisplayName("울음소리")] public int? Trace4 { get; set; }
    [DisplayName("로드킬")] public int? Trace5 { get; set; }
    [DisplayName("기타")] public int? Trace6 { get; set; }
    [DisplayName("위도")] public double? Lat { get; set; }
    [DisplayName("경도")] public double? Lng { get; set; }
    [DisplayName("대분류")] public string? MajorCategory { get; set; }
    [DisplayName("중분류")] public string? MiddleCategory { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("특이사항")] public string? Note { get; set; }

    [NotMapped][DisplayName("오류내용")] public string ErrorContent { get; set; } = string.Empty;

    [NotMapped][DisplayName("위도_도")] public int? Lat_Do { get; set; }
    [NotMapped][DisplayName("위도_분")] public int? Lat_Bun { get; set; }
    [NotMapped][DisplayName("위도_초")] public double? Lat_Cho { get; set; }
    [NotMapped][DisplayName("경도_도")] public int? Lng_Do { get; set; }
    [NotMapped][DisplayName("경도_분")] public int? Lng_Bun { get; set; }
    [NotMapped][DisplayName("경도_초")] public double? Lng_Cho { get; set; }
}
