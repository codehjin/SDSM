using System.Text.RegularExpressions;
using SDSM_Models;

namespace SDSM_Core.Data;

/// <summary>
/// 조사지점 해석 규칙. 지점 마스터를 어디서 읽든(앱 번들·AppData·관리자 파일) **규칙은 하나**다.
/// (05_REFACTORING R3 — 조사자 `SiteListProvider` 와 기준파일 생성기에 같은 코드가 복제돼 있었다.)
///
/// ⚠ **대분류(Project) 격리** — 절대 완화하지 말 것.
/// 방류하천과 생태현황에 같은 이름의 지점이 있고 실제로는 3.6~11.5km 떨어진 **다른 장소**다
/// (`..\SDSM\docs\_ecostatus_sites_extracted.md` §8-2). 대분류를 넘어 찾으면 자료가 조용히 섞인다.
///   · 대분류가 비면 목록은 **빈 목록**이고 해석도 하지 않는다
///   · 해당 대분류에서 못 찾아도 **다른 대분류로 넘어가지 않는다**
/// </summary>
public static class SiteResolver
{
    /// <summary>`ST1` · `St.1` · `st 1` 을 모두 St 번호로 본다.</summary>
    private static readonly Regex StPattern =
        new(@"^\s*st\.?\s*(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>대분류로 거른 지점. **대분류가 비면 빈 목록**이다.</summary>
    public static List<SurveySite> ByProject(IEnumerable<SurveySite> sites, string? project) =>
        string.IsNullOrWhiteSpace(project)
            ? new List<SurveySite>()
            : sites.Where(s => s.Project == project).ToList();

    /// <summary>
    /// 조사자가 친 값을 지점으로 해석한다. 우선순위:
    /// ① 지점명 정확일치 → ② 조사장소 번호(ST1·St.1·st 1) → ③ 연도별 표기·지도번호.
    /// 종명과 달리 지점명은 코드성 값이라 공백 제거·대문자 비교를 쓴다(CLAUDE.md §7.3).
    /// </summary>
    public static SurveySite? Resolve(IEnumerable<SurveySite> sites, string? input, string? project)
    {
        var q = input?.Trim();
        if (string.IsNullOrEmpty(q)) return null;

        var pool = ByProject(sites, project);
        if (pool.Count == 0) return null;          // 대분류 밖으로 넘어가지 않는다

        var key = Key(q);

        // ① 지점명
        var hit = pool.FirstOrDefault(s => Key(s.SiteName) == key);
        if (hit is not null) return hit;

        // ② 조사장소 번호
        var m = StPattern.Match(q);
        if (m.Success)
        {
            var no = m.Groups[1].Value;
            hit = pool.FirstOrDefault(s => Key(s.StNo) == Key($"St.{no}"));
            if (hit is not null) return hit;

            // St 번호가 없는 하천은 연도별 지도 번호가 그 역할을 한다(오산천 등)
            hit = pool.FirstOrDefault(s => s.MapNumbers.Values.Any(v => v == no));
            if (hit is not null) return hit;
        }

        // ③ 연도별 표기 이력(옛 보고서 지점명으로 찾는 경우)
        return pool.FirstOrDefault(s => s.YearAliases.Values.Any(v => Key(v) == key));
    }

    private static string Key(string? s) => (s ?? string.Empty).Replace(" ", "").ToUpperInvariant();
}
