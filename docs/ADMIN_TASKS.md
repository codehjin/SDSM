# 관리자 앱(SDSM) 작업 목록

> 조사자 앱 계획은 `..\..\SDSM_Surveyor\docs\00_MASTER_PLAN.md` 참조.
> 기술 사실·엑셀 셀 매핑은 `..\..\SDSM_Surveyor\docs\90_TECH_NOTES.md` 에 정리되어 있다.

**역할 정의**: 조사자는 조사자 앱만 사용하고, **관리자 앱은 삼성 담당자의 취합·조회용**이다.

---

## 1. [높음] 조사자 보고서 엑셀 → 간편 가져오기

**배경**: 현행 일괄입력은 계산식이 잔뜩 든 복잡한 전치 엑셀을 요구한다. 조사자 앱이 만든 **깔끔한 보고서용 엑셀**을 그대로 받아 분류군 테이블에 넣는 경로가 필요하다.

- 조사자 보고서 엑셀 구조: `조사개황` / `출현종`(지점명 포함 1행1종) / `건강성평가`(1조사 1행)
  → 상세는 `..\..\SDSM_Surveyor\docs\03_FEATURE_EXPORT.md` §2
- 기존 `Bulk*ControlViewModel` 은 **그대로 두고**(레거시 병행), 별도 메뉴로 추가한다.
- 가져올 때 **지수·등급은 엑셀 값을 신뢰하지 말고 재계산**한다(현행 정책과 동일).
- 신규 종(조사자가 현장에서 추가한 종)은 종 마스터에 **검수 후 병합**할 수 있게 표시한다.

## 2. [중] 조사지점 관리 화면

- `SiteDivision`(Project·Workplace·WorkplaceRiver·WorkplaceSite) 기반 4단계 구분 정착
- 지점 등록·수정·**병합**·분류군(분야) 선택, **연도별 지점명 이력** 관리
  → 원천 데이터 구조는 `..\..\SDSM_Surveyor\docs\04_FEATURE_SITE_SESSION.md` §A-1
- **설정 창에 배치하되 평상시 비활성화, 관리자 권한에서만 활성화**
- 지점 마스터를 `sites.json` 으로 **내보내기**(조사자 앱이 이 파일로 지점 목록을 받는다)
  → 기존 [기준자료 내보내기(JSON)]와 동일한 방식으로 버튼 추가

## 3. [중] 종목록 내보내기 / 신규종 취합

- 어류·저서동물 마스터(`FishSpeciesList`·`BenthosSpeciesList`)를 `species.json` 으로 내보내기
  (현재 `species.json`은 DB에서 1회 생성한 상태 — 버튼으로 재생성 가능하게)
- ⚠ **관찰형 3종(조류·포유류·양서파충류)은 이제 관리자 DB가 아니라 국가생물종목록이 기준이다**(2026-08-20).
  - `SpeciesCatalog.Bird/Mammal/Amphibian` 타입이 `List<string>` → **`List<ObservedSpecies>`**(국명·학명·목·과·보호종·교란종·KTSN)로 바뀌었다.
  - 생성은 조사자 저장소의 `tools\build_species_json.py` 가 담당한다(국립생물자원관 목록 xlsx → species.json).
  - **관리자가 이 부분을 덮어쓰지 않도록 주의.** 관리자 내보내기는 `Fish`·`Benthos`만 갱신하고 관찰형 3종은 보존해야 한다.
  - 구형 파일(국명 문자열 배열)도 계속 읽힌다(`ObservedSpeciesListConverter` 하위 호환).
- 조사자가 추가한 신규 종(공식 목록에 없는 종)을 취합해 마스터에 병합하는 검수 화면

## 4. [중] 지도 지점 클릭 줌 제어

- 지점 클릭 시 과도하게 확대된다.
- 현재 확대 비율을 확인해 **클러스터(그룹)가 풀리는 단계까지만** 확대되도록 줌 레벨 조정.

## 6. [높음] FAI M8 계산 수정 + 과거 데이터 재산출 검토

**결정(2026-08-20): 수생태계 건강성 평가 지침에 따라 M8 비정상종 비율을 실수 나눗셈으로 고친다.**

### 6-1. 코드 수정 (`GB.cs`)
현행은 `abnormalCount`·`totalIndiv`가 모두 `int`라 **정수 나눗셈**이 되어 비율이 사실상 항상 0%다.
→ M8이 언제나 만점(12.5)이 되어 **비정상종 지표가 작동하지 않는다.**

```csharp
// 현행 (결함)
double abnormalRatio = abnormalCount != 0 ? (abnormalCount / totalIndiv) * 100 : 0;
// 수정
double abnormalRatio = totalIndiv > 0 ? (double)abnormalCount / totalIndiv * 100.0 : 0;
```
조사자 앱 `Ecology\EcologyCalculator.cs` 도 **같이** 고쳐야 값이 일치한다.
상세 근거·예시는 `..\..\SDSM_Surveyor\docs\90_TECH_NOTES.md` §5.

### 6-2. 과거 데이터 재산출 (사용자 판단 필요)

기존 `Fish` 테이블의 `FAIScore`·`FAIGrade`·`M8Value`는 **옛 계산(정수 나눗셈)으로 저장**되어 있다.
공식만 고치면 **올해 자료와 과거 자료의 산출 기준이 달라져 연도별 비교가 어긋난다.**

선택지:
- **(가) 과거 데이터 재산출** — 원자료(종별 개체수·DE/EF/LE/TU·하천차수)가 DB에 남아 있으므로 재계산 가능.
  전 연도 일괄 재산출 기능이 필요하다. **실행 전 DB 백업 필수.**
- **(나) 그대로 두고 기록만** — 어느 연도부터 기준이 바뀌었는지 명시. 비교 시 주의가 필요하다.

> ⚠ 사용자(생태 전문가)의 결정 사항이다. 임의로 재산출하지 말 것.
> 재산출을 택하면 변경 전후 값을 대조표로 남겨 어떤 지점의 등급이 바뀌었는지 확인할 수 있게 한다.

---

## 5. [낮음] SDSM_Core 참조 교체

- 조사자 앱 리팩토링(`05_REFACTORING.md`)에서 `SDSM_Core` 공용 라이브러리를 만든다.
- 관리자 `GB.cs`를 `SDSM_Core` 호출로 교체한다.
- **주의**: 결과가 완전히 같은지 확인한 뒤 `GB.cs`를 제거한다. `GB.cs`의 정수 나눗셈 등 특이 동작을 보존해야 값이 일치한다.

---

## 이미 완료된 것

- **기준자료 내보내기(JSON)**: 설정 화면 → 종별 과거 개체수 min/max 산출(`Commons\ReferenceDataExporter.cs`) → 조사자 앱의 이상치 경고에 사용
- 공유 모델 추가: `SDSM_Models\ReferenceData.cs`, `SDSM_Models\SpeciesCatalog.cs`
- VS 2026 / Telerik 2026.2.520 마이그레이션

---

## 주의사항

- `SDSM_App\bin\Debug\net8.0-windows\SDSMDB.sqlite` 는 **사용자의 실데이터**다. bin 폴더를 삭제·이동하지 말 것.
- 일괄입력 엑셀 셀 매핑을 임의로 바꾸면 기존 제출 파일이 깨진다. 변경 시 `90_TECH_NOTES.md` §4를 함께 갱신할 것.
