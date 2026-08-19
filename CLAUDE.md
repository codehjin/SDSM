# CLAUDE.md — 개발 및 아키텍처 가이드라인

> 삼성DS 모니터링 DB 통합관리 프로그램 (SDSM)
> 하천 생태 모니터링 자료(어류·저서동물·조류·포유류·양서파충류·식생·식물상·서식수변·수질)의
> 수집·검증·저장·통계·보고를 담당하는 .NET 8 WPF 데스크톱 애플리케이션.
>
> 이 문서는 코드를 추가·수정할 때 반드시 따라야 하는 **개발 표준**이다.
> UI/화면 규칙은 `design.md`를 함께 참조할 것.

---

## 0. AI 협업 원칙 (가장 중요)

- 이 저장소의 **기존 패턴을 최우선**으로 따른다. 새 라이브러리·새 패턴 도입 전에는 반드시 제안하고 합의한다.
- 작성자(조형진)는 **생태 도메인 전문가이며 코딩은 보조를 받는다.** 코드 제안 시 *무엇을·왜* 바꾸는지 한국어로 간단히 설명한다.
- 생태 로직(지수 산출·등급·검증)은 **도메인 규칙이 절대 기준**이다. 임의로 “개선”하지 말고, 기존 계산식(엑셀 원식 주석 포함)을 그대로 유지한다.
- 파괴적 작업(대량 삭제, `bin` 정리, 스키마 변경, 마이그레이션)은 **실행 전 반드시 고지**한다. 특히 `SDSMDB.sqlite`는 프로젝트에 포함되지 않고 실행 폴더에만 존재하므로 `bin` 삭제 시 데이터가 사라진다.

---

## 1. 기술 스택 & 솔루션 구조

| 항목 | 값 |
|------|-----|
| 런타임 | **.NET 8.0** (`net8.0-windows`, WPF, `UseWPF=true`, Nullable enable, ImplicitUsings enable) |
| UI | **Telerik UI for WPF** (`Telerik.UI.for.Wpf.AllControls.Xaml` 2026.2.520, Windows11 테마) |
| MVVM | **CommunityToolkit.Mvvm** 8.x |
| ORM | **Entity Framework Core** 9.x + `Microsoft.EntityFrameworkCore.Sqlite` / `System.Data.SQLite` |
| DI | `Microsoft.Extensions.DependencyInjection` + **Scrutor**(어셈블리 스캔) |
| 아이콘 | **MahApps.Metro.IconPacks** |
| 기타 | Newtonsoft.Json, NinjaNye.SearchExtensions, RestoreWindowPlace, WindowsAPICodePack |

**프로젝트 구성 (2개):**

- **`SDSM_Models`** — 순수 POCO 엔티티/모델. 도메인 데이터 클래스만 둔다(예: `Fish`, `SiteDivision`, `ImportFishSpecies`, `*View`). 어트리뷰트(`[Table]`, `[DisplayName]`, `[Description]`)로 메타데이터를 표현한다. **이 프로젝트에는 앱/EF/UI 의존성을 넣지 않는다.**
- **`SDSM_App`** — WPF 앱 본체. `Views/`, `ViewModels/`, `Models/`(DbContext·Repository), `Helpers/`, `Commons/`, `Converters/`, `Behaviors/`, `Enums/`, `Styles/`, `Themes/`, `Assets/`, 그리고 전역 유틸 `GB.cs`.

폴더 역할을 벗어나는 클래스를 만들지 않는다(예: ViewModel 로직을 View 코드비하인드에 두지 않는다).

---

## 2. 아키텍처 원칙 — MVVM (CommunityToolkit.Mvvm)

### 2.1 ViewModel 작성 규칙

- 모든 ViewModel은 `ObservableObject`를 상속하고 `partial class`로 선언한다.
- 상태 속성은 **필드 + `[ObservableProperty]`** 로 선언한다(수동 `INotifyPropertyChanged` 금지).
- 명령은 **메서드 + `[RelayCommand]`** 로 선언한다. 실행 조건은 `CanExecute = nameof(CanXxx)` 로 연결한다.

