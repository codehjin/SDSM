using System.ComponentModel;

namespace SDSM_Models;

public class ImportBenthos
{
    [Description("오류내용")]
    public string ErrorContent { get; set; } = string.Empty;

    [DisplayName("연도차수")] public string? YearChsu { get; set; }
    [DisplayName("지점명")] public string? Site { get; set; }
    [DisplayName("조사일\n(2010-01-01~오늘)")] public string? SurveyDate { get; set; }
    [DisplayName("대권역명")] public string? MajorRegion { get; set; }
    [DisplayName("중권역명")] public string? MiddleRegion { get; set; }
    [DisplayName("하천명")] public string? River { get; set; }
    [DisplayName("하천유형")] public string? RiverType { get; set; }
    [DisplayName("위도")] public string? Lat { get; set; }
    [DisplayName("경도")] public string? Lng { get; set; }
    [DisplayName("날씨")] public string? Weather { get; set; }
    [DisplayName("조사기관")] public string? SurveyAgency { get; set; }
    [DisplayName("조사자")] public string? Surveyor { get; set; }
    [DisplayName("Surber net(30*30)")] public string? Surbernet30 { get; set; }
    [DisplayName("Surber net(50*50)")] public string? Surbernet50 { get; set; }
    [DisplayName("드렛지")] public string? Dredge { get; set; }
    [DisplayName("에크만")] public string? Ekman { get; set; }
    [DisplayName("유역이용")] public string? Watershed { get; set; }
    [DisplayName("확인 가능 오염원")] public string? PollutionSource { get; set; }
    [DisplayName("식생 수피도")] public string? CamopyCover { get; set; }
    [DisplayName("범람원의 이용")] public string? Floodplain { get; set; }
    [DisplayName("제방(좌안)")] public string? LeveeLeft { get; set; }
    [DisplayName("제방(우안)")] public string? LeveeRight { get; set; }
    [DisplayName("암반")] public string? Bedrock { get; set; }
    [DisplayName("콘크리트")] public string? Concrete { get; set; }
    [DisplayName("진흙이하(<0.063mm)")] public string? Mud { get; set; }
    [DisplayName("모래(0.063-2mm)")] public string? Sand { get; set; }
    [DisplayName("잔자갈(2-16mm)")] public string? FineGravel { get; set; }
    [DisplayName("자갈(16-64mm)")] public string? Gravel { get; set; }
    [DisplayName("작은돌(64-256mm)")] public string? SmallStone { get; set; }
    [DisplayName("큰돌(>256mm)")] public string? BigStone { get; set; }
    [DisplayName("하천유형")] public string? HabitatRiverType { get; set; }
    [DisplayName("하폭, 제방과 제방사이 거리(m)")] public string? RiverWidth { get; set; }
    [DisplayName("수폭, 물이 흐르는 하도의 거리(m)")] public string? WaterWidth { get; set; }
    [DisplayName("평균수심, 샘플링 지점 평균(cm)")] public string? AverageDepth { get; set; }
    [DisplayName("평균유속, 샘플링 지점 평균(cm/s)")] public string? AverageVelocity  { get; set; }
    [DisplayName("기온")] public string? Temperature { get; set; }
    [DisplayName("수온")] public string? WaterTemperature { get; set; }
    [DisplayName("흐름상태")] public string? FlowState { get; set; }
    [DisplayName("투명도")] public string? Transparency { get; set; }
    [DisplayName("냄새")] public string? Smell { get; set; }
    [DisplayName("특이사항")] public string? Note { get; set; }
    [DisplayName("채집불가 내용")] public string? SurveyUnavailableReason { get; set; }
    //자동계산
    [DisplayName("우점도(DI)")] public double? IndexDI { get; set; }
    //자동계산
    [DisplayName("다양도(H')")] public double? IndexH { get; set; }
    //자동계산
    [DisplayName("종풍부도(R1)")] public double? IndexR1 { get; set; }
    //자동계산
    [DisplayName("균등도(J')")] public double? IndexJ { get; set; }
    //자동계산
    [DisplayName("BMI 점수")] public double? BMIScore { get; set; }
    //자동계산
    [DisplayName("BMI 등급")] public string? BMIGrade { get; set; }

    [DisplayName("종수")] public int SpeciesCount { get; set; }
    [DisplayName("개체수")] public double IndividualCount { get; set; }

    //일괄입력시 종목록과 매치
    [DisplayName("번호")] public int MacthNumber { get; set; }
}

