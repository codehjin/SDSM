using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;
using SDSM_Core.Ecology;
using SDSM_Models;

// ─────────────────────────────────────────────────────────────────────────────
// 종목록 ↔ 지침(제2026-30호) 별표 대조 (2026-10-22 · §21)
//
// 관리자 DB 의 FishSpeciesList · BenthosSpeciesList 를 지침 별표 3(어류) · 별표 2(저서 지표종) 추출 CSV 와 비교한다.
//   ① 칸 대조 — 어류: 국명 · 학명 · 내성 · 섭식 · 서식지 · 외래 / 저서: 오탁치 s · 지표가중치 g
//   ② 계산 대조 — SDSM_Core 계산기로 'DB 값'과 '지침 값'을 각각 넣어 FAI(M1~M8 · 점수) · BMI(점수 · 등급)가 같은지
//      (종 하나씩 + 지침 전 종을 한 표본으로)
//
//   dotnet run --project tools\SpeciesListCheck -- --db <SDSMDB.sqlite> --fish <별표3.csv> --benthos <별표2.csv> [--out report.txt]
//
// ⚠ DB 는 읽기 전용(Mode=ReadOnly)으로만 연다.
// ─────────────────────────────────────────────────────────────────────────────

string? db = null, fishCsv = null, benthosCsv = null, outPath = null;
for (int i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--db": db = args[i + 1]; break;
        case "--fish": fishCsv = args[i + 1]; break;
        case "--benthos": benthosCsv = args[i + 1]; break;
        case "--out": outPath = args[i + 1]; break;
    }
}
if (db is null || fishCsv is null || benthosCsv is null)
{
    Console.Error.WriteLine("사용: --db <SDSMDB.sqlite> --fish <별표3.csv> --benthos <별표2.csv> [--out report.txt]");
    return 2;
}

Console.OutputEncoding = Encoding.UTF8;
var rep = new StringBuilder();
void W(string s) { rep.AppendLine(s); Console.WriteLine(s); }

using var conn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = db, Mode = SqliteOpenMode.ReadOnly }.ToString());
conn.Open();

W($"# 종목록 ↔ 지침 별표 대조 · {DateTime.Now:yyyy-MM-dd HH:mm}");
W($"DB : {db}");
int problems = 0;

