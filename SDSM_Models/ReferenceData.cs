namespace SDSM_Models;

/// <summary>
/// 조사자 입력 검증용 기준자료. 관리자 DB(과거 조사자료)에서 산출해 파일로 배포하고,
/// 조사자 앱이 로드하여 개체수 이상치 경고 등에 사용한다.
/// </summary>
public class ReferenceData
{
    /// <summary>기준자료 버전(yyyyMMdd-HHmm).</summary>
    public string Version { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; }

    /// <summary>어류 종별 과거 개체수 범위 (key = 국명).</summary>
    public Dictionary<string, SpeciesRange> Fish { get; set; } = new();

    /// <summary>저서동물 종별 과거 개체수 범위 (key = 국명).</summary>
    public Dictionary<string, SpeciesRange> Benthos { get; set; } = new();
}

/// <summary>종별 과거 개체수 통계.</summary>
public class SpeciesRange
{
    /// <summary>과거 기록 수.</summary>
    public int N { get; set; }

    /// <summary>과거 최소 개체수.</summary>
    public double Min { get; set; }

    /// <summary>과거 최대 개체수.</summary>
    public double Max { get; set; }
}
