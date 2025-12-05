using System.ComponentModel;

namespace SDSM_Models;

public class ImportVegetationENV
{
    [Description("오류내용")]
    public string ErrorContent { get; set; } = string.Empty;

    [DisplayName("연도")] public string? SurveyYear { get; set; }
    [DisplayName("차수")] public string? Chsu { get; set; }
    [DisplayName("조사일")] public string? SurveyDate { get; set; }
    [DisplayName("날씨")] public string? Weather { get; set; }
    [DisplayName("지점명")] public string? Site { get; set; }
    [DisplayName("하천명")] public string? River { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("시점 위도")] public string? StartLat { get; set; }
    [DisplayName("시점 경도")] public string? StartLng { get; set; }
    [DisplayName("시점\n 행정구역명")] public string? StartAddress { get; set; }
    [DisplayName("종점 위도")] public string? EndLat { get; set; }
    [DisplayName("종점 경도")] public string? EndLng { get; set; }
    [DisplayName("시점 각도")] public string? StartAzimuth { get; set; }
    //Riverside Land
    [DisplayName("제외지\n 하천구조")] public string? RLSubstrateStructure { get; set; }
    [DisplayName("제외지 교란")] public string? RLDisturbance { get; set; }
    //Interior Land
    [DisplayName("제내지 좌안\n 토지이용")] public string? ILLeftLandUse { get; set; }
    [DisplayName("제내지 우안\n 토지이용")] public string? ILRightLandUse { get; set; }
    [DisplayName("거리(m)")] public string? Distance { get; set; }
    [DisplayName("경사(°)")] public string? Slope { get; set; }
    [DisplayName("수심(cm)")] public string? DepthWater { get; set; }
    [DisplayName("하상재질")] public string? HashingMaterial { get; set; }
    [DisplayName("군집명")] public string? Cluster { get; set; }
    [DisplayName("T1_높이(m)")] public string? T1Height { get; set; }
    [DisplayName("T1_피도(%)")] public string? T1CoverDegree { get; set; }
    [DisplayName("T2_높이(m)")] public string? T2Height { get; set; }
    [DisplayName("T2_피도(%)")] public string? T2CoverDegree { get; set; }
    [DisplayName("S_높이(m)")] public string? SHeight { get; set; }
    [DisplayName("S_피도(%)")] public string? SCoverDegree { get; set; }
    [DisplayName("H_높이(m)")] public string? HHeight { get; set; }
    [DisplayName("H_피도(%)")] public string? HCoverDegree { get; set; }
    [DisplayName("종수")] public int SpeciesCount { get; set; }
    [DisplayName("개체수")] public int IndividualCount { get; set; }

    //일괄입력시 종목록과 매치
    [DisplayName("ID")] public string? ID { get; set; }
}
