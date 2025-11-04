using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 식생단면환경
/// </summary>
[Table("VegetationENV")]
public class VegetationENV
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int VegetationENVId { get; set; }

    [DisplayName("연도")][Required] public int SurveyYear { get; set; }
    [DisplayName("차수")][Required] public string Chsu { get; set; } = string.Empty; // 빈값없음
    [DisplayName("조사일")] public DateTime? SurveyDate { get; set; }  //필수
    [DisplayName("날씨")] public string? Weather { get; set; } // 빈값없음
    [DisplayName("조사지점")][Required] public string Site { get; set; } = string.Empty; // 빈값없음
    [DisplayName("하천명")][Required] public string River { get; set; } = string.Empty; // 빈값없음
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("시점_위도")] public double? StartLat { get; set; } // 소수점 6자리
    [DisplayName("시점_경도")] public double? StartLng { get; set; }// 소수점 6자리
    [DisplayName("시점_행정구역명")] public string? StartAddress { get; set; }
    [DisplayName("종점_위도")] public double? EndLat { get; set; }// 소수점 6자리
    [DisplayName("종점_경도")] public double? EndLng { get; set; }// 소수점 6자리
    [DisplayName("시점_각도")] public double? StartAzimuth { get; set; }
    //Riverside Land
    [DisplayName("제외지_하천구조")] public string? RLSubstrateStructure  { get; set; }
    [DisplayName("제외지_교란")] public string? RLDisturbance { get; set; }
    //Interior Land
    [DisplayName("제내지_좌안_토지이용")] public string? ILLeftLandUse { get; set; }
    [DisplayName("제내지_우안_토지이용")] public string? ILRightLandUse { get; set; }
    [DisplayName("거리(m)")] public double? Distance { get; set; }// 소수점 1자리
    [DisplayName("경사(°)")] public double? Slope { get; set; }// 소수점 1자리
    [DisplayName("수심(cm)")] public double? DepthWater { get; set; }// 소수점 1자리
    [DisplayName("하상재질")] public string? HashingMaterial { get; set; }
    [DisplayName("군집명")] public string? Cluster { get; set; }
    [DisplayName("T1_높이(m)")] public double? T1Height { get; set; }// 소수점 1자리
    [DisplayName("T1_피도(%)")] public double? T1CoverDegree { get; set; }// 소수점 1자리
    [DisplayName("T2_높이(m)")] public double? T2Height { get; set; }// 소수점 1자리
    [DisplayName("T2_피도(%)")] public double? T2CoverDegree { get; set; }// 소수점 1자리
    [DisplayName("S_높이(m)")] public double? SHeight { get; set; }// 소수점 1자리
    [DisplayName("S_피도(%)")] public double? SCoverDegree { get; set; }// 소수점 1자리
    [DisplayName("H_높이(m)")] public double? HHeight { get; set; }// 소수점 1자리
    [DisplayName("H_피도(%)")] public double? HCoverDegree { get; set; }// 소수점 1자리
    [NotMapped][DisplayName("종수")] public int SpeciesCount { get; set; }
    [NotMapped][DisplayName("개체수")] public int IndividualCount { get; set; }

    public ICollection<VegetationENVSpecies> Subs { get; set; } = [];

    [NotMapped][DisplayName("오류내용")] public string ErrorContent { get; set; } = string.Empty;

    //일괄입력시 종목록과 매치
    [NotMapped][DisplayName("ID")] public string? ID { get; set; }
}
