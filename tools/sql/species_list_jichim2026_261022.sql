-- ============================================================================
-- 종목록 지침 정비 — 어류 · 저서 종목록을 지침(제2026-30호) 별표 3 · 별표 2 에 맞춘다 (2026-10-22 · §21 · 보완 §22)
--
-- 근거  : ..\SDSM_Surveyor\docs\CLAUDE_CODE_TASK_261015.md §21 · 지침근거_수생태계건강성평가_2026-30.md §8 · §9
--         「수생태계 현황 조사 및 건강성 평가 방법 등에 관한 지침(하천편)」(국립환경과학원고시 제2026-30호)
--         별표 3 어류 종목록 · 별표 2 저서 지표종(추출본 지침2026-30_별표3_어류종목록.csv · 별표2_저서종목록.csv)
-- 대상  : 관리자 DB(SDSMDB.sqlite) — 개발 PC 게시본 · 발주처 배포본
-- 바꾸는 것
--   [어류 FishSpeciesList]
--   ① 4종 추가(지침에만 있던 종) — 국명 · 학명 · 목 · 과 · 내성 · 섭식 · 서식지 · 외래는 별표 3 그대로.
--        목 · 과는 기존 행처럼 한글만(별표 3 의 영문 표기는 넣지 않는다 — 기존 253행이 모두 한글만이라 통계 묶음이 갈라지지 않게).
--        강 = 조기강(4종 모두 조기어류). 어류특성확정(FishTrait) = 'O'(별표 3 속성을 그대로 갖춘 행 — 기존 목록 251행과 같음.
--        '' 인 2행(히메치 · 무늬횟대)은 일괄등록이 자동으로 더한 행). 고유 · 멸종위기 I · II · 천연기념물 = ''(별표 3 에 없는 칸 ·
--        네 종 모두 해당 없음 · 기존 행도 빈 값은 '' 로 둔다).
--        계통별 순서(LineageOrder) = 253 ~ 256(지침 순서). 기존 1 ~ 252 는 지침 번호와 다른 계통 순서이고 정수 · 중복 없음이라
--        사이에 끼울 수 없다 — 기존 행 번호는 바꾸지 않고 끝에 붙인다.
--   ② 학명 3건을 지침 표기로 — 종목록과 기존 기록(FishSpecies.SpeciesEn) 둘 다
--        가시납지리 Acanthorhodeus chankaensis → Acheilognathus chankaensis
--        흰수마자   Gobiobotia nakdongensis    → Gobiobotia naktongensis
--        무늬횟대   Furcina oshimai            → Furcina osimae   (기존 값은 'Furcina' 뒤가 줄바꿈 없는 공백 U+00A0)
--   [저서 BenthosSpeciesList]
--   ③ Babronia sp. — 오탁치 2.8 · 지표가중치 4. 행이 없으면 더한다(2026-03-05 배포본에는 행이 없다 — 작업로그 참조).
--        분류: 환형동물문 · 환대강 · 턱거머리목 · 돌거머리과(지침 표에서 참거머리와 돌거머리 사이) · 국명 '-'(지침 표기).
--        기존 기록(BenthosSpecies)에 같은 학명이 있으면 그 행 값도 같게(배포본 0행).
--   ④ 가는점물땡땡이 Laccobius (Microlaccobius) formosus — 2.9 · 3 추가(분류는 꼬마점물땡땡이 행과 같다)
--   ⑤ 꼬마점물땡땡이 Laccobius (Microlaccobius) fragilis — 오탁치 · 지표가중치 NULL(종목록에는 남긴다 · 출현 목록 종)
--   ⑥ 저서 학명 9종을 지침 표기로(2026-10-22 §22-1) — 종목록과 기존 기록(BenthosSpecies.SpeciesEn) 둘 다(배포본 기록 = 애아이노각다귀 2행)
--        13 염주알다슬기   Koreanomelania nodifila            → Koreoleptoxis nodifila
--        14 띠구슬다슬기   Koreoleptoxis globus ovalis        → Koreoleptoxis globus
--        15 주머니알다슬기 Koreanomelania paucicincta         → Semisulcospira calculus
--        38 대륙재첩       Corbicula (Corbicula) fluminalis   → Corbicula fluminalis
--        40 섬재첩         Corbicula (Corbiculina) insularis  → Corbicula insularis
--        42 참재첩         Corbicula (Corbiculina) leana      → Corbicula leana
--        44 점박이재첩     Corbicula (Corbiculina) portentosa → Corbicula portentosa
--        293 닮은물벌레    Hesperocorixa mandsshurica(오타)    → Hesperocorixa mandshurica
--        341 애아이노각다귀 Tipula (Yamatotipula) latemarginata latemarginata → Tipula (Yamatotipula) latemarginata
--        108 톡토기류 Collembola sp. 는 그대로(지침은 강 단위 표기).
--        옛 학명 → 지침 표기 대응은 관리자 앱 SDSM_App/Helpers/SpeciesNameMap.cs 와 같은 표다(일괄등록이 옛 양식을 읽을 때 쓴다) — 고치면 둘 다 고친다.
--   무리 행 15 · 깔따구 2 는 값이 지침과 같아 이름을 바꾸지 않는다.
--   표 구조(스키마)는 바꾸지 않는다.
--
-- 다시 돌려도 : 각 문장은 '아직 바뀌지 않은 행'만 고르므로 두 번째부터 바뀌는 행이 0 이다(멱등).
--
-- 기대 (2026-03-05 배포본 + ① 포유류 복구 + ② 저서 정리 뒤)
--   전 : 어류 목록 253 · 저서 목록 821(지표값 446) · 옛 학명 기록 18 · 저서 옛 학명 목록 9 · 기록 2
--   바뀐 행 : 어류 추가 4 · 목록 학명 3 · 기록 학명 18 · Babronia 1(추가) · 가는점물땡땡이 1(추가) · 꼬마점물땡땡이 1 · Babronia 기록 0
--             · 저서 목록 학명 9 · 저서 기록 학명 2
--   후 : 어류 목록 257 · 저서 목록 823(지표값 447) · 옛 학명 0 · 저서 옛 학명 0
--
-- 실행 순서 (배포 때) : tools\sql\README.md
--   ① mammal_observation_site_restore_261016.sql  ② benthos_bedrock_concrete_to_note_261021.sql  ③ 이 파일
--
-- 실행 방법
--   · 앱을 닫고 SDSMDB.sqlite 를 복사해 백업한다.
--   · 이 파일은 맨 앞 BEGIN TRANSACTION · 맨 끝 COMMIT 으로 한 트랜잭션이다(중간에 실패하면 아무것도 바뀌지 않는다).
--       sqlite3 명령줄 : sqlite3 SDSMDB.sqlite ".read species_list_jichim2026_261022.sql"
--       DB Browser for SQLite : [SQL 실행] 탭에서 이 파일을 열어 전체 실행 → 맨 끝 COMMIT 에서 파일에 쓴다.
--         (결과를 보고 저장/취소를 고르려면 BEGIN TRANSACTION · COMMIT 두 줄을 지우고 실행한 뒤 [변경사항 저장하기] 또는 [취소하기])
--   되돌리기 : 백업 파일로 바꿔 놓는다.
-- ============================================================================

