using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SDSM_App.Enums;
using SDSM_App.Views.Windows;

namespace SDSM_App.Helpers;

/// <summary>DB에 이미 있는 같은 기록과 그 처리 방법(새 기록·묶음 하나에 하나).</summary>
public sealed record DuplicateDecision(DuplicateAction Action, IReadOnlyList<int> ExistingIds);

/// <summary>일괄 저장 결과 — 새로 저장·바꿔 저장·건너뜀·(같은 기록이 있는데) 추가 건수.</summary>
public sealed record BulkSaveResult(bool IsSuccess, int Saved, int Replaced, int Skipped, int DuplicateAdded)
{
    public static BulkSaveResult Fail { get; } = new(false, 0, 0, 0, 0);
}

/// <summary>
/// 관찰형(조류·포유류·양서파충류) 같은 묶음 기준(정규화 값) — 2026-10-21 N4 · 2026-10-20 (3) 결정 1
/// - 방류하천 : 대분류·지점·연도차수·조사일 — 하천 빼기(같은 지점의 하천 표기가 기록마다 다르다: 원천리천 지점의 '황구지천' 등)
/// - 생태현황 : 대분류·지점·하천·연도차수·조사일 — 종전처럼 하천 포함(지점 칸이 비거나 넓은 구역이라 하천 칸이 위치를 가른다)
/// 대분류는 파일 하나가 한 대분류(파일명 생태현황/방류하천)라 키에 넣지 않고, DB 쪽을 같은 대분류 행
/// (SiteDivisionId → SiteDivision.Project, GB.ProjectSiteNames)만 골라 비교한다.
/// </summary>
public readonly record struct ObservationGroupKey(string YearChsu, string Site, string River, DateTime? SurveyDay)
{
    /// <summary>isEcology = 파일 대분류(생태현황 true). 방류하천이면 하천 칸을 빈 값으로 둔다.</summary>
    public static ObservationGroupKey Of(bool? isEcology, string? yearChsu, string? site, string? river, DateTime? surveyDate) =>
        new(DuplicateImportHelper.Norm(yearChsu), DuplicateImportHelper.Norm(site),
            isEcology == true ? DuplicateImportHelper.Norm(river) : string.Empty, DuplicateImportHelper.Day(surveyDate));
}

/// <summary>
/// 일괄등록 중복 검사 공용 처리(2026-10-21 연계시험 문제 2·3).
/// 같은 기록의 기준은 분류군 VM 이 정한다 — 대분류·지점·연도차수·조사일(2026-10-21 N4):
/// 어류·저서·수질·서식수변 = 기록 1건 / 조류·포유류·양서파충류 = 같은 값의 행 묶음(생태현황은 하천도 포함).
/// </summary>
public static class DuplicateImportHelper
{
    /// <summary>비교용 정규화 — 공백 제거·대문자(지점·하천·연도차수는 코드성 값으로 본다). null 은 빈 문자열.</summary>
    public static string Norm(string? value) => (value ?? string.Empty).Replace(" ", "").ToUpper();

    /// <summary>조사일 비교용 — 날짜만(시각 무시).</summary>
    public static DateTime? Day(DateTime? value) => value?.Date;

    public static string DateText(DateTime? value) => value?.ToString("yyyy-MM-dd") ?? "(조사일 없음)";

    public static string IdText(IReadOnlyList<int> ids)
    {
        var sorted = ids.OrderBy(i => i).ToList();
        if (sorted.Count == 0) return string.Empty;
        if (sorted.Count <= 6) return "ID " + string.Join(", ", sorted);
        return $"ID {sorted[0]} ~ {sorted[^1]}";
    }

    /// <summary>하천 표기 목록(빈 값은 '-') — 같은 기록 창 표시용.</summary>
    public static string RiverText(IEnumerable<string?> rivers)
    {
        var list = rivers.Select(r => string.IsNullOrWhiteSpace(r) ? "-" : r.Trim()).Distinct().OrderBy(r => r).ToList();
        return list.Count == 0 ? "-" : string.Join(", ", list);
    }

