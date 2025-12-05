using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 어류 종목록 뷰
/// </summary>
[Table("View_FishSpecies")]
public class FishSpeciesView
{
    [DisplayName("ID")] public int FishId { get; set; }
    [DisplayName("연도")] public int SurveyYear { get; set; }
    [DisplayName("연도차수")] public string YearChsu { get; set; } = string.Empty;
    [DisplayName("지점명")] public string Site { get; set; } = string.Empty;
    [DisplayName("조사일")] public DateTime? SurveyDate { get; set; }
    [DisplayName("대권역명")] public string? MajorRegion { get; set; }
    [DisplayName("중권역명")] public string? MiddleRegion { get; set; }
    [DisplayName("하천명")] public string? River { get; set; }
    [DisplayName("하천유형")] public string? RiverType { get; set; }
    [DisplayName("위도")] public double? Lat { get; set; }
    [DisplayName("경도")] public double? Lng { get; set; }
    [DisplayName("날씨")] public string? Weather { get; set; }
    [DisplayName("조사기관")] public string? SurveyAgency { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("채집\n소요시간(분)")] public int? CollectionTime { get; set; }
    [DisplayName("채집도구")] public string? CollectionTool { get; set; }
    [DisplayName("흐름상태")] public string? CollectionFlowState { get; set; }
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
    [DisplayName("하천내\n 구조물")] public string? Structure { get; set; }
    [DisplayName("어도 유무")] public string? Fishway { get; set; }
    [DisplayName("기타 특이사항")] public string? EtcNote { get; set; }
    [DisplayName("암반")] public int? Bedrock { get; set; }
    [DisplayName("콘크리트")] public int? Concrete { get; set; }
    [DisplayName("진흙이하\n(<0.063mm)")] public int? Mud { get; set; }
    [DisplayName("모래\n(0.063-2mm)")] public int? Sand { get; set; }
    [DisplayName("잔자갈\n(2-16mm)")] public int? FineGravel { get; set; }
    [DisplayName("자갈\n(16-64mm)")] public int? Gravel { get; set; }
    [DisplayName("작은돌\n(64-256mm)")] public int? SmallStone { get; set; }
    [DisplayName("큰돌\n(>256mm)")] public int? BigStone { get; set; }
    [DisplayName("하천형태")] public string? HabitatRiverType { get; set; }
    [DisplayName("흐름상태")] public string? HabitatFlowState { get; set; }
    [DisplayName("특이사항")] public string? SurveyUnavailableReason { get; set; }
    [DisplayName("비고")] public string? Note { get; set; }
    [DisplayName("기형(deformity)\n DE 종명(개체수)")] public int? DECount { get; set; }
    [DisplayName("지느러미손상(erosion of fin)\n EF 종명(개체수)")] public int? EFCount { get; set; }
    [DisplayName("피부손상(lesions)\n LE 종명(개체수)")] public int? LECount { get; set; }
    [DisplayName("종양(tumors)\n TU 종명(개체수)")] public int? TUCount { get; set; }
    [DisplayName("FAI 점수")] public double? FAIScore { get; set; }
    [DisplayName("FAI 등급")] public string? FAIGrade { get; set; }

    [DisplayName("어류특성확정")] public string? FishTrait { get; set; }
    [DisplayName("내성도 길드")] public string? ToleranceGuild { get; set; }
    [DisplayName("섭식 길드")] public string? FeedingGuild { get; set; }
    [DisplayName("서식지 길드")] public string? HabitatGuild { get; set; }
    [DisplayName("외래종")] public string? Exotic { get; set; }
    [DisplayName("고유종")] public string? Endemic { get; set; }
    [DisplayName("멸종위기 I급")] public string? Endangered1 { get; set; }
    [DisplayName("멸종위기 II급")] public string? Endangered2 { get; set; }
    [DisplayName("천연기념물")] public string? NaturalMonument { get; set; }
    [DisplayName("계통별 순서")] public int? LineageOrder { get; set; }
    [DisplayName("강")] public string? ClassKo { get; set; }
    [DisplayName("목")] public string? OrderKo { get; set; }
    [DisplayName("과")] public string? FamilyKo { get; set; }
    [DisplayName("국명")] public string? SpeciesKo { get; set; }
    [DisplayName("학명")] public string? SpeciesEn { get; set; }
    [DisplayName("개체수")] public int? IndividualCount { get; set; }

    // SiteDivision에서 가져온 컬럼
    [DisplayName("프로젝트명")] public string? Project { get; set; }
    [DisplayName("사업장")] public string? Workplace { get; set; }
    [DisplayName("과업\n 대표하천")] public string? WorkplaceRiver { get; set; }
}