```csharp
public partial class FishPageViewModel : ObservableObject, ITransientService
{
    [ObservableProperty] private RadObservableCollection<Fish> _modelCollection = [];

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveExecuted() { /* ... */ }

    private bool CanSave() => ErrorCount == 0 && ConvertedCount > 0;
}
```

- `[ObservableProperty]`가 생성하는 public 속성은 PascalCase(`_modelCollection` → `ModelCollection`)이다. XAML/코드에서는 생성된 public 속성명으로 바인딩·참조한다.
- 파생 값이 있으면 `partial void OnXxxChanged(...)` 훅을 사용한다.

### 2.2 View ↔ ViewModel 결합

- **View의 DataContext는 코드비하인드에서 DI로 주입**한다. XAML에서 `d:DataContext`(디자인타임) 외에는 VM을 직접 `new` 하지 않는다.

```csharp
public partial class FishPage : Page, ITransientService
{
    private readonly FishPageViewModel _vm;
    public FishPage()
    {
        InitializeComponent();
        _vm = App.Current.Services.GetRequiredService<FishPageViewModel>();
        DataContext = _vm;
    }
}
```

### 2.3 의존성 주입 (Scrutor + 마커 인터페이스)

- DI 등록은 **마커 인터페이스 기반 자동 스캔**이다(`App.ConfigureServices`). 수동 `services.AddXxx` 를 늘리지 않는다.
- 클래스에 수명주기 마커 인터페이스를 부여하면 자동 등록된다.

| 마커 인터페이스 | 수명 | 용도 예 |
|----------------|------|---------|
| `ITransientService` | Transient | 페이지/컨트롤 ViewModel, Repository, 창(Window) |
| `IScopedService` | Scoped | 스코프 단위 서비스 |
| `ISingletonService` | Singleton | `MainWindow`, `MainWindowViewModel` 등 앱 전역 1개 |

- 해석은 `App.Current.Services.GetRequiredService<T>()` 로 한다.
- 새 ViewModel/서비스/Repository/Window를 만들면 **알맞은 마커 인터페이스를 반드시 부여**한다(안 하면 DI 해석 실패).

### 2.4 화면 간 통신

- 느슨한 알림(토스트 등)은 **`WeakReferenceMessenger`** 를 사용한다.

```csharp
var toast = (GB.SaveMessage, true);
WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
```

- 전역 UI 상태(로딩/진행률/잠금/페이지크기)는 `GB`의 정적 관찰 속성(§4.4)에 둔다.

---

## 3. 데이터베이스 & Repository 패턴

### 3.1 DbContext

- 단일 컨텍스트 **`SDSMDbContext`** (`Models/SDSMDbContext.cs`). 연결 문자열은 항상 `GB.SqliteDataSource` 를 사용한다.
- SQLite 파일: `SDSMDB.sqlite`, 위치는 **실행 폴더(`AppDomain.CurrentDomain.BaseDirectory`)**. `foreign keys=true`.
- 뷰 엔티티는 `OnModelCreating`에서 `HasNoKey()` 로 등록한다(`FishView`, `BenthosView`, `TaxonCoordinateCountView` 등).
- 조사자료↔지점(`SiteDivision`) 등 자주 쓰는 참조는 `Navigation(...).AutoInclude()` 로 자동 포함한다.
- 새 엔티티 추가 시: ① `SDSM_Models`에 POCO + 어트리뷰트 → ② `SDSMDbContext`에 `DbSet<T>` → ③ 필요 시 `OnModelCreating` 매핑 → ④ Repository(§3.2).

### 3.2 Repository 패턴

- 표준 CRUD는 **`RepositoryBase<T>`** (`Models/Repositories/Bases/`)를 상속해 구현한다. 동기/비동기 쌍(`InsertAsync/Insert`, `UpdateAsync/Update`, `Delete*`, `GetById*`, `GetAll*`, `GetQueryable`)을 제공한다.
- 구체 Repository는 **주 생성자(primary constructor)** 스타일 한 줄로 만든다.

