using System.Globalization;
using SDSM_Models;

namespace SDSM_App.Helpers;

/// <summary>목록 밖 종 하나 — 확인 창에 보이는 줄(국명 · 학명 · 파일의 지표값 · 행 수)과 목록에 더할 때 쓰는 분류 칸.</summary>
public sealed class OutOfListSpecies
{
    public string? SpeciesKo { get; init; }
    public string? SpeciesEn { get; init; }
    /// <summary>파일에 적힌 지표값 · 길드(보이기만 · 목록에 넣지 않는다).</summary>
    public string FileValues { get; init; } = string.Empty;
    public int Rows { get; set; }
    public string? PhylumKo { get; init; }
    public string? ClassKo { get; init; }
    public string? OrderKo { get; init; }
    public string? FamilyKo { get; init; }
}

/// <summary>일괄등록 한 번의 종 행 점검 결과 — 종목록 값과 다른 종 · 목록 밖 종.</summary>
public sealed class SpeciesValueCheck
{
    /// <summary>파일 값이 종목록 값과 다른 종(국명 학명 → 내용) — 종 하나에 한 줄.</summary>
    public SortedDictionary<string, string> Differences { get; } = new(StringComparer.Ordinal);

    /// <summary>목록 밖 종(대응 뒤 국명 + 학명 기준).</summary>
    public Dictionary<string, OutOfListSpecies> OutOfList { get; } = new(StringComparer.Ordinal);
}

/// <summary>
/// 일괄등록 종 행의 지표값 출처 = 관리자 종목록(지침 · 2026-10-22 §22-2 · §22-3).
/// - 목록에 있는 종(옛 학명 대응 뒤 국명 + 학명): 어류 내성 · 섭식 · 서식지 · 외래 · 어류특성확정 / 저서 오탁치 · 가중치를 종목록 값으로 바꾼다.
///   파일 값과 다르면 <see cref="SpeciesValueCheck.Differences"/> 에 남긴다.
/// - 목록 밖 종: 그 행의 지표값 · 길드를 비운다(지침: 별표 밖 종은 지수 계산에서 제외 · 출현 목록에는 넣는다).
/// 지수(FAI · BMI)는 이 값으로 계산되므로 종 행을 읽은 바로 뒤, 지수 계산 전에 부른다.
/// 비교 키는 기존 GetNewSpeciesData 와 같게 공백(+U+00A0) 제거 · 소문자.
/// </summary>
public static class SpeciesListValues
{
    public static string Key(string? ko, string? en) => $"{Norm(ko)}|{Norm(en)}";

    private static string Norm(string? s) => (s ?? string.Empty).Replace(" ", "").Replace(" ", "").ToLower();

    private static string Code(string? s) => (s ?? string.Empty).Replace(" ", "").ToUpper();

    private static string Show(string? s) => string.IsNullOrWhiteSpace(s) ? "-" : s.Trim();

    public static Dictionary<string, FishSpeciesList> FishIndex(IEnumerable<FishSpeciesList> list)
    {
        var index = new Dictionary<string, FishSpeciesList>(StringComparer.Ordinal);
        foreach (var x in list) index.TryAdd(Key(x.SpeciesKo, x.SpeciesEn), x);
        return index;
    }

    public static Dictionary<string, BenthosSpeciesList> BenthosIndex(IEnumerable<BenthosSpeciesList> list)
    {
        var index = new Dictionary<string, BenthosSpeciesList>(StringComparer.Ordinal);
        foreach (var x in list) index.TryAdd(Key(x.SpeciesKo, x.SpeciesEn), x);
        return index;
    }

    /// <summary>어류 종 행(한 기록) — 목록 값으로 바꾸거나(목록 종) 비운다(목록 밖 종).</summary>
    public static void ApplyFish(IEnumerable<ImportFishSpecies> rows, IReadOnlyDictionary<string, FishSpeciesList> index, SpeciesValueCheck check)
    {
        foreach (var row in rows)
        {
            var key = Key(row.SpeciesKo, row.SpeciesEn);
            var file = $"{Show(row.ToleranceGuild)}/{Show(row.FeedingGuild)}/{Show(row.HabitatGuild)}/{Show(row.Exotic)}";

            if (index.TryGetValue(key, out var listed))
            {
                bool differs = Code(row.ToleranceGuild) != Code(listed.ToleranceGuild) || Code(row.FeedingGuild) != Code(listed.FeedingGuild)
                            || Code(row.HabitatGuild) != Code(listed.HabitatGuild) || Code(row.Exotic) != Code(listed.Exotic);
                if (differs)
                    check.Differences[$"{listed.SpeciesKo} {listed.SpeciesEn}"] =
                        $"파일 {file} → 종목록 {Show(listed.ToleranceGuild)}/{Show(listed.FeedingGuild)}/{Show(listed.HabitatGuild)}/{Show(listed.Exotic)}";

                row.FishTrait = listed.FishTrait;
                row.ToleranceGuild = listed.ToleranceGuild;
                row.FeedingGuild = listed.FeedingGuild;
                row.HabitatGuild = listed.HabitatGuild;
                row.Exotic = listed.Exotic;
            }
            else
            {
                if (!check.OutOfList.TryGetValue(key, out var o))
                {
                    o = new OutOfListSpecies
                    {
                        SpeciesKo = row.SpeciesKo?.Trim(), SpeciesEn = row.SpeciesEn?.Trim(),
                        FileValues = $"내성/섭식/서식지/외래 {file}",
                        ClassKo = row.ClassKo?.Trim(), OrderKo = row.OrderKo?.Trim(), FamilyKo = row.FamilyKo?.Trim(),
                    };
                    check.OutOfList[key] = o;
                }
                o.Rows++;

                row.FishTrait = null;
                row.ToleranceGuild = null;
                row.FeedingGuild = null;
                row.HabitatGuild = null;
                row.Exotic = null;
            }
        }
    }