BEGIN TRANSACTION;


-- ── 전 (읽기만) ────────────────────────────────────────────────────────────
SELECT '전' AS 시점,
       (SELECT count(*) FROM FishSpeciesList) AS 어류_목록,
       (SELECT count(*) FROM BenthosSpeciesList) AS 저서_목록,
       (SELECT count(*) FROM BenthosSpeciesList WHERE SaprobicValue IS NOT NULL) AS 저서_지표값,
       (SELECT count(*) FROM FishSpecies
         WHERE SpeciesEn IN ('Acanthorhodeus chankaensis', 'Gobiobotia nakdongensis')
            OR replace(SpeciesEn, char(160), ' ') = 'Furcina oshimai') AS 옛_학명_기록,
       (SELECT count(*) FROM BenthosSpeciesList WHERE SpeciesEn IN ('Koreanomelania nodifila',
                     'Koreoleptoxis globus ovalis',
                     'Koreanomelania paucicincta',
                     'Corbicula (Corbicula) fluminalis',
                     'Corbicula (Corbiculina) insularis',
                     'Corbicula (Corbiculina) leana',
                     'Corbicula (Corbiculina) portentosa',
                     'Hesperocorixa mandsshurica',
                     'Tipula (Yamatotipula) latemarginata latemarginata')) AS 저서_옛_학명_목록,
       (SELECT count(*) FROM BenthosSpecies WHERE SpeciesEn IN ('Koreanomelania nodifila',
                     'Koreoleptoxis globus ovalis',
                     'Koreanomelania paucicincta',
                     'Corbicula (Corbicula) fluminalis',
                     'Corbicula (Corbiculina) insularis',
                     'Corbicula (Corbiculina) leana',
                     'Corbicula (Corbiculina) portentosa',
                     'Hesperocorixa mandsshurica',
                     'Tipula (Yamatotipula) latemarginata latemarginata')) AS 저서_옛_학명_기록;

