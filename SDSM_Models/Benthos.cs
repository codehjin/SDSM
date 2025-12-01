using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 저서동물
/// </summary>
[Table("Benthos")]
public class Benthos
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int BenthosId { get; set; }

    [DisplayName("연도")][Required] public int SurveyYear { get; set; }
    [DisplayName("연도차수")][Required] public string YearChsu { get; set; } = string.Empty;//빈값없음
    [DisplayName("지점명")] public string? Site { get; set; }//빈값없음
    [DisplayName("조사일")] public DateTime? SurveyDate { get; set; }  //필수
    [DisplayName("대권역명")] public string? MajorRegion { get; set; }
    [DisplayName("중권역명")] public string? MiddleRegion { get; set; }
    [DisplayName("하천명")] public string? River { get; set; } //빈값없음
    [DisplayName("하천유형")] public string? RiverType { get; set; }//빈값없음
    [DisplayName("위도")] public double? Lat { get; set; }//소수점 6자리
    [DisplayName("경도")] public double? Lng { get; set; }//소수점 6자리
    [DisplayName("날씨")] public string? Weather { get; set; }//빈값없음
    [DisplayName("조사기관")] public string? SurveyAgency { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("Surber net(30*30)")] public int? Surbernet30 { get; set; }
    [DisplayName("Surber net(50*50)")] public int? Surbernet50 { get; set; }
    [DisplayName("드렛지")] public int? Dredge { get; set; }
    [DisplayName("에크만")] public int? Ekman { get; set; }
    [DisplayName("유역이용")] public string? Watershed { get; set; }
    [DisplayName("확인 가능 오염원")] public string? PollutionSource { get; set; }
    [DisplayName("식생 수피도")] public string? CamopyCover { get; set; }//빈값없음
    [DisplayName("범람원의 이용")] public string? Floodplain { get; set; }//빈값없음
    [DisplayName("제방(좌안)")] public string? LeveeLeft { get; set; }
    [DisplayName("제방(우안)")] public string? LeveeRight { get; set; }
    [DisplayName("암반")] public int? Bedrock { get; set; }
    [DisplayName("콘크리트")] public int? Concrete { get; set; }
    [DisplayName("진흙이하(<0.063mm)")] public int? Mud { get; set; }
    [DisplayName("모래(0.063-2mm)")] public int? Sand { get; set; }
    [DisplayName("잔자갈(2-16mm)")] public int? FineGravel { get; set; }
    [DisplayName("자갈(16-64mm)")] public int? Gravel { get; set; }
    [DisplayName("작은돌(64-256mm)")] public int? SmallStone { get; set; }
    [DisplayName("큰돌(>256mm)")] public int? BigStone { get; set; }
    [DisplayName("하천유형")] public string? HabitatRiverType { get; set; }//빈값없음
    [DisplayName("하폭, 제방과 제방사이 거리(m)")] public double? RiverWidth { get; set; }//소수점 1자리
    [DisplayName("수폭, 물이 흐르는 하도의 거리(m)")] public double? WaterWidth { get; set; }//소수점 1자리
    [DisplayName("평균수심, 샘플링 지점 평균(cm)")] public double? AverageDepth { get; set; }//소수점 1자리
    [DisplayName("평균유속, 샘플링 지점 평균(cm/s)")] public double? AverageVelocity  { get; set; }//소수점 1자리
    [DisplayName("기온")] public double? Temperature { get; set; }//소수점 1자리
    [DisplayName("수온")] public double? WaterTemperature { get; set; }//소수점 1자리
    [DisplayName("흐름상태")] public string? FlowState { get; set; }//빈값없음
    [DisplayName("투명도")] public int? Transparency { get; set; }
    [DisplayName("냄새")] public int? Smell { get; set; }
    [DisplayName("특이사항")] public string? Note { get; set; }
    [DisplayName("채집불가시")] public string? SurveyUnavailableReason { get; set; }//빈값없음
    [DisplayName("우점도(DI)")] public double? IndexDI { get; set; }
    [DisplayName("다양도(H')")] public double? IndexH { get; set; }
    [DisplayName("종풍부도(R1)")] public double? IndexR1 { get; set; }
    [DisplayName("균등도(J')")] public double? IndexJ { get; set; }
    [DisplayName("BMI 점수")] public double? BMIScore { get; set; }//소수점 1자리
    [DisplayName("BMI 등급")] public string? BMIGrade { get; set; }
    [NotMapped][DisplayName("종수")] public int SpeciesCount { get; set; }
    [NotMapped][DisplayName("개체수")] public double IndividualCount { get; set; }//소수점 1자리

    //프로젝트 사업장 지점명과 매칭
    [Description("ID")] public int? SiteDivisionId { get; set; }

    [ForeignKey(nameof(SiteDivisionId))]
    public virtual SiteDivision SiteDivision { get; set; } = null!;

    public ICollection<BenthosSpecies> Subs { get; set; } = [];

    [NotMapped][DisplayName("오류내용")] public string ErrorContent { get; set; } = string.Empty;

    //일괄입력시 종목록과 매치
    [NotMapped][DisplayName("번호")] public int MacthNumber { get; set; }
}