    /// <summary>같은 기록 창 표시용 지점 — 비면 '-'.</summary>
    public static string SiteText(string? site) => string.IsNullOrWhiteSpace(site) ? "-" : site;

    /// <summary>
    /// 같은 묶음인데 하천 표기가 다르면 '기존 하천: 황구지천 · 새 파일: 원천리천', 같으면 null.
    /// 방류하천은 하천이 같은 기록 기준이 아니므로 알리기만 한다(게시 DB 하천 값은 고치지 않는다) · 2026-10-21 N4
    /// (생태현황은 하천이 기준에 들어 있어 같은 묶음이면 하천도 같다 — 늘 null)
    /// </summary>
    public static string? RiverDifferenceNote(IEnumerable<string?> existingRivers, IEnumerable<string?> newRivers)
    {
        var existing = existingRivers.Select(Norm).ToHashSet();
        var incoming = newRivers.Select(Norm).ToHashSet();
        if (existing.SetEquals(incoming)) return null;

        return $"기존 하천: {RiverText(existingRivers)} · 새 파일: {RiverText(newRivers)}";
    }

    /// <summary>
    /// 같은 기록 처리 묻기. '나머지도 같게'를 고른 뒤에는 묻지 않고 그 처리를 돌려준다.
    /// 창을 닫으면 건너뛰기(기본). note 는 창에 따로 보이는 알림(하천 표기 차이 등).
    /// </summary>
    public static DuplicateAction Ask(string header, string newText, string existingText, ref DuplicateAction? applyAll, string? note = null)
    {
        if (applyAll is DuplicateAction fixedAction)
            return fixedAction;

        var window = App.Current.Services.GetRequiredService<DuplicateRecordWindow>();
        var owner = Application.Current.MainWindow;
        if (owner != null && owner.IsVisible && !ReferenceEquals(owner, window))
            window.Owner = owner;
        else
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        window.SetContent(header, newText, existingText, note);
        window.ShowDialog();

        var action = window.Result;
        if (window.ApplyToRest)
            applyAll = action;

        return action;
    }

    /// <summary>저장 결과 요약 — 같은 기록이 있었을 때만 띄운다.</summary>
    public static void ShowSummary(string taxon, int saved, int replaced, int skipped, int duplicateAdded)
    {
        var message = $"{taxon} 일괄 저장 결과{Environment.NewLine}{Environment.NewLine}" +
                      $"새로 저장 : {saved:N0}건{Environment.NewLine}" +
                      $"바꿔 저장 : {replaced:N0}건{Environment.NewLine}" +
                      $"건너뜀 : {skipped:N0}건{Environment.NewLine}" +
                      $"(같은 기록이 있는데 추가 : {duplicateAdded:N0}건)";

        MessageBox.Show(message, "같은 기록 처리 결과", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// 저장 알림(토스트) 문구 — 2026-10-21 N5.
    /// 바꾸거나 건너뛴 것이 없으면 종전 문구, 저장·바꾼 것이 0건이면 '저장한 자료가 없습니다(모두 건너뜀)',
    /// 그 밖은 '저장 n · 바꿈 n · 건너뜀 n건'(저장 = 새로 저장 + 같은 기록이 있는데 추가).
    /// </summary>
    public static (string, bool) SaveToast(BulkSaveResult result)
    {
        if (result.Replaced == 0 && result.Skipped == 0)
            return (GB.SaveMessage, true);

        if (result.Saved + result.Replaced == 0)
            return ("저장한 자료가 없습니다(모두 건너뜀)", true);

        return ($"저장 {result.Saved:N0} · 바꿈 {result.Replaced:N0} · 건너뜀 {result.Skipped:N0}건", true);
    }

    /// <summary>파일명만 같은 경우 — 정보로만 알린다(내용 중복은 저장할 때 검사한다).</summary>
    public static string SavedFileNameInfo(string fileName, DateTime savedTime) =>
        $"같은 이름의 파일을 저장한 기록이 있습니다({savedTime:yyyy-MM-dd HH:mm}). 같은 기록인지는 저장할 때 내용으로 확인합니다.";
}
