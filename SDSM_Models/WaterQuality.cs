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

    [DisplayName("연도")][Required] public int SurveyYear { get; set; }
    [DisplayName("연도차수")][Required] public string YearChsu { get; set; } = string.Empty; // 빈값없음
    [DisplayName("지점명")][Required] public string Site { get; set; } = string.Empty;// 빈값없음
    [DisplayName("조사일")] public DateTime? SurveyDate { get; set; }  //필수
    [DisplayName("대권역명")] public string? MajorRegion { get; set; }
    [DisplayName("중권역명")] public string? MiddleRegion { get; set; }
    [DisplayName("하천명")] public string? River { get; set; }// 빈값없음
    [DisplayName("하천유형")] public string? RiverType { get; set; }// 빈값없음
    [DisplayName("위도")] public double? Lat { get; set; }// 소수점 6자리
    [DisplayName("경도")] public double? Lng { get; set; }// 소수점 6자리
    [DisplayName("날씨")] public string? Weather { get; set; }// 빈값없음
    [DisplayName("조사기관")] public string? SurveyAgency { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("pH")] public double? PH { get; set; }// 소수점 1자리
    [DisplayName("BOD(mg/L)")] public double? BOD { get; set; }// 소수점 2자리
    [DisplayName("COD(mg/L)")] public double? COD { get; set; }// 소수점 2자리
    [DisplayName("TOC(mg/L)")] public double? TOC { get; set; }// 소수점 2자리
    [DisplayName("SS(mg/L)")] public double? SS { get; set; }// 소수점 1자리
    [DisplayName("DO(mg/L)")] public double? DO { get; set; }// 소수점 2자리
    [DisplayName("T-P(mg/L)")] public double? TP { get; set; }// 소수점 3자리
    [DisplayName("생태독성")] public double? Ecotoxicity { get; set; }// 소수점 1자리
    [DisplayName("대장균군(군수/100mL)")] public double? EColi { get; set; }// 소수점 1자리

    [DisplayName("T-N(mg/L) ")] public double? TN { get; set; }// 소수점 2자리
    [DisplayName("전기전도도")] public double? EC { get; set; }// 소수점 1자리
    [DisplayName("염소이온(mg/L)")] public double? Cl { get; set; }// 소수점 1자리
    [DisplayName("황이온(mg/L)")] public double? SO42 { get; set; }// 소수점 1자리
    [DisplayName("구리(mg/L)")] public double? Cu { get; set; }// 소수점 3자리
    [DisplayName("아연(mg/L)")] public double? Zn { get; set; }// 소수점 3자리
    [DisplayName("크롬(mg/L)")] public double? Cr { get; set; }// 소수점 3자리
    [DisplayName("탁도(NTU)")] public double? Turbidity { get; set; }// 소수점 1자리   
    [DisplayName("클로로필a(mg/m3)")] public double? Chla { get; set; }// 소수점 1자리   
    [DisplayName("수온")] public double? WaterTemperature { get; set; }// 소수점 1자리   
    [DisplayName("수심(cm)")] public double? WaterDepth { get; set; } // 소수점 1자리   
    [DisplayName("유속(cm/s)")] public double? FlowVelocity { get; set; } // 소수점 2자리   
    [DisplayName("초당유량(m3/sec)")] public double? FlowSec { get; set; }// 소수점 3자리   
    [DisplayName("일당유량(m3/day)")] public double? FlowDay { get; set; }// 소수점 1자리   

    [DisplayName("pH 등급")] public int? PHGrade { get; set; }   
    [DisplayName("BOD 등급")] public string? BODGrade { get; set; }   
    [DisplayName("COD 등급")] public string? CODGrade { get; set; }   
    [DisplayName("TOC 등급")] public string? TOCGrade { get; set; }   
    [DisplayName("SS 등급")] public int? SSGrade { get; set; }   
    [DisplayName("DO 등급")] public string? DOGrade { get; set; }   
    [DisplayName("T-P 등급")] public string? TPGrade { get; set; }   
    [DisplayName("대장균군 등급")] public string? EColiGrade { get; set; }

    //프로젝트 사업장 지점명과 매칭
    [Description("ID")] public int? SiteDivisionId { get; set; }

    [ForeignKey(nameof(SiteDivisionId))]
    public virtual SiteDivision SiteDivision { get; set; } = null!;

    [NotMapped][DisplayName("오류내용")] public string ErrorContent { get; set; } = string.Empty;
}