-- ── ① 어류 4종 추가(없을 때만) ─────────────────────────────────────────────
INSERT INTO FishSpeciesList
       (FishTrait, ToleranceGuild, FeedingGuild, HabitatGuild, Exotic, Endemic, Endangered1, Endangered2, NaturalMonument,
        LineageOrder, ClassKo, OrderKo, FamilyKo, SpeciesEn, SpeciesKo)
SELECT n.FishTrait, n.ToleranceGuild, n.FeedingGuild, n.HabitatGuild, n.Exotic, '', '', '', '',
       n.LineageOrder, '조기강', n.OrderKo, n.FamilyKo, n.SpeciesEn, n.SpeciesKo
  FROM (SELECT 'O' AS FishTrait, 'IS' AS ToleranceGuild, 'O' AS FeedingGuild, '' AS HabitatGuild, '' AS Exotic,
               253 AS LineageOrder, '청어목' AS OrderKo, '갯농어과' AS FamilyKo, 'Chanos chanos' AS SpeciesEn, '갯농어' AS SpeciesKo
        UNION ALL SELECT 'O', 'IS', 'C', '', 'O', 254, '연어목', '연어과', 'Salmo trutta', '브라운송어'
        UNION ALL SELECT 'O', 'IS', 'O', '', '', 255, '농어목', '납작돔과', 'Scatophagus argus', '납작돔'
        UNION ALL SELECT 'O', 'IS', 'C', '', '', 256, '동갈치목', '동갈치과', 'Strongylura anastomella', '동갈치') AS n
 WHERE NOT EXISTS (SELECT 1 FROM FishSpeciesList f WHERE f.SpeciesKo = n.SpeciesKo);

SELECT changes() AS 어류_추가;

-- ── ② 학명 3건 — 종목록 · 기존 기록 ───────────────────────────────────────
UPDATE FishSpeciesList SET SpeciesEn = 'Acheilognathus chankaensis'
 WHERE SpeciesKo = '가시납지리' AND SpeciesEn = 'Acanthorhodeus chankaensis';
UPDATE FishSpeciesList SET SpeciesEn = 'Gobiobotia naktongensis'
 WHERE SpeciesKo = '흰수마자' AND SpeciesEn = 'Gobiobotia nakdongensis';
UPDATE FishSpeciesList SET SpeciesEn = 'Furcina osimae'
 WHERE SpeciesKo = '무늬횟대' AND replace(SpeciesEn, char(160), ' ') = 'Furcina oshimai';

SELECT (SELECT count(*) FROM FishSpeciesList
         WHERE SpeciesEn IN ('Acheilognathus chankaensis', 'Gobiobotia naktongensis', 'Furcina osimae')) AS 목록_새_학명;

UPDATE FishSpecies SET SpeciesEn = 'Acheilognathus chankaensis'
 WHERE SpeciesEn = 'Acanthorhodeus chankaensis';
SELECT changes() AS 기록_가시납지리;
UPDATE FishSpecies SET SpeciesEn = 'Gobiobotia naktongensis'
 WHERE SpeciesEn = 'Gobiobotia nakdongensis';
SELECT changes() AS 기록_흰수마자;
UPDATE FishSpecies SET SpeciesEn = 'Furcina osimae'
 WHERE replace(SpeciesEn, char(160), ' ') = 'Furcina oshimai';
SELECT changes() AS 기록_무늬횟대;

