namespace SDSM_App.Enums;

/// <summary>
/// 일괄등록 때 DB에 같은 기록이 있을 때 처리 방법(2026-10-21 연계시험 문제 2·3).
/// </summary>
public enum DuplicateAction
{
    /// <summary>건너뛰기(기본) — 새 기록을 저장하지 않는다.</summary>
    Skip = 0,
    /// <summary>바꾸기 — 옛 기록(부모와 종 행, 또는 같은 묶음 행)을 지우고 새로 저장(한 트랜잭션).</summary>
    Replace,
    /// <summary>추가 — 옛 기록은 두고 새 기록을 더 저장(종전 동작).</summary>
    Add,
}
