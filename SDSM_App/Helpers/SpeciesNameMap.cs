namespace SDSM_App.Helpers;

/// <summary>
/// 옛 학명 → 지침(국립환경과학원고시 제2026-30호) 표기 대응표 — 어류 · 저서 한 곳(2026-10-22 §21-1 · §22-1).
/// 2026-03 일괄입력 양식 · 조사자 내보내기(species.json 정비 전)에는 옛 학명이 그대로 있어,
/// 그대로 읽으면 일괄등록이 옛 학명을 '목록 밖 종'으로 보고 기록에도 옛 학명이 남는다.
/// 종목록 · 기존 기록은 tools\sql\species_list_jichim2026_261022.sql 이 같은 표로 바꿨다 — 고치면 둘 다 고친다.
/// </summary>
public static class SpeciesNameMap
{
    private static readonly Dictionary<string, string> FishOldToGuideline = new()
    {
        ["Acanthorhodeus chankaensis"] = "Acheilognathus chankaensis",   // 가시납지리
        ["Gobiobotia nakdongensis"] = "Gobiobotia naktongensis",         // 흰수마자
        ["Furcina oshimai"] = "Furcina osimae",                          // 무늬횟대(옛 값은 줄바꿈 없는 공백 U+00A0 이 섞여 있다)
    };

    private static readonly Dictionary<string, string> BenthosOldToGuideline = new()
    {
        ["Koreanomelania nodifila"] = "Koreoleptoxis nodifila",                                          // 13 염주알다슬기
        ["Koreoleptoxis globus ovalis"] = "Koreoleptoxis globus",                                        // 14 띠구슬다슬기
        ["Koreanomelania paucicincta"] = "Semisulcospira calculus",                                      // 15 주머니알다슬기
        ["Corbicula (Corbicula) fluminalis"] = "Corbicula fluminalis",                                   // 38 대륙재첩
        ["Corbicula (Corbiculina) insularis"] = "Corbicula insularis",                                   // 40 섬재첩
        ["Corbicula (Corbiculina) leana"] = "Corbicula leana",                                           // 42 참재첩
        ["Corbicula (Corbiculina) portentosa"] = "Corbicula portentosa",                                 // 44 점박이재첩
        ["Hesperocorixa mandsshurica"] = "Hesperocorixa mandshurica",                                    // 293 닮은물벌레(옛 값 오타)
        ["Tipula (Yamatotipula) latemarginata latemarginata"] = "Tipula (Yamatotipula) latemarginata",   // 341 애아이노각다귀
    };

    /// <summary>어류 — 옛 표기면 지침 표기를, 아니면 받은 값을 그대로 돌려준다(종명은 원문 그대로 · 비교할 때만 공백을 고른다).</summary>
    public static string? Fish(string? speciesEn) => Map(FishOldToGuideline, speciesEn);

    /// <summary>저서 — 옛 표기면 지침 표기를, 아니면 받은 값 그대로.</summary>
    public static string? Benthos(string? speciesEn) => Map(BenthosOldToGuideline, speciesEn);

    private static string? Map(Dictionary<string, string> table, string? speciesEn)
    {
        if (string.IsNullOrWhiteSpace(speciesEn)) return speciesEn;

        //비교할 때만 U+00A0 을 보통 공백으로 · 연속 공백을 하나로
        var key = string.Join(' ', speciesEn.Replace(' ', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return table.TryGetValue(key, out var guideline) ? guideline : speciesEn;
    }
}
