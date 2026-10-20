using System.IO;
using SDSM_App.Commons;
using SDSM_Models;

namespace SDSM_App.Helpers;

/// <summary>
/// SiteDivision 관련 공용 헬퍼
/// </summary>
public static class SiteDivisionHelper
{
    #region 지점 목록(sites.json) 기준 매칭 — 2026-10-21 (연계시험 문제 9)

    private static SiteCatalog? _catalog;
    private static string? _catalogPath;
    private static DateTime _catalogStamp;
    private static readonly object _catalogLock = new();

    /// <summary>
    /// 일괄등록 지점 → SiteDivisionId.
    /// ① 실행 파일 옆 sites.json 에서 대분류·지점명(·연도 구간)으로 지점을 찾아
    ///    사업장·과업대표하천·지점그룹으로 SiteDivision 을 정한다.
    /// ② 목록에 없는 지점(생태현황 직접 입력 등)만 종전 키워드 방식(<see cref="FindSiteDivisionIdBySiteContains"/>).
    /// </summary>
    public static int? FindSiteDivisionId(bool? isEcology, string? site, string? yearChsu)
        => FindSiteDivisionIdByCatalog(isEcology, site, yearChsu) ?? FindSiteDivisionIdBySiteContains(isEcology, site);

    /// <summary>sites.json 으로만 찾는다. 목록에 없거나 대응하는 SiteDivision 이 없으면 null.</summary>
    public static int? FindSiteDivisionIdByCatalog(bool? isEcology, string? site, string? yearChsu)
    {
        if (isEcology == null || string.IsNullOrWhiteSpace(site)) return null;

        var catalog = GetCatalog();
        if (catalog.Sites.Count == 0) return null;

        var project = isEcology == true ? "생태현황" : "방류하천";
        var name = Norm(site);
        int? year = ParseYear(yearChsu);

        // 같은 지점명이 연도 구간별로 여러 건일 수 있다(진위천1 등) — 조사연도에 맞는 것
        var found = catalog.Sites.FirstOrDefault(s => s.Project == project && Norm(s.SiteName) == name && s.IsValidFor(year));
        if (found == null) return null;

        return FindSiteDivisionIdBySurveySite(found);
    }

    /// <summary>
    /// 지점 목록 1건 → SiteDivisionId. 생태현황은 하나뿐이다.
    /// 방류하천은 사업장·과업대표하천이 같은 것 중 지점그룹으로 고른다:
    /// 이름이 같은 것 → 지점그룹이 지점명을 포함하는 것(오산천 ⊃ 오산) → 과업대표하천 자체 그룹(공수리 → 곡교천).
    /// </summary>
    public static int? FindSiteDivisionIdBySurveySite(SurveySite site)
    {
        IEnumerable<SiteDivision> siteDivisions = GB.ProjectSiteNames;

        if (site.Project == "생태현황")
            return siteDivisions.FirstOrDefault(sd => sd.Project == "생태현황")?.SiteDivisionId;

        if (site.Project != "방류하천") return null;

        var candidates = siteDivisions
            .Where(sd => sd.Project == "방류하천" && Norm(sd.Workplace) == Norm(site.Workplace) && Norm(sd.WorkplaceRiver) == Norm(site.River))
            .Where(sd => !string.IsNullOrWhiteSpace(sd.WorkplaceSite))
            .ToList();

        var group = Norm(site.SiteGroup);
        var river = Norm(site.River);

        return (candidates.FirstOrDefault(sd => Norm(sd.WorkplaceSite) == group)
                ?? candidates.FirstOrDefault(sd => group.Length > 0 && group.Contains(Norm(sd.WorkplaceSite)))
                ?? candidates.FirstOrDefault(sd => river.Length > 0 && river.Contains(Norm(sd.WorkplaceSite))))
               ?.SiteDivisionId;
    }

    /// <summary>실행 폴더의 sites.json(파일이 바뀌면 다시 읽음). 없으면 빈 목록.</summary>
    public static SiteCatalog GetCatalog()
    {
        lock (_catalogLock)
        {
            var path = SiteCatalogStore.MasterPath;
            var stamp = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;

            if (_catalog == null || _catalogPath != path || _catalogStamp != stamp)
            {
                _catalog = SiteCatalogStore.Load(path);
                _catalogPath = path;
                _catalogStamp = stamp;
            }
            return _catalog;
        }
    }

    private static string Norm(string? value) => (value ?? string.Empty).Replace(" ", "").ToUpper();

    private static int? ParseYear(string? yearChsu)
    {
        var v = (yearChsu ?? string.Empty).Replace(" ", "");
        return v.Length >= 4 && int.TryParse(v[..4], out int y) ? y : null;
    }

    #endregion

    /// <summary>
    /// 전달된 'site' 문자열에 siteDivisions 목록의 각 WorkplaceSite 값이 포함되는지 검사하여
    /// 처음 매칭되는 SiteDivisionId를 반환합니다. 매칭이 없으면 null 반환.
    /// (대소문자 무시, WorkplaceSite가 null/빈값이면 건너뜀)
    /// </summary>
    public static int? FindSiteDivisionIdBySiteContains(bool? isEcology, string? site)
    {
        IEnumerable<SiteDivision> siteDivisions = GB.ProjectSiteNames;

        if (!siteDivisions.Any()) return null;

        if (isEcology == true)
        {
            return siteDivisions.Where(sd => sd.Project == "생태현황").FirstOrDefault()?.SiteDivisionId;
        }
        else if (isEcology == false)
        {
            if (string.IsNullOrWhiteSpace(site)) return null;

            var normalizedSite = site.Replace(" ", "").ToUpper();

            foreach (var sd in siteDivisions)
            {
                var ws = sd.WorkplaceSite;
                if (string.IsNullOrWhiteSpace(ws)) continue;

                if (normalizedSite.Contains(ws, StringComparison.OrdinalIgnoreCase))
                    return sd.SiteDivisionId;
            }
        }
        return null;
    }

    /// <summary>
    /// 파일명에 '생태현황' 단어가 포함되면 true, '방류하천' 단어가 포함되면 false,
    /// 둘 다 없거나 둘 다 포함되면 null을 반환합니다.
    /// 반환 튜플: (bool? IsEcology, string Message)
    /// (대소문자 구분 없이 검사하며 기본적으로 공백을 제거하고 검사)
    /// </summary>
    public static (bool? IsEcology, string Message) CheckFileNameByKeywords(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return (null, "파일명이 비어 있습니다.");

        // 기본 정규화: 공백 제거. 필요하면 추가 정규화(밑줄, 하이픈 등) 적용 가능.
        var normalized = fileName.Replace(" ", "");

        bool hasEcology = normalized.Contains("생태현황", StringComparison.OrdinalIgnoreCase);
        bool hasFlowRiver = normalized.Contains("방류하천", StringComparison.OrdinalIgnoreCase);

        if (hasEcology && !hasFlowRiver)
            return (true, "파일명에 '생태현황'이 포함되어 있습니다.");
        if (hasFlowRiver && !hasEcology)
            return (false, "파일명에 '방류하천'이 포함되어 있습니다.");
        if (!hasEcology && !hasFlowRiver)
            return (null, "파일명에 '생태현황' 또는 '방류하천'이 포함되어 있지 않습니다.");

        // 둘 다 포함된 경우
        return (null, "파일명에 '생태현황'과 '방류하천'이 둘 다 포함되어 있습니다.");
    }
}