-- ── ③ Babronia sp. 2.8 · 4 ─────────────────────────────────────────────────
UPDATE BenthosSpeciesList SET SaprobicValue = 2.8, IndicatorWeight = 4
 WHERE SpeciesEn = 'Babronia sp.' AND (SaprobicValue IS NOT 2.8 OR IndicatorWeight IS NOT 4);
SELECT changes() AS Babronia_목록_고침;

INSERT INTO BenthosSpeciesList
       (SaprobicValue, IndicatorWeight, Endangered1, Endangered2, Endemic, PhylumKo, ClassKo, OrderKo, FamilyKo, SpeciesKo, SpeciesEn)
SELECT 2.8, 4, '', '', '', '환형동물문', '환대강', '턱거머리목', '돌거머리과', '-', 'Babronia sp.'
 WHERE NOT EXISTS (SELECT 1 FROM BenthosSpeciesList WHERE SpeciesEn = 'Babronia sp.');
SELECT changes() AS Babronia_목록_추가;

UPDATE BenthosSpecies SET SaprobicValue = 2.8, IndicatorWeight = 4
 WHERE SpeciesEn = 'Babronia sp.' AND (SaprobicValue IS NOT 2.8 OR IndicatorWeight IS NOT 4);
SELECT changes() AS Babronia_기록;

-- ── ④ 가는점물땡땡이 추가(분류는 꼬마점물땡땡이 행과 같게) ─────────────────
INSERT INTO BenthosSpeciesList
       (SaprobicValue, IndicatorWeight, Endangered1, Endangered2, Endemic, PhylumKo, ClassKo, OrderKo, FamilyKo, SpeciesKo, SpeciesEn)
SELECT 2.9, 3, '', '', '', b.PhylumKo, b.ClassKo, b.OrderKo, b.FamilyKo, '가는점물땡땡이', 'Laccobius (Microlaccobius) formosus'
  FROM BenthosSpeciesList b
 WHERE b.SpeciesEn = 'Laccobius (Microlaccobius) fragilis'
   AND NOT EXISTS (SELECT 1 FROM BenthosSpeciesList WHERE SpeciesEn = 'Laccobius (Microlaccobius) formosus')
 LIMIT 1;
SELECT changes() AS 가는점물땡땡이_추가;

-- ── ⑤ 꼬마점물땡땡이 지표값 비움(종목록에는 남김) ───────────────────────────
UPDATE BenthosSpeciesList SET SaprobicValue = NULL, IndicatorWeight = NULL
 WHERE SpeciesEn = 'Laccobius (Microlaccobius) fragilis' AND (SaprobicValue IS NOT NULL OR IndicatorWeight IS NOT NULL);
SELECT changes() AS 꼬마점물땡땡이_비움;

-- ── ⑥ 저서 학명 9종 — 종목록 · 기존 기록 ───────────────────────────────────
UPDATE BenthosSpeciesList
   SET SpeciesEn = CASE SpeciesEn
                     WHEN 'Koreanomelania nodifila' THEN 'Koreoleptoxis nodifila'
                     WHEN 'Koreoleptoxis globus ovalis' THEN 'Koreoleptoxis globus'
                     WHEN 'Koreanomelania paucicincta' THEN 'Semisulcospira calculus'
                     WHEN 'Corbicula (Corbicula) fluminalis' THEN 'Corbicula fluminalis'
                     WHEN 'Corbicula (Corbiculina) insularis' THEN 'Corbicula insularis'
                     WHEN 'Corbicula (Corbiculina) leana' THEN 'Corbicula leana'
                     WHEN 'Corbicula (Corbiculina) portentosa' THEN 'Corbicula portentosa'
                     WHEN 'Hesperocorixa mandsshurica' THEN 'Hesperocorixa mandshurica'
                     WHEN 'Tipula (Yamatotipula) latemarginata latemarginata' THEN 'Tipula (Yamatotipula) latemarginata'
                   END
 WHERE SpeciesEn IN ('Koreanomelania nodifila',
                     'Koreoleptoxis globus ovalis',
                     'Koreanomelania paucicincta',
                     'Corbicula (Corbicula) fluminalis',
                     'Corbicula (Corbiculina) insularis',
                     'Corbicula (Corbiculina) leana',
                     'Corbicula (Corbiculina) portentosa',
                     'Hesperocorixa mandsshurica',
                     'Tipula (Yamatotipula) latemarginata latemarginata');
