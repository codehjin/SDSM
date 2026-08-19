# design.md — UI/UX 및 화면 디자인 가이드라인

> 삼성DS 모니터링 DB 통합관리 프로그램 (SDSM) — WPF 화면 표준.
> 아키텍처/데이터 규칙은 `CLAUDE.md`를 함께 참조할 것.
> 이 문서의 목표: **일관된 Telerik Windows11 룩앤필**과 **바인딩 오류 없는 XAML**.

---

## 1. UI 프레임워크 원칙 — Telerik UI for WPF (Windows11 테마)

- 표준 컨트롤은 **Telerik(RadControls)** 를 우선 사용한다. 기본 WPF 컨트롤은 Telerik에 대응물이 없거나 단순 레이아웃일 때만 쓴다.
  - 목록/표: `RadGridView` (일반 WPF `DataGrid` 금지)
  - 도킹/레이아웃: `RadDocking`, `RadPane`
  - 입력: `RadWatermarkTextBox`, `RadNumericUpDown`, `RadComboBox`, `RadDateTimePicker`, `RadToggleButton`, `RadButton`
  - 문서: `RadPdfViewer`(+ `PdfDocumentSource`), 엑셀 처리는 `XlsxFormatProvider`/`RadSpreadsheet`
  - 알림/로딩: `RadSplashScreenManager`, `RadBusyIndicator`(전역 `GB.IsBusy` 연동)
- 컬렉션 바인딩은 **`RadObservableCollection<T>`** 를 사용한다(대량 변경 성능).

### 1.1 테마 초기화 (건드리지 말 것 / 확장만)

테마는 `App` 생성자에서 전역 1회 설정된다. 개별 화면에서 테마를 바꾸지 않는다.

```csharp
StyleManager.ApplicationTheme = new Windows11Theme();
Windows11Palette.Palette.FontFamily = (FontFamily)FindResource("DefaultFontFamily");
Windows11Palette.Palette.FontSize   = (double)TryFindResource("DefaultFontSize");
// PrimaryBorderColor / SelectedColor 등 팔레트 커스터마이징 …
```

- 다크/라이트 전환은 `App.SetLightModeThemes()` / `App.SetDarkModeThemes()` 를 통해서만 한다(팔레트 색상 일괄 조정).
- 폰트/폰트크기는 항상 리소스 키(`DefaultFontFamily`, `DefaultFontSize`)를 참조한다. 하드코딩 금지.

### 1.2 리소스 병합 순서 (App.xaml)

`App.xaml`의 `MergedDictionaries` 순서를 유지한다: **Colors → Telerik FontResources → 프로젝트 System/Telerik 테마 오버라이드(`Themes/*.xaml`) → Styles.xaml → TelerikStyles.xaml**. 전역 스타일/색은 여기 등록하고, 화면 로컬 리소스는 각 View의 `Resources`에 둔다.

---

## 2. 화면 레이아웃 구성 방식

- **셸:** `MainWindow`가 좌측 메뉴 + 우측 콘텐츠(`ContentFrame`, `Frame` 네비게이션) 구조. 페이지 전환은 `NavigationService`로 하고, 각 메뉴는 `MainWindowViewModel`의 `IsCheckedXxx` 토글 속성과 연동한다.
- **페이지/컨트롤 분리:** 큰 화면은 `Views/Pages/*Page.xaml`, 재사용 UI는 `Views/UserControls/**/*Control.xaml`. 컨트롤은 페이지에 조합한다.
- **표준 화면 골격:** 상단 툴바 영역(고정 높이 `RowDefinition`) + 본문(`*`). 상단 바는 `FormTopBorderStyle` 등 공용 스타일을 사용.
- 화면 상태(로딩/잠금)는 전역 `GB` 속성에 바인딩해 오버레이로 표시한다(§6).

---

## 3. 아이콘 & 디자인 요소

