using System.IO;
using System.Globalization;
using System.Text;
using SDSM_App;
using SDSM_Models;

// ─────────────────────────────────────────────────────────────────────────────
// 계산 회귀 대조 하네스 (05_REFACTORING §2-C · R2)
//
// 관리자 `GB` 의 계산 결과를 **고정 케이스**로 전부 덤프한다.
// R2 에서 GB 내부를 SDSM_Core 호출로 갈아끼우기 **전후**로 각각 돌려 CSV 를 비교하면,
// 값이 한 건이라도 달라졌는지 바로 드러난다.
//
//   dotnet run --project tools\CalcParity -- --out before.csv     (교체 전)
//   dotnet run --project tools\CalcParity -- --out after.csv      (교체 후)
//   python tools\calc_parity_diff.py before.csv after.csv
//
// ⚠ 실데이터 보호(§0-3) : DB·AppData 를 읽지도 쓰지도 않는다. 순수 계산만 호출한다.
// ⚠ 케이스를 바꾸면 이전 덤프와 비교가 끊긴다. 늘릴 때는 **기존 케이스를 두고 추가**한다.
// ─────────────────────────────────────────────────────────────────────────────

var outPath = "calc_parity.csv";
for (int i = 0; i < args.Length - 1; i++)
    if (args[i] is "--out" or "-o") outPath = Path.GetFullPath(args[i + 1]);

Console.OutputEncoding = Encoding.UTF8;
var sb = new StringBuilder();
sb.AppendLine("case,input,result");
int n = 0;

void Row(string kind, string input, string result)
{
    sb.Append(kind).Append(',').Append(Quote(input)).Append(',').Append(Quote(result)).Append('\n');
    n++;
}