    /// <summary>저서 종 행(한 기록) — 목록 값으로 바꾸거나(목록 종) 비운다(목록 밖 종).</summary>
    public static void ApplyBenthos(IEnumerable<ImportBenthosSpecies> rows, IReadOnlyDictionary<string, BenthosSpeciesList> index, SpeciesValueCheck check)
    {
        foreach (var row in rows)
        {
            var key = Key(row.SpeciesKo, row.SpeciesEn);
            var fileS = Num(row.SaprobicValue);
            var fileG = Num(row.IndicatorWeight);
            var file = $"{(fileS?.ToString(CultureInfo.InvariantCulture) ?? "-")}/{(fileG?.ToString(CultureInfo.InvariantCulture) ?? "-")}";

            if (index.TryGetValue(key, out var listed))
            {
                bool differs = !Same(fileS, listed.SaprobicValue) || !Same(fileG, listed.IndicatorWeight);
                if (differs)
                    check.Differences[$"{listed.SpeciesKo} {listed.SpeciesEn}"] =
                        $"파일 {file} → 종목록 {(listed.SaprobicValue?.ToString(CultureInfo.InvariantCulture) ?? "-")}/{(listed.IndicatorWeight?.ToString(CultureInfo.InvariantCulture) ?? "-")}";

                row.SaprobicValue = listed.SaprobicValue?.ToString(CultureInfo.InvariantCulture);
                row.IndicatorWeight = listed.IndicatorWeight?.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                if (!check.OutOfList.TryGetValue(key, out var o))
                {
                    o = new OutOfListSpecies
                    {
                        SpeciesKo = row.SpeciesKo?.Trim(), SpeciesEn = row.SpeciesEn?.Trim(),
                        FileValues = $"오탁치/가중치 {file}",
                        PhylumKo = row.PhylumKo?.Trim(), ClassKo = row.ClassKo?.Trim(), OrderKo = row.OrderKo?.Trim(), FamilyKo = row.FamilyKo?.Trim(),
                    };
                    check.OutOfList[key] = o;
                }
                o.Rows++;

                row.SaprobicValue = null;
                row.IndicatorWeight = null;
            }
        }
    }

    /// <summary>목록 밖 종을 종목록에 더할 행 — 지표값 · 길드 없이(출현 목록 · §22-3). 분류 칸(목 · 과)만 파일 값.</summary>
    public static FishSpeciesList NewFishListRow(OutOfListSpecies o) => new()
    {
        FishTrait = "", ToleranceGuild = "", FeedingGuild = "", HabitatGuild = "", Exotic = "",
        Endemic = "", Endangered1 = "", Endangered2 = "", NaturalMonument = "",
        LineageOrder = null, ClassKo = o.ClassKo, OrderKo = o.OrderKo, FamilyKo = o.FamilyKo,
        SpeciesKo = o.SpeciesKo, SpeciesEn = o.SpeciesEn,
    };

    public static BenthosSpeciesList NewBenthosListRow(OutOfListSpecies o) => new()
    {
        SaprobicValue = null, IndicatorWeight = null, Endangered1 = "", Endangered2 = "", Endemic = "",
        PhylumKo = o.PhylumKo, ClassKo = o.ClassKo, OrderKo = o.OrderKo, FamilyKo = o.FamilyKo,
        SpeciesKo = o.SpeciesKo, SpeciesEn = o.SpeciesEn,
    };

    private static double? Num(string? s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d
        : double.TryParse(s, out d) ? d : null;

    private static bool Same(double? a, double? b) => a is null ? b is null : b is not null && Math.Abs(a.Value - b.Value) < 1e-9;
}