### 3.1 아이콘 — MahApps.Metro.IconPacks

- 네임스페이스 접두사 `icon`(관례)로 선언하고 `PackIcon*`을 사용한다.

```xml
xmlns:icon="http://metro.mahapps.com/winfx/xaml/iconpacks"
...
<icon:PackIconModern Kind="OfficeExcel" Width="14" Height="14" VerticalAlignment="Center"/>
<icon:PackIconMaterial Kind="Numeric1Box"/>
<icon:PackIconVaadinIcons Kind="InfoCircleOutline"
                          Foreground="{telerik:Windows11Resource ResourceKey=AccentBrush}"/>
```

- 실제 사용 팩: `PackIconMaterial`, `PackIconModern`, `PackIconVaadinIcons`. 기존 화면에서 쓰던 팩/`Kind`를 우선 재사용해 시각적 일관성을 지킨다.
- 아이콘 색은 가급적 **Telerik `Windows11Resource` 브러시**(예: `AccentBrush`, `PrimaryForegroundBrush`)에 바인딩한다(테마 전환 대응).

### 3.2 색상 · 테마 바인딩 — `Windows11Resource` 규칙

- 컨트롤의 색/코너/폰트/여백은 **하드코딩하지 말고** Telerik 동적 리소스 마크업을 사용한다:

```xml
Foreground="{telerik:Windows11Resource ResourceKey=PrimaryForegroundBrush}"
Background="{telerik:Windows11Resource ResourceKey=TertiaryBackgroundBrush}"
BorderBrush="{telerik:Windows11Resource ResourceKey=PrimaryBorderBrush}"
CornerRadius="{telerik:Windows11Resource ResourceKey=CornerRadius}"
FontFamily="{telerik:Windows11Resource ResourceKey=FontFamily}"
Padding="{telerik:Windows11Resource ResourceKey=DefaultControlPadding}"
```

