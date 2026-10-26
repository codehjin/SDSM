namespace SDSM_App.Enums;

/// <summary>
/// 일괄등록 때 종목록에 없는 종이 있을 때 처리(2026-10-22 §22-3). 어느 쪽이든 그 종 행의 지표값 · 길드는 비워 저장한다.
/// </summary>
public enum OutOfListAction
{
    /// <summary>취소(기본) — 저장하지 않는다.</summary>
    Cancel = 0,
    /// <summary>목록에 추가 — 종목록에 지표값 · 길드 없이(출현 목록) 더하고 저장.</summary>
    AddToList,
    /// <summary>목록에 넣지 않고 저장 — 기록만 저장.</summary>
    SaveOnly,
}
