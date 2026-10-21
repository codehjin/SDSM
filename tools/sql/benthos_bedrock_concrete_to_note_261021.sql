-- ============================================================================
-- 저서 하상 암반 · 콘크리트 → 특이사항(Note) 옮기기 — 일회성 정리 (2026-10-21 · 지침 저서 하상 6칸)
--
-- 근거  : ..\SDSM_Surveyor\docs\CLAUDE_CODE_TASK_261015.md §20 · 지침근거_수생태계건강성평가_2026-30.md §6
--         「수생태계 현황 조사 및 건강성 평가 방법 등에 관한 지침(하천편)」(국립환경과학원고시 제2026-30호)
--         [서식 2] 저서 하상은 6칸(진흙 이하 · 모래 · 왕모래 · 자갈 · 작은돌 · 큰돌 이상) — 암반 · 콘크리트가 없다.
--         관리자 앱(2026-10-21 판)은 저서 화면에서 두 칸을 없애고, 값이 있으면 특이사항으로 옮긴다.
--         이 스크립트는 이미 DB 에 들어 있는 값을 같은 방식으로 정리한다.
-- 대상  : 관리자 DB(SDSMDB.sqlite) — 개발 PC 게시본 · 발주처 배포본
-- 바꾸는 것 : Benthos 표의 Bedrock · Concrete · Note 칸만. 어류(Fish) 하상 8칸은 지침 [서식 3]도 8칸이라 건드리지 않는다.
--   ① Bedrock 또는 Concrete 가 0보다 크면 Note 끝에 덧붙인다(Note 가 있으면 ' / ' 로 잇는다)
--        '하상 암반 n% · 콘크리트 n%(지침 저서 하상 6칸 밖 · 옮김)'  — 0보다 큰 칸만 적는다
--        (관리자 앱 SDSM_App\Helpers\BenthosSubstrateNote.cs 와 같은 문구)
--   ② 그 뒤 남은 Bedrock · Concrete 값(0 포함)은 모두 NULL
--   표 구조(스키마)는 바꾸지 않는다 — 두 열은 그대로 두고 값만 비운다.
--
-- 다시 돌려도 : ① 은 0보다 큰 값이 남은 행만, ② 는 값이 남은 행만 바꾸므로 두 번째부터는 바뀌는 행이 0 이다(멱등).
--
-- 기대 (2026-03-05 배포본 그대로 · 복구 SQL 뒤) : 저서_전체 188 · 옮길_기록 0 · 값_남음 0 → 바뀐 행 0 · 0
--   (게시 DB 의 저서 188건은 암반 · 콘크리트가 모두 빈칸이다 — 2026-10-21 읽기 전용 확인)
--
-- 실행 순서 (배포 때) : tools\sql\README.md
--   ① mammal_observation_site_restore_261016.sql  ② 이 파일
--
-- 실행 방법
--   · 앱을 닫고 SDSMDB.sqlite 를 복사해 백업한다(예: SDSMDB_백업_YYYYMMDD.sqlite).
--   · 이 파일은 맨 앞 BEGIN TRANSACTION · 맨 끝 COMMIT 으로 한 트랜잭션이다(중간에 실패하면 아무것도 바뀌지 않는다).
--       sqlite3 명령줄 : sqlite3 SDSMDB.sqlite ".read benthos_bedrock_concrete_to_note_261021.sql"
--       DB Browser for SQLite : [SQL 실행] 탭에서 이 파일을 열어 전체 실행 → 맨 끝 COMMIT 에서 파일에 쓴다.
--         (결과를 보고 저장/취소를 고르려면 BEGIN TRANSACTION · COMMIT 두 줄을 지우고 실행한 뒤
--          [변경사항 저장하기] 또는 [변경사항 취소하기] — 복구 SQL 과 같은 방식)
--   · 결과 줄 : '전' · 옮긴_행 · 비운_행 · '후'. 후에는 옮길_기록 0 · 값_남음 0 이어야 한다.
--   되돌리기 : 백업 파일로 바꿔 놓는다.
-- ============================================================================

BEGIN TRANSACTION;

-- ── 전 (읽기만) ────────────────────────────────────────────────────────────
SELECT '전' AS 시점,
       (SELECT count(*) FROM Benthos) AS 저서_전체,
       (SELECT count(*) FROM Benthos WHERE Bedrock > 0 OR Concrete > 0) AS 옮길_기록,
       (SELECT count(*) FROM Benthos WHERE Bedrock IS NOT NULL OR Concrete IS NOT NULL) AS 값_남음,
       (SELECT count(*) FROM Benthos WHERE Note LIKE '%(지침 저서 하상 6칸 밖 · 옮김)%') AS 옮김_표시;

-- ── ① 0보다 큰 값 → 특이사항 끝 ───────────────────────────────────────────
UPDATE Benthos
   SET Note = CASE WHEN Note IS NULL OR trim(Note, ' ' || char(9) || char(10) || char(13)) = '' THEN ''
                   ELSE Note || ' / ' END
              || '하상 '
              || CASE WHEN Bedrock > 0 AND Concrete > 0 THEN '암반 ' || Bedrock || '% · 콘크리트 ' || Concrete || '%'
                      WHEN Bedrock > 0 THEN '암반 ' || Bedrock || '%'
                      ELSE '콘크리트 ' || Concrete || '%' END
              || '(지침 저서 하상 6칸 밖 · 옮김)'
 WHERE Bedrock > 0 OR Concrete > 0;

SELECT changes() AS 옮긴_행;

-- ── ② 남은 값(0 포함) 비우기 ───────────────────────────────────────────────
UPDATE Benthos
   SET Bedrock = NULL,
       Concrete = NULL
 WHERE Bedrock IS NOT NULL OR Concrete IS NOT NULL;

SELECT changes() AS 비운_행;

-- ── 후 (읽기만) — 옮길_기록 0 · 값_남음 0 ─────────────────────────────────
SELECT '후' AS 시점,
       (SELECT count(*) FROM Benthos) AS 저서_전체,
       (SELECT count(*) FROM Benthos WHERE Bedrock > 0 OR Concrete > 0) AS 옮길_기록,
       (SELECT count(*) FROM Benthos WHERE Bedrock IS NOT NULL OR Concrete IS NOT NULL) AS 값_남음,
       (SELECT count(*) FROM Benthos WHERE Note LIKE '%(지침 저서 하상 6칸 밖 · 옮김)%') AS 옮김_표시;

COMMIT;