- 자주 쓰는 키: `PrimaryForegroundBrush`, `SecondaryForegroundBrush`, `DisabledForegroundBrush`, `PrimaryBackgroundBrush`, `TertiaryBackgroundBrush`, `PrimaryBorderBrush`, `AccentBrush`, `CornerRadius`, `FontFamily`, `FontSize`, `DefaultControlPadding`, `DefaultControlMinHeight`, `DisabledOpacity`.
- **앱 고유 색**(차트/브랜드)은 `Styles/Colors.xaml`의 명명 브러시를 쓴다: `MaxBrush`(#f25022), `AvgBrush`(#8EC441), `MinBrush`(#5B9BD5)(차트 최대/평균/최소), `OpacityBackgroundBrush`(오버레이) 등. 새 브랜드 색이 필요하면 임의 리터럴 대신 `Colors.xaml`에 키로 추가한다.
- **원칙:** 테마 색은 `Windows11Resource`, 도메인/브랜드 색은 `Colors.xaml`. XAML 안에 `#RRGGBB` 리터럴을 직접 남기지 않는다.

---

## 4. XAML 작성 & DataBinding 규칙

### 4.1 View ↔ ViewModel 바인딩

- `DataContext`는 코드비하인드에서 DI로 주입된다(`CLAUDE.md §2.2`). XAML에서는 VM의 **`[ObservableProperty]` 생성 public 속성명**(예: `ModelCollection`)과 `[RelayCommand]` 생성 명령명(예: `SaveCommand`)에 바인딩한다.

```xml
<telerik:RadButton Command="{Binding SaveCommand}" Content="저장"/>
<telerik:RadGridView ItemsSource="{Binding ModelCollection}"/>
```

- 디자인타임 도우미가 필요하면 `d:DataContext`만 사용하고 런타임 인스턴스를 XAML에서 만들지 않는다.
- 값 변환은 `Converters/`의 기존 컨버터 또는 Telerik 컨버터(`telerik:BooleanToVisibilityConverter` 등)를 재사용한다.

### 4.2 `RelativeSource` / `ElementName` 주의 (바인딩 에러 방지)

- **`ElementName`** 은 대상이 **같은 네임스코프**에 있을 때만 유효하다. `ControlTemplate`/`DataTemplate`/`Popup` 내부에서 바깥 요소를 `ElementName`으로 참조하지 않는다(템플릿은 별도 네임스코프 → 바인딩 실패). 이 경우 `RelativeSource`를 쓴다.
- **템플릿 내부**에서 컨트롤 자신/속성 참조는 `TemplateBinding` 또는 `{Binding RelativeSource={RelativeSource TemplatedParent}, Path=...}` 를 쓴다.
- **첨부 속성**을 바인딩 `Path`에 쓸 때는 **괄호 표기**가 필수다:
  ```xml
  Value="{Binding RelativeSource={RelativeSource TemplatedParent},
                  Path=(helpers:ThemeHelper.PressedBackgroundBrush)}"
  ```
- 상위 요소 참조는 `RelativeSource={RelativeSource AncestorType=Window}`(또는 `AncestorType=telerik:RadGridView` 등) 형태로 타입을 명시한다. 예: 창 최대화 상태 바인딩
  ```xml
  <DataTrigger Binding="{Binding Path=WindowState,
                    RelativeSource={RelativeSource AncestorType=Window}}"
               Value="Maximized">
  ```
- 바인딩 실패가 잦은 자리는 **`Mode`를 명시**(`OneWay`/`TwoWay`)하고, 널/초기값 자리는 `FallbackValue`/`TargetNullValue`로 방어한다.
- 개발 중에는 **출력 창의 바인딩 오류(System.Windows.Data Error)** 를 0으로 유지하는 것을 목표로 한다.

### 4.3 스타일링

- 반복 스타일은 로컬 리소스가 아니라 `Styles/Styles.xaml`·`Styles/TelerikStyles.xaml`에 공용 `Style`(키 지정)로 두고 `{StaticResource ...}`로 참조한다.
- `Style`은 `TargetType`을 명시하고, 값은 §3.2의 동적 리소스를 세터로 사용한다.
- `FocusVisualStyle="{x:Null}"`, `SnapsToDevicePixels="True"` 등 기존 컨트롤 스타일의 관례를 따른다.

---

## 5. 팝업 및 창(Window) 구성

`ManualWindow.xaml`이 커스텀 창의 기준 예시다. 새 팝업/도구 창은 이 패턴을 따른다.

### 5.1 커스텀 `WindowChrome`

```xml
<WindowChrome.WindowChrome>
    <WindowChrome ResizeBorderThickness="8" CaptionHeight="40" UseAeroCaptionButtons="False"/>
</WindowChrome.WindowChrome>
```

- 기본 캡션 버튼을 끄고(`UseAeroCaptionButtons="False"`) **최소화/최대화/닫기 버튼을 커스텀**으로 배치한다. 동작은 코드비하인드에 둔다:
  ```csharp
  private void Minimize_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
  private void Maximize_Click(object s, RoutedEventArgs e) =>
      WindowState = WindowState != WindowState.Normal ? WindowState.Normal : WindowState.Maximized;
  private void Close_Click(object s, RoutedEventArgs e) => Close();
  ```
- 커스텀 캡션 위의 상호작용 요소에는 `WindowChrome.IsHitTestVisibleInChrome="True"` 를 지정해 클릭이 먹도록 한다.
- **최대화 시 클리핑 보정:** 창이 `Maximized`면 외곽 여백을 주는 트리거를 둔다.
  ```xml
  <DataTrigger Binding="{Binding WindowState, RelativeSource={RelativeSource AncestorType=Window}}"
               Value="Maximized">
      <Setter Property="Margin" Value="8"/>
  </DataTrigger>
  ```

### 5.2 컨트롤 템플릿 & 글리프

- 커스텀 버튼은 `telerik:RadButton`/`RadToggleButton`의 `ControlTemplate`을 재정의하고, 아이콘은 **`telerik:RadGlyph`** 로 그린다(`Glyph="{StaticResource GlyphArrowChevronRight}"` 등 글리프 키 사용).
- 상태 색(마우스오버/눌림/비활성)은 `helpers:ThemeHelper.*` 첨부 속성과 트리거로 제어하며, 값은 §3.2 동적 리소스에서 가져온다.
- 트리거에서 첨부 속성 바인딩은 §4.2의 괄호 표기를 지킨다.

### 5.3 창 생성 · 수명

- 창은 **DI로 생성**한다: `App.Current.Services.GetRequiredService<ManualWindow>()`. 창 클래스에는 `ITransientService` 마커를 부여한다.
- 이미 열려 있는지 확인 후 재사용/포커스한다(중복 오픈 방지):
  ```csharp
  var win = App.Current.Windows.OfType<ManualWindow>().FirstOrDefault();
  if (win != null) { win.WindowState = WindowState.Normal; win.Activate(); }
  else App.Current.Services.GetRequiredService<ManualWindow>().Show();
  ```
- 창 위치/크기는 `RestoreWindowPlace`로 저장·복원한다(`MainWindow`처럼 `WindowPlace.Register(this)`).
- 폰트/폰트크기는 창 루트에 `FontFamily="{StaticResource DefaultFontFamily}"`, `FontSize="{StaticResource DefaultFontSize}"` 를 지정한다.

### 5.4 알림 · 대화상자

- 로딩 스플래시는 `RadSplashScreenManager.Show<SplashScreenControl>()` / `Close()`.
- 단순 확인/경고는 `MessageBox`(제목·아이콘 지정), 저장/삭제 성공 토스트는 `WeakReferenceMessenger` + `NotifyMessage`(문구는 `GB.SaveMessage` 등 상수).
- 사용자 안내 문구는 임의로 만들지 말고 `GB`의 메시지 상수를 우선 사용한다.

---

## 6. 반응성 · 바쁨 표시 (전역 상태 바인딩)

- 로딩 오버레이/진행률은 전역 `GB` 정적 속성에 바인딩한다: `IsBusy`, `BusyContent`, `IsIndeterminate`, `ProgressValue`, `BusyIndicatorVisibility`, `BusyCancelVisibility`.
- 화면 잠금/메뉴 축소는 `IsLockFloating`, `IsMinimal`. 페이징 크기는 `CurrentPageSize`.
- 이 값들은 `GB.StaticPropertyChanged`로 변경 통지되므로, 바인딩만 걸어두면 UI가 자동 반영된다. **개별 화면에서 자체 로딩 플래그를 새로 만들지 말고 전역 상태를 사용**한다.

---

## 7. 신규 화면 체크리스트

1. 페이지/컨트롤을 `Views/`의 올바른 폴더에 만들고, VM은 DI 주입(`DataContext = _vm`) + 마커 인터페이스.
2. 표는 `RadGridView` + `RadObservableCollection`, 입력은 Rad* 컨트롤 사용.
3. 색/폰트/코너는 `Windows11Resource`, 브랜드/차트 색은 `Colors.xaml` 키 — 리터럴 금지.
4. 아이콘은 `MahApps IconPacks`(`PackIcon*`), 기존 팩/Kind 재사용.
5. 반복 스타일은 `Styles/*.xaml`에 공용 키로.
6. 바인딩은 `TemplatedParent`/`AncestorType`/첨부속성 괄호 규칙 준수 → 출력창 바인딩 오류 0 유지.
7. 커스텀 창은 `WindowChrome` + 커스텀 캡션 버튼 + 최대화 마진 보정 + DI 생성 + `RestoreWindowPlace`.
8. 로딩/진행은 전역 `GB` 상태에 바인딩.