```csharp
public class FishRepository(SDSMDbContext context)
    : RepositoryBase<Fish>(context), IFishRepository { }
```

- 접근은 인터페이스로: `App.Current.Services.GetRequiredService<IFishRepository>()`.
- `RepositoryBase`의 규칙(그대로 유지):
  - 모든 메서드는 `try/catch`로 감싸고 실패 시 `GB.LogWrite(...)` 기록 후 `null!`/`false`/빈 컬렉션을 반환한다.
  - 수정/삭제는 `DbUpdateConcurrencyException`을 별도 처리해 “동시성 충돌” 안내 후 목록을 다시 불러오게 한다.

### 3.3 트랜잭션 & 대량 저장

- **부모 → 자식** 구조 저장은: 부모를 EF Core Repository(`InsertAsync`)로 저장해 PK를 얻은 뒤, **자식 다건은 원시 `SQLiteConnection` + `BeginTransaction()`** 로 일괄 INSERT 후 `Commit()` 한다(`BulkFishControlViewModel.SubBatchSave` 참조).
- 원시 SQL은 **반드시 파라미터 바인딩**(`AddWithValue("@X", model.X ?? (object)DBNull.Value)`)을 쓴다. 문자열 연결로 값 삽입 금지.
- 트랜잭션 블록은 `try/catch`로 감싸고, 실패 시 `GB.LogWrite` 후 **예외를 rethrow**해 상위에서 롤백/실패 처리되게 한다.

---

## 4. 코드 컨벤션

### 4.1 네이밍 · 스타일

- 타입/공개 멤버/메서드: **PascalCase**. private 필드: **`_camelCase`**. 지역 변수: camelCase.
- Nullable 참조형이 켜져 있으므로 `?`/`!`/`??`를 의도적으로 사용한다. 컬렉션 초기화는 컬렉션 식(`[]`, `[.. src]`)을 쓴다.
- `[RelayCommand]` 메서드는 `XxxExecuted` 네이밍, 조건은 `CanXxx` 네이밍을 유지한다.
- **주석은 한국어로 간결하게.** 도메인 의미(무엇을 계산/검증하는지)를 적는다. 특히 생태 지수는 **원본 엑셀 수식을 주석으로 병기**한다(기존 `GB.GetH`/`GetBMI` 스타일).

### 4.2 모델 어트리뷰트 규약 (리플렉션 기반 처리의 근간)

`SDSM_Models`의 속성에는 다음 어트리뷰트를 붙인다. `GB`의 여러 유틸이 이 어트리뷰트를 리플렉션으로 읽어 동작한다.

- `[Table("SiteDivision")]` — 테이블명 고정(단수/정확 일치).
- `[DisplayName("지점명")]` — 화면 표시명 & 엑셀 헤더 매칭 키(`GB.GetFieldDisplayName`, `ToDataTable`).
- `[Description("ID")]` — 필드 설명(`GB.GetFieldDescription`).
- `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` — PK 자동증가.

어트리뷰트 이름/문구를 바꾸면 매칭·내보내기 로직이 깨질 수 있으므로 변경 시 영향 범위를 확인한다.

### 4.3 `GB.cs` 공통 유틸 호출 방식

`GB`(static)는 전역 설정·유틸·도메인 계산·메시지·전역 상태의 단일 창구다. 중복 구현하지 말고 `GB`를 호출/확장한다.

- **로깅(필수 관례):** 모든 `catch`에서
  ```csharp
  GB.LogWrite(GetType().Name, nameof(메서드), ex.Message);
  ```
  (`RepositoryBase`처럼 static 문맥이면 `typeof(T).Name`). 로그는 실행폴더 `Logs/Log_yyyyMMdd.log`에 CSV 형태로 남는다.
