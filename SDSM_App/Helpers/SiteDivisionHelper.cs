using SDSM_Models;

namespace SDSM_App.Helpers;

/// <summary>
/// SiteDivision 관련 공용 헬퍼
/// </summary>
public static class SiteDivisionHelper
{
    /// <summary>
    /// 전달된 'site' 문자열에 siteDivisions 목록의 각 WorkplaceSite 값이 포함되는지 검사하여
    /// 처음 매칭되는 SiteDivisionId를 반환합니다. 매칭이 없으면 null 반환.
    /// (대소문자 무시, WorkplaceSite가 null/빈값이면 건너뜀)
    /// </summary>
    public static int? FindSiteDivisionIdBySiteContains(bool? isEcology, string? site)
    {
        IEnumerable<SiteDivision> siteDivisions = GB.ProjectSiteNames;

        if (string.IsNullOrWhiteSpace(site) || !siteDivisions.Any()) return null;

        var normalizedSite = site.Replace(" ", "").ToUpper();

        if (isEcology == true)
        {
            return siteDivisions.Where(sd => sd.Project == "생태현황").FirstOrDefault()?.SiteDivisionId;
        }
        else if (isEcology == false)
        {
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
