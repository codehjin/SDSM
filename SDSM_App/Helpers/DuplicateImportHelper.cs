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

/// <summary>관찰형(조류·포유류·양서파충류) 같은 묶음 기준 — 연도차수·하천·지점·조사일(정규화 값).</summary>
public readonly record struct ObservationGroupKey(string YearChsu, string River, string Site, DateTime? SurveyDay)
{
    public static ObservationGroupKey Of(string? yearChsu, string? river, string? site, DateTime? surveyDate) =>
        new(DuplicateImportHelper.Norm(yearChsu), DuplicateImportHelper.Norm(river), DuplicateImportHelper.Norm(site), DuplicateImportHelper.Day(surveyDate));
}

/// <summary>
/// 일괄등록 중복 검사 공용 처리(2026-10-21 연계시험 문제 2·3).
/// 같은 기록의 기준은 분류군 VM 이 정한다:
/// 어류·저서·수질·서식수변 = 대분류·지점·연도차수·조사일 / 조류·포유류·양서파충류 = 연도차수·하천·지점·조사일 묶음.
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

    /// <summary>
    /// 같은 기록 처리 묻기. '나머지도 같게'를 고른 뒤에는 묻지 않고 그 처리를 돌려준다.
    /// 창을 닫으면 건너뛰기(기본).
    /// </summary>
    public static DuplicateAction Ask(string header, string newText, string existingText, ref DuplicateAction? applyAll)
    {
        if (applyAll is DuplicateAction fixedAction)
            return fixedAction;

        var window = App.Current.Services.GetRequiredService<DuplicateRecordWindow>();
        var owner = Application.Current.MainWindow;
        if (owner != null && owner.IsVisible && !ReferenceEquals(owner, window))
            window.Owner = owner;
        else
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        window.SetContent(header, newText, existingText);
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

    /// <summary>파일명만 같은 경우 — 정보로만 알린다(내용 중복은 저장할 때 검사한다).</summary>
    public static string SavedFileNameInfo(string fileName, DateTime savedTime) =>
        $"같은 이름의 파일을 저장한 기록이 있습니다({savedTime:yyyy-MM-dd HH:mm}). 같은 기록인지는 저장할 때 내용으로 확인합니다.";
}
