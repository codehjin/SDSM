using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 어류
/// </summary>
[Table("Fish")]
public class Fish
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("ID")]
    public int FishId { get; set; }

    [DisplayName("연도")][Required] public int SurveyYear { get; set; }
    [DisplayName("연도차수")][Required] public string YearChsu { get; set; } = string.Empty; // 빈값없음
    [DisplayName("지점명")][Required] public string Site { get; set; } = string.Empty; // 빈값없음
    [DisplayName("조사일")][Required] public DateTime? SurveyDate { get; set; }    // 필수
    [DisplayName("대권역명")] public string? MajorRegion { get; set; } 
    [DisplayName("중권역명")] public string? MiddleRegion { get; set; }
    [DisplayName("하천명")][Required] public string River { get; set; } = string.Empty; // 빈값없음
    [DisplayName("하천유형")] public string? RiverType { get; set; }// 빈값없음
    [DisplayName("위도")] public double? Lat { get; set; } //소수점 6자리
    [DisplayName("경도")] public double? Lng { get; set; }    //소수점 6자리
    [DisplayName("날씨")] public string? Weather { get; set; } // 빈값없음
    [DisplayName("조사기관")] public string? SurveyAgency { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("채집소요시간(분)")] public int? CollectionTime { get; set; }
    [DisplayName("채집도구")] public string? CollectionTool { get; set; }// 빈값없음
    [DisplayName("흐름상태")] public string? CollectionFlowState { get; set; }// 빈값없음
    [DisplayName("하천차수")] public int? AutoRiverChasu { get; set; }
    [DisplayName("유역형태")] public string? WatershedType { get; set; }
    [DisplayName("수계형태")] public string? DrainageSystemType { get; set; }
    [DisplayName("하폭(m)")] public string? RiverWidth { get; set; }
    [DisplayName("유폭(m)")] public string? YuWidth { get; set; }
    [DisplayName("수심(m)")] public string? Depth { get; set; }
    [DisplayName("침수성")] public string? Submerged { get; set; }
    [DisplayName("부수성")] public string? Floating { get; set; }
    [DisplayName("정수성")] public string? Emergent { get; set; }
    [DisplayName("나지")] public int? LeftVacantLand { get; set; }
    [DisplayName("초본류")] public int? LeftH { get; set; }
    [DisplayName("관목류")] public int? LeftS { get; set; }
    [DisplayName("교목류")] public int? LeftT1 { get; set; }
    [DisplayName("나지")] public int? RightVacantLand { get; set; }
    [DisplayName("초본류")] public int? RightH { get; set; }
    [DisplayName("관목류")] public int? RightS { get; set; }
    [DisplayName("교목류")] public int? RightT1 { get; set; }
    [DisplayName("좌안")] public string? LeveeLeft { get; set; }
    [DisplayName("우안")] public string? LeveeRight { get; set; }
    [DisplayName("교란요인")] public string? Confounder { get; set; }
    [DisplayName("하천내 구조물")] public string? Structure { get; set; }
    [DisplayName("어도 유무")] public string? Fishway { get; set; }
    [DisplayName("기타 특이사항")] public string? EtcNote { get; set; }
    [DisplayName("암반")] public int? Bedrock { get; set; }
    [DisplayName("콘크리트")] public int? Concrete { get; set; }
    [DisplayName("진흙이하(<0.063mm)")] public int? Mud { get; set; }
    [DisplayName("모래(0.063-2mm)")] public int? Sand { get; set; }
    [DisplayName("잔자갈(2-16mm)")] public int? FineGravel { get; set; }
    [DisplayName("자갈(16-64mm)")] public int? Gravel { get; set; }
    [DisplayName("작은돌(64-256mm)")] public int? SmallStone { get; set; }
    [DisplayName("큰돌(>256mm)")] public int? BigStone { get; set; }
    [DisplayName("하천형태")] public string? HabitatRiverType { get; set; }// 빈값없음
    [DisplayName("흐름상태")] public string? HabitatFlowState { get; set; }// 빈값없음
    [DisplayName("특이사항")] public string? SurveyUnavailableReason { get; set; }// 빈값없음
    [DisplayName("비고")] public string? Note { get; set; }
    [DisplayName("기형(deformity) DE 종명(개체수)")] public int? DECount { get; set; }
    [DisplayName("지느러미손상(erosion of fin) EF 종명(개체수)")] public int? EFCount { get; set; }    
    [DisplayName("피부손상(lesions) LE 종명(개체수)")] public int? LECount { get; set; }
    [DisplayName("종양(tumors) TU 종명(개체수)")] public int? TUCount { get; set; }
    [DisplayName("총 개체수")] public int? TotalIndividualCount { get; set; }
    [DisplayName("총 종수")] public int? TotalSpeciesCount { get; set; }
    [DisplayName("국내종 총 종수")] public int? M1Value { get; set; }
    [DisplayName("어울성 저서 종수")] public int? M2Value { get; set; }
    [DisplayName("민감 종수")] public int? M3Value { get; set; }
    [DisplayName("내성종 개체수 비율(%)")] public double? M4Value { get; set; } //소수점 1자리
    [DisplayName("잡식종 개체수 비율(%)")] public double? M5Value { get; set; } //소수점 1자리
    [DisplayName("충식종 개체수 비율(%)")] public double? M6Value { get; set; } //소수점 1자리
    [DisplayName("국내종 총 개체수")] public int? M7Value { get; set; }
    [DisplayName("FAI 점수")] public double? FAIScore { get; set; } //소수점 1자리
    [DisplayName("FAI 등급")] public string? FAIGrade { get; set; } 
    [NotMapped][DisplayName("종수")] public int SpeciesCount { get; set; }
    [NotMapped][DisplayName("개체수")] public int IndividualCount { get; set; }

    public ICollection<FishSpecies> Subs { get; set; } = [];

    [NotMapped][DisplayName("오류내용")] public string ErrorContent { get; set; } = string.Empty;

    //일괄입력시 종목록과 매치
    [NotMapped][DisplayName("번호")] public int MacthNumber { get; set; }
}
