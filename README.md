# SDSM — 삼성DS 모니터링 DB 통합관리 프로그램 (관리자)

하천 생태 모니터링 자료(어류·저서동물·조류·포유류·양서파충류·식생·식물상·서식수변·수질)의
수집·검증·저장·통계·보고를 담당하는 .NET 8 WPF 데스크톱 애플리케이션.

## ⚠ 두 저장소를 **같은 상위 폴더에 나란히** clone 해야 합니다

조사자 앱이 이 저장소의 공용 프로젝트를 **상대 경로로 참조**합니다.
폴더 배치가 아래와 다르면 조사자 앱이 빌드되지 않습니다.

```
<상위폴더>\
  SDSM\            ← 이 저장소 (관리자 · 공용 모델 · 공용 계산)
  SDSM_Surveyor\   ← 조사자 앱
```

```bash
cd <상위폴더>
git clone https://github.com/codehjin/SDSM.git
git clone https://github.com/codehjin/SDSM_Surveyor.git
```

> NuGet 패키지·git submodule 은 쓰지 않습니다. 유지보수 인력이 1인이라 배포·버전 관리 비용이
> 얻는 것보다 큽니다(`SDSM_Surveyor\docs\05_REFACTORING.md` §2-0의 결정).

## 프로젝트 구성

| 프로젝트 | 역할 |
|---|---|
| `SDSM_App` | 관리자 앱 본체 (WPF) |
| `SDSM_Models` | 순수 POCO 엔티티·모델. **앱·EF·UI 의존성을 넣지 않습니다.** |
| `SDSM_Core` | 관리자·조사자가 **함께 쓰는 생태 계산** (FAI·BMI·다양도지수·수질등급·HRI) |

`SDSM_Models` 와 `SDSM_Core` 는 조사자 앱도 참조합니다. **여기를 고치면 양쪽이 함께 바뀝니다.**

## 빌드

- Visual Studio 2026 / .NET 8 / Telerik UI for WPF 2026.2.520
- 시작 프로젝트: `SDSM_App`
- Telerik 은 로컬 설치 소스를 씁니다(`NuGet.Config` 에 등록됨)

## ⚠ 데이터 파일 주의

`SDSMDB.sqlite` 는 프로젝트에 포함되지 않고 **실행 폴더**(`SDSM_App\bin\...\net8.0-windows\`)에만 있습니다.
`bin` 을 지우면 **실제 조사 자료가 사라집니다.** 정리 전에 반드시 백업하세요.

지점 마스터(`sites.json`)도 실행 폴더에 있으며 **DB가 아니라 이 파일이 원본**입니다.

## 흰 화면일 때(1.1.2)

어떤 PC 에서는 WPF 가 그래픽카드로 그리지 못해 창이 하얗게만 보인다(지시서 §25 · 조사자 v0.5.2 와 같은 규칙).
그때는 **실행 폴더(`삼성DS모니터링DB통합관리프로그램.exe` 옆)에 이름이 `software_render` 인 빈 파일**을 만들고 다시 실행한다
(확장자 · 내용 · 대소문자 상관없음 — `software_render.txt` 도 된다 · `software_render_old.txt` 처럼 이름이 다르면 안 켜짐). 소프트웨어로 그린다.
지금 그리기 방식은 [프로그램 정보] 창 「화면 그리기」 줄에 보인다. 되돌리려면 파일을 지운다. 판정은 `Commons\RenderModeSwitch` 한 곳.

## 설치 폴더 안내(`읽어주세요.txt`)

사용 설명서는 앱 안(리소스)에 있어 흰 화면이면 열 수 없다. 그래서 `tools\publish.ps1` 이 압축 · 전체 설치본 모두
exe 옆에 `tools\읽어주세요.txt`(흰 화면일 때 · 사용 설명서 여는 곳 · 새 판으로 바꿀 때 `SDSMDB.sqlite` · `sites.json` 백업 · 연락처)를 넣는다.
메모장에서 한글이 깨지지 않게 **UTF-8 with BOM** 으로 둔다(게시 3단계가 같은 파일 · BOM 을 확인하고, `software_render*` 가 섞이면 멈춘다).

## 문서

| 문서 | 내용 |
|---|---|
| `CLAUDE.md` | 개발·아키텍처 표준 (필독) |
| `design.md` | UI·화면 규칙 |
| `docs\ADMIN_TASKS.md` | 관리자 앱 작업 목록 |
| `docs\_ecostatus_sites_extracted.md` | 생태현황 지점 추출·판정 결과 |
