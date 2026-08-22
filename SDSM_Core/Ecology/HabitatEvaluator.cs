namespace SDSM_Core.Ecology;

/// <summary>
/// 서식·수변환경 평가점수/등급 산정. 관리자 GetGrade 로직 이식.
/// 평가항목 10개 합계 ÷ 2 → 점수, 접근불가면 등급 "-".
/// </summary>
public static class HabitatEvaluator
{
    public static (double? score, string? grade) Evaluate(
        IEnumerable<double?> items, string? surveyUnavailableReason)
    {
        var d = EvaluateDetail(items, surveyUnavailableReason);
        return (d.Score, d.Grade);
    }

    /// <summary>
    /// 평가점수를 산출하면서 **항목별 점수와 합계까지 노출**한다(보고서 엑셀의 계산과정 수록용).
    /// 계산식은 <see cref="Evaluate"/>와 하나의 구현을 공유한다.
    /// </summary>
    public static HriResult EvaluateDetail(
        IEnumerable<double?> items, string? surveyUnavailableReason)
    {
        var arr = items.ToArray();

        // 판정 기준은 **합계가 아니라 선택 여부**다(2026-08-22 수정 · docs\_excel_formula_audit.md §5-4).
        //   종전 `total != 0` 은 10개 항목을 전부 0점으로 고른 경우까지 미입력으로 뭉갰다.
        //   콘크리트로 직강화된 도시하천이 실제로 그렇게 나오는데, 0점·E등급이어야 할 지점이 `-`(미평가)로 빠졌다.
        //   → 가장 훼손된 하천이 평가를 못 받는 구조. 원본 엑셀(`SUM=0` → 0점 → E)과도 어긋났다.
        //   FAI 0종(`12_CALC_FIX` = `0·E`)과 같은 결함이며 같은 방향으로 고친다.
        // 전부 미선택은 그대로 null 로 둔다 — 엑셀은 빈칸과 0점을 구분 못 하지만 우리는 구분한다.
        bool anySelected = arr.Any(x => x.HasValue);
        double total = arr.Where(x => x.HasValue).Sum(x => x!.Value);

        double? score = anySelected
            ? Math.Round(total / 2, 1, MidpointRounding.AwayFromZero)
            : null;

        var unavailable = (surveyUnavailableReason ?? string.Empty).Replace(" ", "");
        string? grade;
        if (unavailable.Contains("접근불가"))
        {
            grade = "-";
        }
        else
        {
            grade =
                score >= 80 ? "A" :
                score >= 60 ? "B" :
                score >= 40 ? "C" :
                score >= 20 ? "D" :
                score >= 0 ? "E" : null;
        }

        return new HriResult { Items = arr, Total = total, Score = score, Grade = grade };
    }
}

/// <summary>서식·수변환경(HRI) 상세 결과(계산과정 포함). 보고서 엑셀에 그대로 수록한다.</summary>
public sealed class HriResult
{
    /// <summary>평가항목 1~10 점수(좌/우안 항목은 평균). 미선택은 null.</summary>
    public double?[] Items { get; set; } = Array.Empty<double?>();

    /// <summary>항목 합계.</summary>
    public double Total { get; set; }

    /// <summary>평가점수 = 합계 ÷ 2.</summary>
    public double? Score { get; set; }

    /// <summary>평가등급 A80·B60·C40·D20·E. 접근불가 시 "-".</summary>
    public string? Grade { get; set; }
}
