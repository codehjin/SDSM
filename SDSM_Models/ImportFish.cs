using System.ComponentModel;

namespace SDSM_Models;

public class ImportFish
{
    [Description("오류내용")]
    public string ErrorContent { get; set; } = string.Empty;

    [DisplayName("연도차수")] public string YearChsu { get; set; } = string.Empty;
    [DisplayName("지점명")] public string Site { get; set; } = string.Empty;
    [DisplayName("조사일")] public string? SurveyDate { get; set; }  
    [DisplayName("대권역명")] public string? MajorRegion { get; set; }
    [DisplayName("중권역명")] public string? MiddleRegion { get; set; }
    [DisplayName("하천명")] public string? River { get; set; }
    [DisplayName("하천유형")] public string? RiverType { get; set; }
    [DisplayName("위도")] public string? Lat { get; set; }
    [DisplayName("경도")] public string? Lng { get; set; }
    [DisplayName("날씨")] public string? Weather { get; set; }
    [DisplayName("조사기관")] public string? SurveyAgency { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("채집\n소요시간(분)")] public string? CollectionTime { get; set; }
    [DisplayName("채집도구")] public string? CollectionTool { get; set; }
    [DisplayName("흐름상태")] public string? CollectionFlowState { get; set; }
    [DisplayName("하천차수")] public string? AutoRiverChasu { get; set; }
    [DisplayName("유역형태")] public string? WatershedType { get; set; }
    [DisplayName("수계형태")] public string? DrainageSystemType { get; set; }
    [DisplayName("하폭(m)")] public string? RiverWidth { get; set; }
    [DisplayName("유폭(m)")] public string? YuWidth { get; set; }
    [DisplayName("수심(m)")] public string? Depth { get; set; }
    [DisplayName("침수성")] public string? Submerged { get; set; }
    [DisplayName("부수성")] public string? Floating { get; set; }
    [DisplayName("정수성")] public string? Emergent { get; set; }
    [DisplayName("나지")] public string? LeftVacantLand { get; set; }
    [DisplayName("초본류")] public string? LeftH { get; set; }
    [DisplayName("관목류")] public string? LeftS { get; set; }
    [DisplayName("교목류")] public string? LeftT1 { get; set; }
    [DisplayName("나지")] public string? RightVacantLand { get; set; }
    [DisplayName("초본류")] public string? RightH { get; set; }
    [DisplayName("관목류")] public string? RightS { get; set; }
    [DisplayName("교목류")] public string? RightT1 { get; set; }
    [DisplayName("좌안")] public string? LeveeLeft { get; set; }
    [DisplayName("우안")] public string? LeveeRight { get; set; }
    [DisplayName("교란요인")] public string? Confounder { get; set; }
    [DisplayName("하천내\n 구조물")] public string? Structure { get; set; }
    [DisplayName("어도 유무")] public string? Fishway { get; set; }
    [DisplayName("기타 특이사항")] public string? EtcNote { get; set; }
    [DisplayName("암반")] public string? Bedrock { get; set; }
    [DisplayName("콘크리트")] public string? Concrete { get; set; }
    [DisplayName("진흙이하\n(<0.063mm)")] public string? Mud { get; set; }
    [DisplayName("모래\n(0.063-2mm)")] public string? Sand { get; set; }
    [DisplayName("잔자갈\n(2-16mm)")] public string? FineGravel { get; set; }
    [DisplayName("자갈\n(16-64mm)")] public string? Gravel { get; set; }
    [DisplayName("작은돌\n(64-256mm)")] public string? SmallStone { get; set; }
    [DisplayName("큰돌\n(>256mm)")] public string? BigStone { get; set; }
    [DisplayName("하천형태")] public string? HabitatRiverType { get; set; }
    [DisplayName("흐름상태")] public string? HabitatFlowState { get; set; }
    [DisplayName("특이사항")] public string? SurveyUnavailableReason { get; set; }
    [DisplayName("비고")] public string? Note { get; set; }
    [DisplayName("기형(deformity)\n DE 종명(개체수)")] public string? DECount { get; set; }
    [DisplayName("지느러미손상(erosion of fin)\n EF 종명(개체수)")] public string? EFCount { get; set; }
    [DisplayName("피부손상(lesions)\n LE 종명(개체수)")] public string? LECount { get; set; }
    [DisplayName("종양(tumors)\n TU 종명(개체수)")] public string? TUCount { get; set; }
    //자동계산
    [DisplayName("총 개체수")] public int? TotalIndividualCount { get; set; }
    //자동계산
    [DisplayName("총 종수")] public int? TotalSpeciesCount { get; set; }
    //자동계산
    [DisplayName("국내종\n 총 종수")] public int? M1Value { get; set; }
    //자동계산
    [DisplayName("어울성\n 저서 종수")] public int? M2Value { get; set; }
    //자동계산
    [DisplayName("민감 종수")] public int? M3Value { get; set; }
    //자동계산
    [DisplayName("내성종\n 개체수 비율(%)")] public double? M4Value { get; set; }
    //자동계산
    [DisplayName("잡식종\n 개체수 비율(%)")] public double? M5Value { get; set; }
    //자동계산
    [DisplayName("충식종\n 개체수 비율(%)")] public double? M6Value { get; set; }
    //자동계산
    [DisplayName("국내종\n 총 개체수")] public int? M7Value { get; set; }    
    //자동계산
    [DisplayName("FAI 점수")] public double? FAIScore { get; set; }
    //자동계산
    [DisplayName("FAI 등급")] public string? FAIGrade { get; set; }

    [DisplayName("종수")] public int SpeciesCount { get; set; }
    [DisplayName("개체수")] public int IndividualCount { get; set; }

    //일괄입력시 종목록과 매치
    [DisplayName("번호")] public int MacthNumber { get; set; }
}