- **변환/포맷:** 날짜(`ToDateFromInt`, `GetDiffTimeString`), 좌표 도분초(`DegressToDMS`, `DmsToDegress`, `ParseDmsString`), 백분율(`GetPercentage`), 파일크기(`BytesToString`), `List<T>→DataTable`(`ToDataTable`).
- **정규식/상수:** `GB.IntPattern`, `GB.DoublePattern`, `GB.MinDate`, `GB.DBName`, `GB.NoImagePath` 등 하드코딩 대신 사용.
- **메시지 상수:** 사용자 알림 문구는 `GB.SaveMessage`/`EditMessage`/`DeleteMessage`/`ErrorMessage` 등을 재사용(문구 일관성).
- **도메인 계산:** 생태 지수·수질 등급은 §7의 `GB` 메서드를 통해서만 산출한다.

### 4.4 전역 UI 상태

로딩/진행률/잠금/페이징은 `GB`의 정적 관찰 속성(`StaticPropertyChanged`)을 사용한다: `IsBusy`, `BusyContent`, `IsIndeterminate`, `ProgressValue`, `IsLockFloating`, `IsMinimal`, `CurrentPageSize`. 무거운 작업 전후로 이 값들을 설정한다(§6).

### 4.5 예외 처리 관례

- UI 흐름에서 파일 I/O는 `IOException`을 별도 안내(“파일이 실행중인지 확인하세요”).
- DB 접근은 `SqliteException`을 별도 안내(“데이터베이스 또는 테이블을 찾을 수 없습니다”).
- 그 외 일반 `Exception`은 `GB.LogWrite` 후 사용자에게는 일반화된 오류 메시지를 보여준다. **예외를 조용히 삼키지 않는다.**

---

## 5. 데이터 처리 — 일괄 입력(Excel Import) 원칙

대량등록 ViewModel(`ViewModels/BulkRegisters/Bulk*ControlViewModel.cs`)은 **동일한 파이프라인**을 따른다. 새 분류군 추가 시 이 구조를 복제한다.

### 5.1 표준 파이프라인

1. **파일 선택** — `OpenFileDialog`(`*.xlsx`). 파일명 키워드로 생태현황/방류하천 판별(`SiteDivisionHelper.CheckFileNameByKeywords`). 저장 이력 중복 경고(파일명 비교는 `Replace(" ","").ToLower()` 정규화).
2. **엑셀 로드** — Telerik `XlsxFormatProvider().Import(stream, null)`. **마지막 인자 `null`(타임아웃 무제한)은 최신 Telerik 필수 인자이므로 유지**한다. 무거운 로드는 `await Task.Run(...)`.
3. **시트 확인** — 시트명 매칭도 `Replace(" ","").ToLower()`.
4. **모델 변환** — `DataTableToModels(worksheet)` → `(List<Import*>, List<Import*Species>)`.
5. **검증** — `DataValidation(...)` / `SpeciesDataValidation(...)` 를 `await Task.Run`으로 실행. 리플렉션으로 속성을 순회하며 `StringBuilder`에 오류를 누적하고, 오류 문구는 **반드시 `GB.GetImportErrorText(prop, ErrorType.X)`** 로 생성한다.
6. **검수 결과 집계** — `ConvertedCount`, `ErrorCount = ...Count(m => m.ErrorContent.Length > 0)` (종목록도 동일).
7. **저장** — §5.3.

### 5.2 엄격한 검증 게이트

- **오류가 하나라도 있으면 저장 불가.** 저장 명령의 `CanExecute`는 다음을 만족해야 한다:
  ```csharp
  private bool CanSave() =>
      ErrorCount == 0 && ConvertedCount > 0 && ErrorSpeciesCount == 0;
  ```
- `SaveExecuted` 진입부에서도 같은 조건을 재확인(방어적)한다.
- `ErrorType`(`Enums`) 종류: `Unique`(필수누락), `ListType`(목록없음), `Conversion`(형변환실패), `SpeciesDuplicate`(종명중복), `Duplicate`(자료중복), `YearError`(연도오류), `SiteError`(지점 목록없음). 새 검증 사유가 필요하면 **먼저 `ErrorType`과 `GB.GetImportErrorText`를 확장**한다.