// ── 어류 ──────────────────────────────────────────────────────────────────
var fishRows = Query(conn, "select SpeciesKo, SpeciesEn, ToleranceGuild, FeedingGuild, HabitatGuild, Exotic, OrderKo, FamilyKo from FishSpeciesList");
var fishGuide = ReadCsv(fishCsv);
W("");
W($"## 어류 — 지침 별표 3 {fishGuide.Count}종 · DB {fishRows.Count}행");
int fishSame = 0, fishCalcSame = 0;
var fishAll = new List<(ImportFishSpecies db, ImportFishSpecies g)>();
foreach (var g in fishGuide)
{
    var ko = g["국명"];
    var m = fishRows.Where(r => r["SpeciesKo"] == ko).ToList();
    if (m.Count != 1) { W($"- 못 찾음/여럿 {g["관리번호"]} {ko} ({m.Count}행)"); problems++; continue; }
    var r = m[0];
    var diffs = new List<string>();
    void Cmp(string name, string? dbv, string? gv) { if ((dbv ?? "") != (gv ?? "")) diffs.Add($"{name} DB '{Show(dbv)}' ↔ 지침 '{Show(gv)}'"); }
    Cmp("학명", r["SpeciesEn"], g["학명"]);
    Cmp("내성", r["ToleranceGuild"], g["내성도(TS/IS/SS)"]);
    Cmp("섭식", r["FeedingGuild"], g["섭식(I/O/H/C)"]);
    Cmp("서식지", r["HabitatGuild"], g["서식지(RB)"]);
    Cmp("외래", r["Exotic"], g["외래종(O)"]);
    if (diffs.Count == 0) fishSame++; else { W($"- 다름 {g["관리번호"]} {ko}: {string.Join(" · ", diffs)}"); problems++; }

    // 계산 대조 — 이 종 하나(개체 10)
    var a = FishOf(r["ToleranceGuild"], r["FeedingGuild"], r["HabitatGuild"], r["Exotic"], 10);
    var b = FishOf(g["내성도(TS/IS/SS)"], g["섭식(I/O/H/C)"], g["서식지(RB)"], g["외래종(O)"], 10);
    if (FaiKey([a]) == FaiKey([b])) fishCalcSame++; else { W($"- FAI 다름 {ko}: DB {FaiKey([a])} ↔ 지침 {FaiKey([b])}"); problems++; }
    int n = fishAll.Count + 1;
    fishAll.Add((FishOf(r["ToleranceGuild"], r["FeedingGuild"], r["HabitatGuild"], r["Exotic"], n % 17 + 1),
                 FishOf(g["내성도(TS/IS/SS)"], g["섭식(I/O/H/C)"], g["서식지(RB)"], g["외래종(O)"], n % 17 + 1)));
}
var fishOnlyDb = fishRows.Where(r => !fishGuide.Any(g => g["국명"] == r["SpeciesKo"])).ToList();
W($"- 칸 일치 {fishSame} / {fishGuide.Count} · 종별 FAI 일치 {fishCalcSame} / {fishGuide.Count} · DB 에만 있는 종 {fishOnlyDb.Count}{(fishOnlyDb.Count > 0 ? " (" + string.Join(", ", fishOnlyDb.Select(r => r["SpeciesKo"])) + ")" : "")}");
foreach (var chasu in new[] { 1, 3, 5 })
{
    var ka = FaiKey(fishAll.Select(x => x.db), chasu); var kb = FaiKey(fishAll.Select(x => x.g), chasu);
    W($"- 전 종 한 표본(차수 {chasu}) FAI : DB {ka} · 지침 {kb} → {(ka == kb ? "같음" : "다름")}");
    if (ka != kb) problems++;
}
// 참고 — 목 · 과(한글) 표기 차이는 계산에 쓰지 않는다
var ordFam = fishGuide.Select(g => (g, r: fishRows.FirstOrDefault(r => r["SpeciesKo"] == g["국명"])))
    .Where(x => x.r != null && (Ko(x.g["목"]) != x.r!["OrderKo"] || Ko(x.g["과"]) != x.r!["FamilyKo"]))
    .Select(x => $"{x.g["국명"]}({x.r!["OrderKo"]}/{x.r!["FamilyKo"]} ↔ {Ko(x.g["목"])}/{Ko(x.g["과"])})").ToList();
W($"- (참고) 목 · 과 한글 표기 다름 {ordFam.Count}종{(ordFam.Count > 0 ? ": " + string.Join(", ", ordFam) : "")}");

