namespace SDSM_App.Helpers;

/// <summary>
/// 저서 하상구성은 지침(국립환경과학원고시 제2026-30호) [서식 2] · &lt;표 1&gt; 6칸이다 —
/// 진흙 이하 · 모래 · 왕모래(2~16) · 자갈 · 작은돌 · 큰돌 이상. 암반 · 콘크리트는 칸 밖이다(2026-10-21 §20).
/// 기록에 암반 · 콘크리트 값(0보다 큼)이 있으면 특이사항(Note) 끝에 옮겨 적고 두 칸은 NULL 로 둔다. 0 · 빈칸은 버린다.
/// DB 정리 SQL(tools\sql\benthos_bedrock_concrete_to_note_261021.sql)도 같은 문구를 만든다 — 고치면 둘 다 고친다.
/// (어류 하상은 지침 [서식 3]도 8칸이라 그대로 둔다)
/// </summary>
public static class BenthosSubstrateNote
{
    /// <summary>'하상 암반 n% · 콘크리트 n%(지침 저서 하상 6칸 밖 · 옮김)' — 0보다 큰 칸만 적는다. 둘 다 없으면 null.</summary>
    public static string? MovedText(int? bedrock, int? concrete)
    {
        var parts = new List<string>();
        if (bedrock > 0) parts.Add($"암반 {bedrock}%");
        if (concrete > 0) parts.Add($"콘크리트 {concrete}%");

        return parts.Count == 0 ? null : $"하상 {string.Join(" · ", parts)}(지침 저서 하상 6칸 밖 · 옮김)";
    }

    /// <summary>특이사항 끝에 덧붙인다 — 특이사항이 비어 있으면 옮긴 문구만, 있으면 ' / ' 로 잇는다.</summary>
    public static string? AppendTo(string? note, string? movedText)
    {
        if (movedText == null) return note;
        return string.IsNullOrWhiteSpace(note) ? movedText : $"{note} / {movedText}";
    }
}