### 5.3 트랜잭션 처리 지침

- 부모(예: `Fish`)는 Repository `InsertAsync`로 저장 → 얻은 PK로 자식(예: `FishSpecies`)을 원시 SQLite 트랜잭션으로 일괄 저장(§3.3).
- 진행률은 `GB.ProgressValue`/`GB.BusyContent`로 표시. 저장 성공 시 `SavedFileInfo` 기록 + `WeakReferenceMessenger` 토스트 + `Reset()`.
- 저장 실패 시 사용자에게 오류 안내하고 커밋하지 않는다.

### 5.4 텍스트 정규화 규칙 (중요 — §7.3과 연결)

| 대상 | 처리 | 이유 |
|------|------|------|
| 파일명·시트명 비교 | `Replace(" ","").ToLower()` | 사용자 입력 편차 흡수 |
| 지점(Site)·코드성 값(길드/외래/보호종 코드 등) | `Replace(" ","").ToUpper()` | 코드 표준화(예: `O`,`SS`,`TS`,`RB`,`I`) |
| **분류학적 종명(SpeciesKo/En, 분류계급)** | **`Trim()`만** (대소문자·내부 공백 보존) | 종명은 원문 그대로가 식별자 |

---

## 6. 비동기 · 반응성

- UI를 멈추는 연산(엑셀 파싱, 검증, 대량 저장, 통계)은 **`await Task.Run(...)`** 으로 백그라운드 실행한다.
- 시작 시 `GB.IsBusy = true; GB.BusyContent = "...";`, 종료(성공/실패 포함) 시 `finally`에서 `GB.IsBusy = false;`로 반드시 원복한다.
- 진행률이 있는 작업은 `GB.IsIndeterminate = false; GB.ProgressValue = ...`.

---

## 7. 생태학적 데이터 처리 특별 지침

> 생태 계산·검증·통계·주석은 아래 3원칙을 **예외 없이** 지킨다. 도메인 규칙이 코드 스타일보다 우선한다.

### 7.1 데이터 중심주의 (객관적 실측 지표만)

- 코드·주석·변수명·산출 로직은 **객관적이고 실측에 기반한 현장 지표(descriptive field metrics)** 만을 기준으로 한다.
- **주관적·이론적 논쟁(예: 가장자리 효과 등 해석 담론)은 로직과 주석에서 철저히 배제**한다. “왜 이 종이 여기 사는가” 같은 해석이 아니라, “측정된 개체수/출현종수/지수 값”만 다룬다.
- 지수의 의미를 주석에 적을 때도 **계산 정의(무엇을 어떻게 셈하는가)** 만 기술한다.

### 7.2 지수 산출 로직 — 결측치(Null)와 ‘0’의 엄격한 구분

- **`null`(미조사/결측)과 `0`(실측된 부재)은 다른 값이다.** 절대 혼용하지 않는다.
  - 저장 단계: 개체수 등 정수 필드가 비어 있으면 **`null`로 저장**한다(0으로 채우지 않는다). (`SpeciesDataValidation`에서 빈 값 → `null`)
  - 계산 단계: 합/비율을 낼 때만 `IndividualCount ?? 0`으로 캐스팅한 뒤 **`Where(x => x > 0)`** 로 유효 개체만 사용한다(`GB.GetDI/GetH/GetR1/GetJ/GetBMI/GetFAI` 참조).
- 지수 계산 표준(모두 `GB`에 위치, 신규 계산도 여기 추가):
  - 어류 **FAI**(`GetFAI` + `GetM1~M8Value`), 저서동물 **BMI/우점도 DI/다양도 H'/풍부도 R1/균등도 J'/RankScorer**, 수질 등급(`GetBodGrade` 등).
  - **`RankScorer`**: 개체수 `0`이면 순위 계산에서 제외(반환 0), 0 초과만 백분위 순위로 등급화. 0과 결측을 뒤섞지 말 것.