// ── 저서 ──────────────────────────────────────────────────────────────────
// 지침의 무리 행 · 깔따구 2행 ↔ 앱 이름 대응(값 같음 · 이름은 앱 그대로 두기로 함 — 근거 §8-2 · §21-2)
var groupMap = new Dictionary<string, string>
{
    ["26"] = "Lymnaeidae sp.", ["68"] = "Glossiphoniidae sp.", ["290"] = "Chloroperlidae sp.", ["329"] = "Elmidae sp.",
    ["362"] = "Dixidae sp.", ["366"] = "Culicidae sp.", ["368"] = "Ceratopogonidae sp.",
    ["369"] = "Chironomidae spp.(non-red type)", ["370"] = "Chironomidae spp. (red type)",
    ["377"] = "Empididae sp.", ["378"] = "Dolichopodidae sp.", ["384"] = "Tabanidae sp.", ["387"] = "Syrphidae sp.",
    ["388"] = "Ephydridae sp.", ["389"] = "Muscidae sp.", ["448"] = "Nymphalinae sp.", ["449"] = "Acentropinae sp.",
};
var bRows = Query(conn, "select BenthosSpeciesListId, SpeciesKo, SpeciesEn, SaprobicValue, IndicatorWeight from BenthosSpeciesList");
var bGuide = ReadCsv(benthosCsv);
W("");
W($"## 저서 — 지침 별표 2 지표종 {bGuide.Count}행 · DB {bRows.Count}행(지표값 {bRows.Count(r => r["SaprobicValue"] != null)})");
int byEn = 0, byKo = 0, byMap = 0, valSame = 0, bCalcSame = 0;
var used = new HashSet<string>();
var koOnly = new List<string>();
var mapUsed = new List<string>();
var bAll = new List<(ImportBenthosSpecies db, ImportBenthosSpecies g)>();
foreach (var g in bGuide)
{
    var no = g["관리번호"]; var en = Norm(g["학명"]); var ko = Norm(g["국명"]);
    Dictionary<string, string?>? r = null;
    var e = bRows.Where(x => Norm(x["SpeciesEn"]) == en && en.Length > 0).ToList();
    if (e.Count == 1) { r = e[0]; byEn++; }
    else if (ko.Length > 0 && ko != "-" && bRows.Count(x => Norm(x["SpeciesKo"]) == ko) == 1)
    { r = bRows.First(x => Norm(x["SpeciesKo"]) == ko); byKo++; koOnly.Add($"{no} {ko} 지침 '{g["학명"]}' ↔ 앱 '{r["SpeciesEn"]}'"); }
    else if (groupMap.TryGetValue(no, out var appEn) && bRows.Count(x => x["SpeciesEn"] == appEn) == 1)
    { r = bRows.First(x => x["SpeciesEn"] == appEn); byMap++; mapUsed.Add($"{no} '{(g["국명"].Trim().Length > 0 ? g["국명"] : g["분류군(무리 행)"]).Trim()}' ↔ {r["SpeciesKo"]} {appEn}"); }
    if (r is null) { W($"- 못 찾음 {no} {g["국명"]} {g["학명"]} {g["분류군(무리 행)"]}"); problems++; continue; }
    used.Add(r["BenthosSpeciesListId"]!);

    var s = double.Parse(g["오탁치 s"], CultureInfo.InvariantCulture); var w = int.Parse(g["지표가중치 g"]);
    double? ds = r["SaprobicValue"] is string sv ? double.Parse(sv, CultureInfo.InvariantCulture) : null;
    int? dw = r["IndicatorWeight"] is string wv ? int.Parse(wv) : null;
    if (ds is double x1 && Math.Abs(x1 - s) < 1e-9 && dw == w) valSame++;
    else { W($"- 값 다름 {no} {g["국명"]} {g["학명"]}: 지침 {s}/{w} ↔ DB {Show(r["SaprobicValue"])}/{Show(r["IndicatorWeight"])}"); problems++; }

    var a = BenthosOf(r["SaprobicValue"], r["IndicatorWeight"], 10); var b = BenthosOf(g["오탁치 s"], g["지표가중치 g"], 10);
    if (BmiKey([a]) == BmiKey([b])) bCalcSame++; else { W($"- BMI 다름 {no}: DB {BmiKey([a])} ↔ 지침 {BmiKey([b])}"); problems++; }
    int k = bAll.Count + 1;
    bAll.Add((BenthosOf(r["SaprobicValue"], r["IndicatorWeight"], k % 23 + 1), BenthosOf(g["오탁치 s"], g["지표가중치 g"], k % 23 + 1)));
}
var appOnlyVal = bRows.Where(r => r["SaprobicValue"] != null && !used.Contains(r["BenthosSpeciesListId"]!)).ToList();
W($"- 대응: 학명 일치 {byEn} · 국명 일치(학명 표기 다름) {byKo} · 대응표(무리 · 깔따구) {byMap} · 못 찾음 {bGuide.Count - byEn - byKo - byMap}");
W($"- 오탁치 · 가중치 같음 {valSame} / {bGuide.Count} · 값 다름 {bGuide.Count - valSame - (bGuide.Count - byEn - byKo - byMap)} · 종별 BMI 일치 {bCalcSame} / {bGuide.Count}");
var ka2 = BmiKey(bAll.Select(x => x.db)); var kb2 = BmiKey(bAll.Select(x => x.g));
W($"- 지침 전 종 한 표본 BMI : DB {ka2} · 지침 {kb2} → {(ka2 == kb2 ? "같음" : "다름")}");
if (ka2 != kb2) problems++;
W($"- DB 에 지표값이 있는데 지침 대응이 없는 행 {appOnlyVal.Count}{(appOnlyVal.Count > 0 ? ": " + string.Join(", ", appOnlyVal.Select(r => $"{r["SpeciesKo"]} {r["SpeciesEn"]} {r["SaprobicValue"]}/{r["IndicatorWeight"]}")) : "")}");
if (appOnlyVal.Count > 0) problems++;
W("");
W($"### 국명으로 맞춘 행(학명 표기만 다름 · 값 같음) {koOnly.Count}");
foreach (var x in koOnly) W($"- {x}");
W($"### 대응표 {mapUsed.Count}");
foreach (var x in mapUsed) W($"- {x}");
W("");
W(problems == 0 ? "결과: 문제 0" : $"결과: 문제 {problems}");

