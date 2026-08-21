using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SDSM_Models;

/// <summary>
/// 조사지점 마스터(sites.json). 관리자·조사자 공용.
/// 원천은 제출 엑셀의 `조사지점` 시트이며 조사자 저장소의 `tools\build_sites_json.py` 로 생성한다.
/// 4단계 구분은 관리자 DB `SiteDivision`(Project·Workplace·WorkplaceRiver·WorkplaceSite)과 대응한다.
/// </summary>
public class SiteCatalog
{
    public string Version { get; set; } = string.Empty;   // 예: sites-20260821
    public DateTime GeneratedAt { get; set; }

    public List<SurveySite> Sites { get; set; } = new();
}

/// <summary>조사지점 1건.</summary>
public class SurveySite
{
    /// <summary>대분류 — 방류하천 / 생태현황 (`SiteDivision.Project`).</summary>
    public string? Project { get; set; }

    /// <summary>사업장 — 기흥 / 화성 / 온양 (`SiteDivision.Workplace`).</summary>
    public string? Workplace { get; set; }

    /// <summary>과업대표하천 — 오산천 / 원천리천 / 곡교천 (`SiteDivision.WorkplaceRiver`).</summary>
    public string? River { get; set; }

    /// <summary>지점 그룹 — 곡교천 / 천안천 / 회룡천 (`SiteDivision.WorkplaceSite`).
    /// 한 과업대표하천 안에 여러 지천이 있을 때 구분한다.</summary>
    public string? SiteGroup { get; set; }

    /// <summary>DB상 지점명. 조사자료의 `Site` 값이며 **이 앱의 지점 식별자**다(예: 곡교천1).</summary>
    public string SiteName { get; set; } = string.Empty;

    /// <summary>조사장소 번호(예: St.1). 조사자가 `ST1`로 입력하면 이 값으로 지점을 찾는다.</summary>
    public string? StNo { get; set; }

    /// <summary>지점 설명(예: 배방야구장).</summary>
    public string? Desc { get; set; }

    /// <summary>하천차수(FAI 계산 입력). 원본에 있는 하천만 채워진다.</summary>
    public int? Chasu { get; set; }

    /// <summary>위도(십진). 엑셀에는 DMS와 십진이 섞여 있어 생성 시 십진으로 통일한다.</summary>
    public double? Lat { get; set; }

    /// <summary>경도(십진).</summary>
    public double? Lng { get; set; }

    /// <summary>연도 구간별 보고서 표기 이력(예: "22~25" → "곡교천-1").</summary>
    public Dictionary<string, string> YearAliases { get; set; } = new();

    /// <summary>연도 구간별 보고서 지도 번호(예: "22~26" → "3"). 지점명 이력과 의미가 달라 분리한다.</summary>
    public Dictionary<string, string> MapNumbers { get; set; } = new();

    /// <summary>이 지점이 적용되는 분류군. 비고에 "수질만"이 적힌 지점은 ["수질"] 이다.</summary>
    public List<string> Taxa { get; set; } = new();

    /// <summary>지점 사진. 사진 루트 기준 **상대경로만** 담는다.
    /// ⚠ 이미지 바이트를 DB·JSON에 넣지 않는다. 현장 사진은 장당 2~5MB라 파일이 급격히 커진다.</summary>
    public List<SitePhoto> Photos { get; set; } = new();

    /// <summary>이 지점 정의가 유효한 시작 연도. null이면 제한 없음.
    /// 같은 SiteName이 연도별로 다른 장소를 가리킬 때 구간을 나눈다
    /// (예: 어류 진위천1 — 2023년과 2025년 좌표가 18km 떨어져 있다).</summary>
    public int? ValidFrom { get; set; }

    /// <summary>이 지점 정의가 유효한 끝 연도(포함). null이면 현재까지.</summary>
    public int? ValidTo { get; set; }

    /// <summary>주어진 조사연도에 이 지점 정의가 유효한지. 연도를 모르면(null) 항상 참.</summary>
    public bool IsValidFor(int? year)
    {
        if (year is not int y) return true;
        if (ValidFrom is int from && y < from) return false;
        if (ValidTo is int to && y > to) return false;
        return true;
    }

    /// <summary>유효 연도 구간 표기 — `2023~2024` · `2025~` · `~2024`. 구간이 없으면 빈 문자열.</summary>
    [JsonIgnore]
    public string PeriodText => (ValidFrom, ValidTo) switch
    {
        (null, null) => string.Empty,
        (int f, null) => $"{f}~",
        (null, int t) => $"~{t}",
        (int f, int t) => f == t ? $"{f}" : $"{f}~{t}"
    };

    /// <summary>화면 표시용 — `곡교천1 (St.1)` 형태. 조사자가 번호를 함께 인지하도록.</summary>
    [JsonIgnore]
    public string Display => string.IsNullOrWhiteSpace(StNo) ? SiteName : $"{SiteName} ({StNo})";

    /// <summary>연도 구간이 있는 지점은 구간을 병기한다 — `진위천1 (2023~2024)`.
    /// 같은 SiteName이 여러 건 존재할 수 있으므로 목록에서 구분이 필요하다.</summary>
    [JsonIgnore]
    public string DisplayWithPeriod => PeriodText.Length == 0 ? Display : $"{Display} [{PeriodText}]";

    /// <summary>지점 설명까지 붙인 긴 표시(드롭다운 항목용).</summary>
    [JsonIgnore]
    public string DisplayLong => string.IsNullOrWhiteSpace(Desc) ? Display : $"{Display} · {Desc}";

    public override string ToString() => Display;
}

/// <summary>지점 사진 1장. 파일 자체는 사진 루트 폴더에 두고 여기에는 경로만 남긴다.</summary>
public class SitePhoto
{
    /// <summary>사진 루트 기준 상대경로(예: `생태현황\진위천1\2026_상류.jpg`).</summary>
    public string RelPath { get; set; } = string.Empty;

    /// <summary>설명(예: 상류 전경).</summary>
    public string? Caption { get; set; }

    /// <summary>촬영 연도. 연도별 변화 비교에 쓴다.</summary>
    public int? Year { get; set; }

    [JsonIgnore]
    public string FileName => System.IO.Path.GetFileName(RelPath);

    public override string ToString() =>
        string.IsNullOrWhiteSpace(Caption) ? FileName : $"{FileName} — {Caption}";
}