- 수치 규칙:
  - 반올림은 **`Math.Round(..., MidpointRounding.AwayFromZero)`** 로 통일(엑셀과 동일 거동).
  - 분모 0, `double.IsNaN`, `double.IsInfinity`는 **명시적으로 가드**하고 정의된 기본값(0 또는 `null`)을 반환한다.
  - **조사불가 사유**(공백 제거 후 `접근불가/건천화/준설/공사중`)가 있으면 등급은 `"-"` 로 반환한다.
  - 원본 엑셀 수식이 있는 계산은 **수식을 주석으로 병기**하고 결과가 엑셀과 일치하도록 유지한다.

### 7.3 종 무결성 검증 — 완전 텍스트 일치

- 법정보호종(멸종위기 야생생물 Ⅰ·Ⅱ급 등), 고유종, 생태계교란생물, 천연기념물 등의 **종 목록 매핑은 종목록 DB(`FishSpeciesList` 등)와 정확히 일치**해야 한다.
- **분류학적 종명은 띄어쓰기·대소문자를 포함해 완벽히 일치**시킨다. 종명에는 `Trim()`(양끝 공백만 제거) 외의 정규화(공백제거/대문자화)를 적용하지 않는다.
- 반대로 **코드성 값**(외래=`O`, 내성길드=`TS/SS`, 섭식길드=`O/I`, 서식길드=`RB`, 보호종 표기 코드 등)은 `Replace(" ","").ToUpper()` 로 표준화해 비교한다.
- 종명이 목록에 없으면 임의로 추가/치환하지 말고 검증 오류로 처리하거나(신규 종목록 추가 흐름을 탈 때만 명시적으로 추가) 사용자에게 확인받는다.

---

## 8. 빌드 · 실행 환경

- **Visual Studio 2026**(v18) / **.NET 8** / **Telerik UI for WPF 2026.2.520**.
- Telerik 참조: `Telerik.UI.for.Wpf.AllControls.Xaml` (로컬 설치 소스 `Telerik UI for WPF 2026 Q2\Binaries\WPF80\NuGet`; `NuGet.Config`에 등록됨). 온라인 피드 전용의 `*.80.Xaml` 이름은 이 환경에서 복원되지 않으므로 사용하지 않는다.
- 시작 프로젝트: **`SDSM_App`**.
- **데이터 파일 주의:** `SDSMDB.sqlite`는 프로젝트에 포함되지 않고 실행 폴더(`bin\...\net8.0-windows\`)에만 존재한다. `bin` 폴더를 지우면 데이터가 사라지므로, 정리 전 **DB 백업**을 먼저 한다. (장기적으로는 마스터 DB를 프로젝트에 포함하고 “출력에 복사” 설정을 검토.)
- 로그 확인: 실행 폴더 `Logs/Log_yyyyMMdd.log`.

---

## 9. 새 기능 추가 체크리스트

1. 엔티티가 필요하면 `SDSM_Models`에 POCO + 어트리뷰트, `SDSMDbContext`에 `DbSet<T>`(+ 필요 시 매핑).
2. 데이터 접근은 `RepositoryBase<T>` 상속 Repository + 인터페이스(DI 마커) 추가.
3. ViewModel은 `ObservableObject` + `[ObservableProperty]`/`[RelayCommand]` + 마커 인터페이스.
4. View는 DI로 VM 주입(`DataContext = _vm`), UI 규칙은 `design.md` 준수.
5. 생태 계산은 `GB`에 추가하고 §7 규칙(Null/0 구분, 반올림, 가드, 종명 일치) 준수.
6. 일괄입력은 §5 파이프라인·검증 게이트·트랜잭션을 복제.
7. 예외는 `GB.LogWrite`로 기록, 사용자 메시지는 `GB` 상수 재사용.
8. 파괴적 변경·마이그레이션은 사전 고지.