static string Quote(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
static string D(double? v) => v?.ToString("R", CultureInfo.InvariantCulture) ?? "null";
static string I(int? v) => v?.ToString(CultureInfo.InvariantCulture) ?? "null";
static string S(string? v) => v ?? "null";

// ── ① 수질 등급 8종 : 경계값 위·아래·정확히 ───────────────────────────────────
// 등급 경계마다 '바로 아래 / 정확히 / 바로 위' 세 점을 넣는다.
double[] waterProbe =
[
    -1, 0, 0.001, 0.01, 0.019, 0.02, 0.021, 0.04, 0.041, 0.1, 0.101, 0.2, 0.201, 0.3, 0.5, 0.501,
    0.999, 1, 1.001, 1.5, 1.999, 2, 2.001, 2.5, 2.999, 3, 3.001, 3.5, 3.999, 4, 4.001, 4.5,
    4.999, 5, 5.001, 5.5, 5.999, 6, 6.001, 6.5, 6.501, 6.999, 7, 7.001, 7.5, 7.501, 7.999,
    8, 8.001, 8.5, 8.501, 8.999, 9, 9.001, 9.5, 10, 10.001, 11, 12, 15, 25, 50, 100, 200,
    499, 500, 501, 1000, 1001, 5000, 10000, 100000
];
foreach (var v in waterProbe)
{
    Row("pH", D(v), I(GB.GetPhGrade(v)));
    Row("BOD", D(v), S(GB.GetBodGrade(v)));
    Row("COD", D(v), S(GB.GetCodGrade(v)));
    Row("TOC", D(v), S(GB.GetTocGrade(v)));
    Row("SS", D(v), I(GB.GetSsGrade(v)));
    Row("DO", D(v), S(GB.GetDoGrade(v)));
    Row("TP", D(v), S(GB.GetTpGrade(v)));
    Row("EColi", D(v), S(GB.GetEColiGrade(v)));
}
foreach (var name in new[] { "pH", "BOD", "COD", "TOC", "SS", "DO", "TP", "EColi" })
    Row(name, "null", name switch
    {
        "pH" => I(GB.GetPhGrade(null)),
        "SS" => I(GB.GetSsGrade(null)),
        "BOD" => S(GB.GetBodGrade(null)),
        "COD" => S(GB.GetCodGrade(null)),
        "TOC" => S(GB.GetTocGrade(null)),
        "DO" => S(GB.GetDoGrade(null)),
        "TP" => S(GB.GetTpGrade(null)),
        _ => S(GB.GetEColiGrade(null)),
    });

// ── ② RankScorer : 개체수 배열 × 값 ──────────────────────────────────────────
int[][] rankSets =
[
    [],
    [0],
    [5],
    [1, 2, 3],
    [10, 10, 10],
    [0, 0, 5, 10],
    [1, 5, 10, 50, 100],
    [100, 50, 10, 5, 1],
    [3, 3, 7, 7, 9, 9, 9],
    [1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
];
foreach (var set in rankSets)
    foreach (var v in new[] { 0, 1, 2, 3, 5, 7, 9, 10, 50, 100, 1000 })
        Row("RankScorer", $"[{string.Join(' ', set)}]|{v}", GB.GetRankScorer(set, v).ToString());

// ── ③ 저서동물 다양도 지수 · BMI ────────────────────────────────────────────
var benthosSets = BenthosCases();
foreach (var (label, data) in benthosSets)
{
    Row("DI", label, D(GB.GetDI(data)));
    Row("H", label, D(GB.GetH(data)));
    Row("R1", label, D(GB.GetR1(data)));
    Row("J", label, D(GB.GetJ(data)));

    foreach (var reason in new string?[] { null, "", "접근불가", "건 천 화", "준설", "공사중", "기타사유" })
    {
        var (score, grade) = GB.GetBMI(data, reason);
        Row("BMI", $"{label}|{reason ?? "null"}", $"{D(score)}|{S(grade)}");
    }
}
Row("BMI", "null자료|null", $"{D(GB.GetBMI(null!, null).Item1)}|{S(GB.GetBMI(null!, null).Item2)}");

// ── ④ 어류 FAI : 하천차수 × 군집 조합 × 비정상종 × 조사불가 ─────────────────
foreach (var (label, data) in FishCases())
    foreach (var chasu in new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 })
        foreach (var abnormal in new[] { 0, 1, 7, 50 })
        {
            var (cluster, score, grade) = GB.GetFAI(data, null, abnormal, chasu);
            Row("FAI", $"{label}|차수{chasu}|비정상{abnormal}",
                $"{D(score)}|{S(grade)}|" + Cluster(cluster));
        }

foreach (var reason in new[] { "접근불가", "건 천 화", "준설", "공사중", "기타사유" })
{
    var (cluster, score, grade) = GB.GetFAI(FishCases()[1].Data, reason, 3, 3);
    Row("FAI", $"조사불가|{reason}", $"{D(score)}|{S(grade)}|" + Cluster(cluster));
}
{
    var (cluster, score, grade) = GB.GetFAI(null!, null, 0, 3);
    Row("FAI", "null자료", $"{D(score)}|{S(grade)}|" + Cluster(cluster));
}

File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));
Console.WriteLine($"케이스 {n:N0}건 → {outPath}");
return 0;

static string Cluster(object? c)
{
    if (c is null) return "cluster:null";
    var t = c.GetType();
    string G(string p) => t.GetProperty(p)?.GetValue(c)?.ToString() ?? "null";
    return $"cluster:{G("TotalIndividualCount")}/{G("TotalSpeciesCount")}/{G("M1Value")}/{G("M2Value")}"
         + $"/{G("M3Value")}/{G("M4Value")}/{G("M5Value")}/{G("M6Value")}/{G("M7Value")}";
}

// 저서동물 케이스 — 오탁치·지표가중치·순위점수·개체수의 조합
static List<(string Label, List<ImportBenthosSpecies> Data)> BenthosCases()
{
    var cases = new List<(string, List<ImportBenthosSpecies>)>();

    cases.Add(("빈목록", []));
    cases.Add(("전부0", [B("가", 0, "1.0", "2", 0), B("나", 0, "2.0", "3", 0)]));
    cases.Add(("단일종", [B("가", 100, "1.5", "5", 3)]));
    cases.Add(("2종균등", [B("가", 50, "1.0", "2", 2), B("나", 50, "3.0", "4", 2)]));
    cases.Add(("2종편중", [B("가", 990, "0.5", "1", 5), B("나", 10, "3.5", "5", 1)]));
    cases.Add(("결측섞임", [B("가", 30, "1.0", "2", 2), B("나", null, "2.0", "3", 1), B("다", 0, "2.5", "4", 0)]));
    cases.Add(("오탁치0", [B("가", 20, "0", "2", 2), B("나", 30, "0", "3", 3)]));
    cases.Add(("가중치0", [B("가", 20, "2.0", "0", 2), B("나", 30, "3.0", "0", 3)]));
    cases.Add(("순위0", [B("가", 20, "2.0", "2", 0), B("나", 30, "3.0", "3", 0)]));
    cases.Add(("오탁치4상한", [B("가", 10, "4.0", "5", 5), B("나", 10, "4.0", "5", 5)]));
    cases.Add(("오탁치범위밖", [B("가", 10, "9.0", "5", 5), B("나", 10, "8.0", "5", 5)]));
    cases.Add(("파싱실패", [B("가", 10, "숫자아님", "가중치아님", 3)]));
    cases.Add(("다종", [
        B("가", 250, "3.5", "1", 5), B("나", 30, "2.0", "3", 3), B("다", 5000, "3.8", "1", 5),
        B("라", 12, "1.2", "5", 2), B("마", 0, "2.0", "3", 0), B("바", null, "1.0", "4", 0)]));

    return cases.Select(c => (c.Item1, c.Item2)).ToList();
}