SELECT changes() AS 저서_목록_학명;

UPDATE BenthosSpecies
   SET SpeciesEn = CASE SpeciesEn
                     WHEN 'Koreanomelania nodifila' THEN 'Koreoleptoxis nodifila'
                     WHEN 'Koreoleptoxis globus ovalis' THEN 'Koreoleptoxis globus'
                     WHEN 'Koreanomelania paucicincta' THEN 'Semisulcospira calculus'
                     WHEN 'Corbicula (Corbicula) fluminalis' THEN 'Corbicula fluminalis'
                     WHEN 'Corbicula (Corbiculina) insularis' THEN 'Corbicula insularis'
                     WHEN 'Corbicula (Corbiculina) leana' THEN 'Corbicula leana'
                     WHEN 'Corbicula (Corbiculina) portentosa' THEN 'Corbicula portentosa'
                     WHEN 'Hesperocorixa mandsshurica' THEN 'Hesperocorixa mandshurica'
                     WHEN 'Tipula (Yamatotipula) latemarginata latemarginata' THEN 'Tipula (Yamatotipula) latemarginata'
                   END
 WHERE SpeciesEn IN ('Koreanomelania nodifila',
                     'Koreoleptoxis globus ovalis',
                     'Koreanomelania paucicincta',
                     'Corbicula (Corbicula) fluminalis',
                     'Corbicula (Corbiculina) insularis',
                     'Corbicula (Corbiculina) leana',
                     'Corbicula (Corbiculina) portentosa',
                     'Hesperocorixa mandsshurica',
                     'Tipula (Yamatotipula) latemarginata latemarginata');
SELECT changes() AS 저서_기록_학명;

-- ── 후 (읽기만) ────────────────────────────────────────────────────────────
SELECT '후' AS 시점,
       (SELECT count(*) FROM FishSpeciesList) AS 어류_목록,
       (SELECT count(*) FROM BenthosSpeciesList) AS 저서_목록,
       (SELECT count(*) FROM BenthosSpeciesList WHERE SaprobicValue IS NOT NULL) AS 저서_지표값,
       (SELECT count(*) FROM FishSpecies
         WHERE SpeciesEn IN ('Acanthorhodeus chankaensis', 'Gobiobotia nakdongensis')
            OR replace(SpeciesEn, char(160), ' ') = 'Furcina oshimai') AS 옛_학명_기록,
       (SELECT count(*) FROM FishSpecies WHERE SpeciesEn = 'Acheilognathus chankaensis') AS 새_학명_기록,
       (SELECT count(*) FROM BenthosSpeciesList WHERE SpeciesEn IN ('Koreanomelania nodifila',
                     'Koreoleptoxis globus ovalis',
                     'Koreanomelania paucicincta',
                     'Corbicula (Corbicula) fluminalis',
                     'Corbicula (Corbiculina) insularis',
                     'Corbicula (Corbiculina) leana',
                     'Corbicula (Corbiculina) portentosa',
                     'Hesperocorixa mandsshurica',
                     'Tipula (Yamatotipula) latemarginata latemarginata')) AS 저서_옛_학명_목록,
       (SELECT count(*) FROM BenthosSpecies WHERE SpeciesEn IN ('Koreanomelania nodifila',
                     'Koreoleptoxis globus ovalis',
                     'Koreanomelania paucicincta',
                     'Corbicula (Corbicula) fluminalis',
                     'Corbicula (Corbiculina) insularis',
                     'Corbicula (Corbiculina) leana',
                     'Corbicula (Corbiculina) portentosa',
                     'Hesperocorixa mandsshurica',
                     'Tipula (Yamatotipula) latemarginata latemarginata')) AS 저서_옛_학명_기록,
       (SELECT count(*) FROM BenthosSpeciesList WHERE SpeciesEn IN ('Koreoleptoxis nodifila',
                     'Koreoleptoxis globus',
                     'Semisulcospira calculus',
                     'Corbicula fluminalis',
                     'Corbicula insularis',
                     'Corbicula leana',
                     'Corbicula portentosa',
                     'Hesperocorixa mandshurica',
                     'Tipula (Yamatotipula) latemarginata')) AS 저서_새_학명_목록;


COMMIT;
