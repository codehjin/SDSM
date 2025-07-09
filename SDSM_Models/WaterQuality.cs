using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 수질
/// </summary>
[Table("WaterQuality")]
public class WaterQuality
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int WaterQualityId { get; set; }

    [DisplayName("연도차수")][Required] public string YearChsu { get; set; } = string.Empty;
    [DisplayName("지점명")][Required] public string Site { get; set; } = string.Empty;
    [DisplayName("조사일")] public DateTime? SurveyDate { get; set; }  //필수
    [DisplayName("대권역명")] public string? MajorRegion { get; set; }
    [DisplayName("중권역명")] public string? MiddleRegion { get; set; }
    [DisplayName("하천명")] public string? River { get; set; }
    [DisplayName("하천유형")] public string? RiverType { get; set; }
    [DisplayName("위도")] public double? Lat { get; set; }
    [DisplayName("경도")] public double? Lng { get; set; }
    [DisplayName("날씨")] public string? Weather { get; set; }
    [DisplayName("조사기관")] public string? SurveyAgency { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("pH")] public double? PH { get; set; }
    [DisplayName("BOD")] public double? BOD { get; set; }
    [DisplayName("COD")] public double? COD { get; set; }
    [DisplayName("TOC")] public double? TOC { get; set; }
    [DisplayName("SS")] public double? SS { get; set; }
    [DisplayName("DO")] public double? DO { get; set; }
    [DisplayName("T-P")] public double? TP { get; set; }
    [DisplayName("생태독성")] public double? Ecotoxicity { get; set; }
    [DisplayName("대장균군")] public double? EColi { get; set; }
    [DisplayName("T-N")] public double? TN { get; set; }
    [DisplayName("EC")] public double? EC { get; set; }
    [DisplayName("유량")] public double? Flow { get; set; }
    [DisplayName("Cl-")] public double? Cl { get; set; }
    [DisplayName("SO42-")] public double? SO42 { get; set; }
    [DisplayName("수온")] public double? WaterTemperature { get; set; }
    [DisplayName("Cu")] public double? Cu { get; set; }
    [DisplayName("Zn")] public double? Zn { get; set; }
    [DisplayName("Cr")] public double? Cr { get; set; }
    [DisplayName("온도")] public double? Temperature { get; set; }
    [DisplayName("Chl-a")] public double? Chla { get; set; }
    [DisplayName("전기전도도")] public double? ElectricalConductivity { get; set; }   
    [DisplayName("탁도")] public double? Turbidity { get; set; }   
    [DisplayName("pH 등급")] public int? PHGrade { get; set; }   
    [DisplayName("BOD 등급")] public string? BODGrade { get; set; }   
    [DisplayName("COD 등급")] public string? CODGrade { get; set; }   
    [DisplayName("TOC 등급")] public string? TOCGrade { get; set; }   
    [DisplayName("SS 등급")] public int? SSGrade { get; set; }   
    [DisplayName("DO 등급")] public string? DOGrade { get; set; }   
    [DisplayName("T-P 등급")] public string? TPGrade { get; set; }   
    [DisplayName("대장균군 등급")] public string? EColiGrade { get; set; }   

    [NotMapped][DisplayName("오류내용")] public string ErrorContent { get; set; } = string.Empty;
}