if (outPath != null) File.WriteAllText(outPath, rep.ToString(), new UTF8Encoding(false));
return problems == 0 ? 0 : 1;

// ── 도구 ──────────────────────────────────────────────────────────────────
static string Norm(string? s) => string.Join(' ', (s ?? "").Replace(' ', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries));
static string Ko(string s) => s.Trim().Split(' ')[0];
static string Show(string? s) => s is null ? "NULL" : s.Replace(" ", "␣(NBSP)");

static List<Dictionary<string, string?>> Query(SqliteConnection c, string sql)
{
    using var cmd = c.CreateCommand(); cmd.CommandText = sql;
    using var r = cmd.ExecuteReader();
    var list = new List<Dictionary<string, string?>>();
    while (r.Read())
    {
        var d = new Dictionary<string, string?>();
        for (int i = 0; i < r.FieldCount; i++)
            d[r.GetName(i)] = r.IsDBNull(i) ? null : Convert.ToString(r.GetValue(i), CultureInfo.InvariantCulture);
        list.Add(d);
    }
    return list;
}

static List<Dictionary<string, string>> ReadCsv(string path)
{
    var lines = File.ReadAllLines(path, Encoding.UTF8);
    var head = Split(lines[0]);
    return lines.Skip(1).Where(l => l.Trim().Length > 0)
        .Select(l => { var v = Split(l); return head.Select((h, i) => (h, v: i < v.Count ? v[i] : "")).ToDictionary(x => x.h, x => x.v); }).ToList();
}

static List<string> Split(string line)
{
    var res = new List<string>(); var sb = new StringBuilder(); bool q = false;
    for (int i = 0; i < line.Length; i++)
    {
        var ch = line[i];
        if (q) { if (ch == '"') { if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; } else q = false; } else sb.Append(ch); }
        else if (ch == '"') q = true;
        else if (ch == ',') { res.Add(sb.ToString()); sb.Clear(); }
        else sb.Append(ch);
    }
    res.Add(sb.ToString());
    return res;
}

static ImportFishSpecies FishOf(string? tol, string? feed, string? hab, string? exo, int count) =>
    new() { ToleranceGuild = tol ?? "", FeedingGuild = feed ?? "", HabitatGuild = hab ?? "", Exotic = exo ?? "", IndividualCount = count };

static string FaiKey(IEnumerable<ImportFishSpecies> list, int chasu = 3)
{
    var d = EcologyCalculator.CalculateFaiDetail(list.ToList(), null, 0, chasu);
    return $"M{d.M1}/{d.M2}/{d.M3}/{d.M4}/{d.M5}/{d.M6}/{d.M7}/{d.M8} 점수 {d.Score} {d.Grade}";
}

static ImportBenthosSpecies BenthosOf(string? s, string? g, int count) =>
    new() { SaprobicValue = s, IndicatorWeight = g, IndividualCount = count };

static string BmiKey(IEnumerable<ImportBenthosSpecies> data)
{
    var list = data.ToList();
    int[] counts = [.. list.Select(x => (int)(x.IndividualCount ?? 0))];
    foreach (var x in list) x.RankScorer = BenthosCalculator.GetRankScorer(counts, (int)(x.IndividualCount ?? 0));
    var (score, grade) = BenthosCalculator.GetBMI(list, null);
    return $"{score?.ToString("R", CultureInfo.InvariantCulture) ?? "null"} {grade}";
}
