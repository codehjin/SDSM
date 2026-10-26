using System.Reflection;

namespace SDSM_App.Commons;

/// <summary>
/// 프로그램 정보 한 곳 — 정보 창([프로그램 정보])에 보이는 글은 모두 여기서 고친다(2026-10-22 §21-5).
/// 판(버전)은 SDSM_App.csproj 의 &lt;Version&gt;, 커밋 해시는 빌드 때 SDK 가 제품 버전 뒤에 붙인다(+해시).
/// 빌드 날짜는 csproj 의 AssemblyMetadata("BuildDate").
/// </summary>
public static class AppInfo
{
    public const string ProgramName = "삼성DS 모니터링DB 통합관리 프로그램";

    // 적용 지침
    public const string GuidelineTitle = "「수생태계 현황 조사 및 건강성 평가 방법 등에 관한 지침(하천편)」";
    public const string GuidelineNotice = "국립환경과학원고시 제2026-30호(2026. 7. 27. 개정)";
    /// <summary>앱에 넣은 지침 원본(PDF) — [지침 원본 보기]가 ManualWindow 로 연다.</summary>
    public const string GuidelinePdfUri = "/SDSM_App;component/Assets/Pdfs/guideline_2026-30.pdf";
    public const string GuidelinePdfCaption = "지침 원본 — 국립환경과학원고시 제2026-30호";

    // 종목록 기준(종 수는 정보 창이 DB 에서 센다)
    public const string SpeciesListBasis = "지침 별표 2(저서동물 지표종) · 별표 3(어류)에 맞춰 정비(2026-10-22)";

    // 개발
    public const string Developer = "주식회사 에코앤지오";
    /// <summary>연락처 — 사용자에게 받은 그대로(2026-10-22 §22-4). 정보 창은 ' · ' 마다 줄을 나눠 보인다.</summary>
    public const string Contact = "www.econgeo.com · 032-888-2205 · evicdatalab@gmail.com";

    // 사용 설명서(기존)
    public const string ManualPdfUri = "/SDSM_App;component/Assets/Pdfs/manual.pdf";
    public const string ManualPdfCaption = "사용 설명서";

    private static readonly Assembly Asm = typeof(AppInfo).Assembly;

    private static string Informational =>
        Asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;

    /// <summary>판 — 예: 1.1.0</summary>
    public static string Version => Informational.Split('+')[0] is { Length: > 0 } v ? v : Asm.GetName().Version?.ToString(3) ?? "-";

    /// <summary>제품 버전의 커밋 해시(앞 7자리). 없으면 '-'.</summary>
    public static string Commit
    {
        get
        {
            var parts = Informational.Split('+');
            return parts.Length > 1 && parts[1].Length > 0 ? parts[1][..Math.Min(7, parts[1].Length)] : "-";
        }
    }

    /// <summary>빌드 날짜(yyyy-MM-dd) — 없으면 실행 파일 수정 날짜.</summary>
    public static string BuildDate =>
        Asm.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "BuildDate")?.Value
        ?? (string.IsNullOrEmpty(Asm.Location) ? "-" : System.IO.File.GetLastWriteTime(Asm.Location).ToString("yyyy-MM-dd"));
}
