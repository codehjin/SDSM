using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 식생단면 군락 뷰
/// </summary>
[Table("View_VegetationENVSpecies")]
public class VegetationENVSpeciesView
{
    [DisplayName("ID")] public int VegetationENVId { get; set; }
    [DisplayName("연도")] public int SurveyYear { get; set; }
    [DisplayName("차수")] public string Chsu { get; set; } = string.Empty;
    [DisplayName("조사일")] public DateTime? SurveyDate { get; set; }  //필수
    [DisplayName("날씨")] public string? Weather { get; set; }
    [DisplayName("조사지점")] public string Site { get; set; } = string.Empty;
    [DisplayName("하천명")] public string? River { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("시점_위도")] public double? StartLat { get; set; }
    [DisplayName("시점_경도")] public double? StartLng { get; set; }
    [DisplayName("시점_행정구역명")] public string? StartAddress { get; set; }
    [DisplayName("종점_위도")] public double? EndLat { get; set; }
    [DisplayName("종점_경도")] public double? EndLng { get; set; }
    [DisplayName("시점_각도")] public double? StartAzimuth { get; set; }
    //Riverside Land
    [DisplayName("제외지_하천구조")] public string? RLSubstrateStructure { get; set; }
    [DisplayName("제외지_교란")] public string? RLDisturbance { get; set; }
    //Interior Land
    [DisplayName("제내지_좌안_토지이용")] public string? ILLeftLandUse { get; set; }
    [DisplayName("제내지_우안_토지이용")] public string? ILRightLandUse { get; set; }
    [DisplayName("거리(m)")] public double? Distance { get; set; }
    [DisplayName("경사(°)")] public double? Slope { get; set; }
    [DisplayName("수심(cm)")] public double? DepthWater { get; set; }
    [DisplayName("하상재질")] public string? HashingMaterial { get; set; }
    [DisplayName("군집명")] public string? Cluster { get; set; }
    [DisplayName("T1_높이(m)")] public double? T1Height { get; set; }
    [DisplayName("T1_피도(%)")] public double? T1CoverDegree { get; set; }
    [DisplayName("T2_높이(m)")] public double? T2Height { get; set; }
    [DisplayName("T2_피도(%)")] public double? T2CoverDegree { get; set; }
    [DisplayName("S_높이(m)")] public double? SHeight { get; set; }
    [DisplayName("S_피도(%)")] public double? SCoverDegree { get; set; }
    [DisplayName("H_높이(m)")] public double? HHeight { get; set; }
    [DisplayName("H_피도(%)")] public double? HCoverDegree { get; set; }

    [DisplayName("계층")] public string Layer { get; set; } = string.Empty;
    [DisplayName("종명")] public string SpeciesKo { get; set; } = string.Empty;
    [DisplayName("피도")] public double? CoverDegree { get; set; }

    // SiteDivision에서 가져온 컬럼
    [DisplayName("프로젝트명")] public string? Project { get; set; }
    [DisplayName("사업장")] public string? Workplace { get; set; }
    [DisplayName("과업 대표하천")] public string? WorkplaceRiver { get; set; }
}
