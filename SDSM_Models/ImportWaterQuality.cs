using System.ComponentModel;

namespace SDSM_Models;

public class ImportWaterQuality
{
    [Description("오류내용")]
    public string ErrorContent { get; set; } = string.Empty;

    [DisplayName("연도차수")] public string? YearChsu { get; set; }
    [DisplayName("지점명")] public string? Site { get; set; }
    [DisplayName("조사일)")] public string? SurveyDate { get; set; }
    [DisplayName("대권역명")] public string? MajorRegion { get; set; }
    [DisplayName("중권역명")] public string? MiddleRegion { get; set; }
    [DisplayName("하천명")] public string? River { get; set; }
    [DisplayName("하천유형")] public string? RiverType { get; set; }
    [DisplayName("위도")] public string? Lat { get; set; }
    [DisplayName("경도")] public string? Lng { get; set; }
    [DisplayName("날씨")] public string? Weather { get; set; }
    [DisplayName("조사기관")] public string? SurveyAgency { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("pH")] public string? PH { get; set; }
    [DisplayName("BOD")] public string? BOD { get; set; }
    [DisplayName("COD")] public string? COD { get; set; }
    [DisplayName("TOC")] public string? TOC { get; set; }
    [DisplayName("SS")] public string? SS { get; set; }
    [DisplayName("DO")] public string? DO { get; set; }
    [DisplayName("T-P")] public string? TP { get; set; }
    [DisplayName("생태독성")] public string? Ecotoxicity { get; set; }
    [DisplayName("대장균군")] public string? EColi { get; set; }
    [DisplayName("T-N")] public string? TN { get; set; }
    [DisplayName("EC")] public string? EC { get; set; }
    [DisplayName("유량")] public string? Flow { get; set; }
    [DisplayName("Cl-")] public string? Cl { get; set; }
    [DisplayName("SO42-")] public string? SO42 { get; set; }
    [DisplayName("수온")] public string? WaterTemperature { get; set; }
    [DisplayName("Cu")] public string? Cu { get; set; }
    [DisplayName("Zn")] public string? Zn { get; set; }
    [DisplayName("Cr")] public string? Cr { get; set; }
    [DisplayName("온도")] public string? Temperature { get; set; }
    [DisplayName("Chl-a")] public string? Chla { get; set; }
    [DisplayName("전기전도도")] public string? ElectricalConductivity { get; set; }   
    [DisplayName("탁도")] public string? Turbidity { get; set; }
    //자동계산
    [DisplayName("pH 등급")] public int? PHGrade { get; set; }
    //자동계산
    [DisplayName("BOD 등급")] public string? BODGrade { get; set; }
    //자동계산
    [DisplayName("COD 등급")] public string? CODGrade { get; set; }
    //자동계산
    [DisplayName("TOC 등급")] public string? TOCGrade { get; set; }
    //자동계산
    [DisplayName("SS 등급")] public int? SSGrade { get; set; }
    //자동계산
    [DisplayName("DO 등급")] public string? DOGrade { get; set; }
    //자동계산
    [DisplayName("T-P 등급")] public string? TPGrade { get; set; }
    //자동계산
    [DisplayName("대장균군 등급")] public string? EColiGrade { get; set; }
}