static ImportBenthosSpecies B(string ko, double? count, string sap, string weight, int rank) => new()
{
    SpeciesKo = ko,
    IndividualCount = count,
    SaprobicValue = sap,
    IndicatorWeight = weight,
    RankScorer = rank
};

// 어류 케이스 — 길드·외래종·개체수 조합
static List<(string Label, List<ImportFishSpecies> Data)> FishCases()
{
    var cases = new List<(string, List<ImportFishSpecies>)>
    {
        ("빈목록", []),
        ("전부0", [F("가", 0), F("나", 0)]),
        ("단일종", [F("가", 100)]),
        ("국내5종", [F("가", 120), F("나", 15), F("다", 40), F("라", 8), F("마", 3)]),
        ("외래섞임", [F("가", 120, exotic: "O"), F("나", 15), F("다", 40, exotic: "O"), F("라", 8)]),
        ("여울성", [F("가", 30, habitat: "RB"), F("나", 20, habitat: "RB"), F("다", 10)]),
        ("민감종", [F("가", 30, tolerance: "SS"), F("나", 20, tolerance: "SS"), F("다", 10)]),
        ("내성종", [F("가", 80, tolerance: "TS"), F("나", 20)]),
        ("잡식종", [F("가", 90, feeding: "O"), F("나", 10)]),
        ("충식종", [F("가", 70, feeding: "I"), F("나", 30)]),
        ("충식외래", [F("가", 70, feeding: "I", exotic: "O"), F("나", 30, feeding: "I")]),
        ("결측섞임", [F("가", 50), F("나", null), F("다", 0), F("라", 25)]),
        ("복합", [
            F("피라미", 120, tolerance: "TS", feeding: "O"),
            F("붕어", 15, tolerance: "TS", feeding: "O"),
            F("참갈겨니", 40, habitat: "RB", tolerance: "SS", feeding: "I"),
            F("밀어", 9999, habitat: "RB"),
            F("돌고기", 0),
            F("버들치", null),
            F("블루길", 6, exotic: "O", feeding: "I")]),
        ("소량100", [F("가", 60, feeding: "O"), F("나", 20), F("다", 15, habitat: "RB"), F("라", 5)]),
        ("전외래", [F("가", 50, exotic: "O"), F("나", 30, exotic: "O"), F("다", 20, exotic: "O")]),
        ("길드전체", [
            F("가", 10, habitat: "RB", tolerance: "SS", feeding: "I"),
            F("나", 10, habitat: "RB", tolerance: "TS", feeding: "O"),
            F("다", 10, tolerance: "SS", feeding: "I", exotic: "O"),
            F("라", 10, tolerance: "TS", feeding: "O", exotic: "O"),
            F("마", 10)]),
        ("대량", [F("가", 100000), F("나", 50000, tolerance: "TS"), F("다", 1, tolerance: "SS")]),
    };
    return cases.Select(c => (c.Item1, c.Item2)).ToList();
}

static ImportFishSpecies F(string ko, int? count, string? exotic = null,
                           string? habitat = null, string? tolerance = null, string? feeding = null) => new()
{
    SpeciesKo = ko,
    IndividualCount = count,
    Exotic = exotic,
    HabitatGuild = habitat,
    ToleranceGuild = tolerance,
    FeedingGuild = feeding
};
