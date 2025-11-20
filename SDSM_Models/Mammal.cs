using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 포유류 조사
/// </summary>
[Table("Mammal")]
public class Mammal
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int MammalId { get; set; }

    [DisplayName("연도")][Required] public int SurveyYear { get; set; }
    [DisplayName("연도차수")][Required] public string YearChsu { get; set; } = string.Empty;// 빈값없음
    [DisplayName("조사일")] public DateTime? SurveyDate { get; set; }  //필수
    [DisplayName("날씨")] public string? Weather { get; set; }// 빈값없음
    [DisplayName("하천명")][Required] public string River { get; set; } = string.Empty;// 빈값없음
    [DisplayName("지점명")][Required] public string Site { get; set; } = string.Empty;//빈값없음
    [DisplayName("일반명")][Required] public string SpeciesKo { get; set; } = string.Empty;
    [DisplayName("학명")] public string? SpeciesEn { get; set; }
    [DisplayName("특징")] public string? Feature { get; set; }
    [DisplayName("위도")] public double? Lat { get; set; }
    [DisplayName("경도")] public double? Lng { get; set; }
    [DisplayName("관찰지 유형")] public string? ObservationSite { get; set; }
    [DisplayName("포획")] public int? Trace1 { get; set; }
    [DisplayName("관찰")] public int? Trace2 { get; set; }
    [DisplayName("울음")] public int? Trace3 { get; set; }
    [DisplayName("사체")] public int? Trace4 { get; set; }
    [DisplayName("족적")] public int? Trace5 { get; set; }
    [DisplayName("털")] public int? Trace6 { get; set; }
    [DisplayName("식흔")] public int? Trace7 { get; set; }
    [DisplayName("굴")] public int? Trace8 { get; set; }
    [DisplayName("번식지")] public int? Trace9 { get; set; }
    [DisplayName("배설물")] public int? Trace10 { get; set; }
    [DisplayName("카메라")] public int? Trace11 { get; set; }
    [DisplayName("기타(탐문 등)")] public int? Trace12 { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("특이사항")] public string? Note { get; set; }

    //프로젝트 사업장 지점명과 매칭
    [Description("ID")] public int? SiteDivisionId { get; set; }

    [ForeignKey(nameof(SiteDivisionId))]
    public virtual SiteDivision SiteDivision { get; set; } = null!;

    [NotMapped][DisplayName("오류내용")] public string ErrorContent { get; set; } = string.Empty;
    [NotMapped][DisplayName("위도_도")] public int? Lat_Do { get; set; }
    [NotMapped][DisplayName("위도_분")] public int? Lat_Bun { get; set; }
    [NotMapped][DisplayName("위도_초")] public double? Lat_Cho { get; set; }
    [NotMapped][DisplayName("경도_도")] public int? Lng_Do { get; set; }
    [NotMapped][DisplayName("경도_분")] public int? Lng_Bun { get; set; }
    [NotMapped][DisplayName("경도_초")] public double? Lng_Cho { get; set; }
}
