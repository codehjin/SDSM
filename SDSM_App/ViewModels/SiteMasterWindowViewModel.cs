using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using SDSM_App.Commons;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_Models;
using Telerik.Windows.Data;

namespace SDSM_App.ViewModels;

/// <summary>
/// 조사지점 관리 (ADMIN_TASKS §2-4).
/// 지점 마스터의 원본은 `sites.json` 이며 **이 화면은 DB에 쓰지 않는다.**
/// 좌표 변환은 `GB.ParseDmsString`/`GB.DmsToDegress`/`GB.DegressToDMS` 를 그대로 쓴다(중복 구현 금지).
/// </summary>
public partial class SiteMasterWindowViewModel : ObservableObject, ITransientService
{
    #region 필드

    private SiteCatalog _catalog = new();

    /// <summary>상세 폼 → 모델 되쓰기를 막아야 하는 구간(선택 전환 중).</summary>
    private bool _loadingDetail;

    #endregion

    #region 생성자

    public SiteMasterWindowViewModel()
    {
        ReloadExecuted();
    }

    #endregion

    #region 목록 · 트리

    /// <summary>대분류 → 사업장 → 과업대표하천 → 지점 4단계 트리.</summary>
    [ObservableProperty] private ObservableCollection<SiteTreeNode> _tree = [];

    /// <summary>트리에서 선택된 노드(지점 노드일 때만 상세가 열린다).</summary>
    [ObservableProperty] private SiteTreeNode? _selectedNode;

    /// <summary>지점명·설명·하천 부분일치 검색.</summary>
    [ObservableProperty] private string? _search;

    [ObservableProperty] private string _statusText = string.Empty;

    /// <summary>마스터 파일 경로(화면 안내용).</summary>
    public string MasterPath => SiteCatalogStore.MasterPath;

    /// <summary>사진 루트(화면 안내용).</summary>
    [ObservableProperty] private string _photoRoot = SiteCatalogStore.PhotoRoot;

    partial void OnSearchChanged(string? value) => BuildTree();

    partial void OnSelectedNodeChanged(SiteTreeNode? value)
    {
        if (value?.Site is SurveySite site) LoadDetail(site);
        else LoadDetail(null);

        SaveSiteExecutedCommand.NotifyCanExecuteChanged();
        DeleteSiteExecutedCommand.NotifyCanExecuteChanged();
        MergeSiteExecutedCommand.NotifyCanExecuteChanged();
        AddPhotoExecutedCommand.NotifyCanExecuteChanged();
    }

    /// <summary>병합 대상 후보(현재 선택 지점을 제외한 같은 대분류의 지점).</summary>
    [ObservableProperty] private List<SurveySite> _mergeCandidates = [];

    [ObservableProperty] private SurveySite? _mergeTarget;

    partial void OnMergeTargetChanged(SurveySite? value) => MergeSiteExecutedCommand.NotifyCanExecuteChanged();

    #endregion

    #region 상세 편집 항목

    /// <summary>지금 편집 중인 지점(트리의 실제 인스턴스).</summary>
    [ObservableProperty] private SurveySite? _current;

    [ObservableProperty] private string? _project;
    [ObservableProperty] private string? _workplace;
    [ObservableProperty] private string? _river;
    [ObservableProperty] private string? _siteGroup;
    [ObservableProperty] private string? _siteName;
    [ObservableProperty] private string? _stNo;
    [ObservableProperty] private string? _desc;
    [ObservableProperty] private int? _chasu;

    /// <summary>위도 입력 — 십진(`37.2122527`) · DMS(`37_12_44.11`, `37°12'44.11"N`) 모두 받는다.</summary>
    [ObservableProperty] private string? _latText;

    [ObservableProperty] private string? _lngText;

    /// <summary>정규화된 십진 좌표 미리보기.</summary>
    [ObservableProperty] private string _coordPreview = string.Empty;

    [ObservableProperty] private int? _validFrom;
    [ObservableProperty] private int? _validTo;

    partial void OnLatTextChanged(string? value) => UpdateCoordPreview();
    partial void OnLngTextChanged(string? value) => UpdateCoordPreview();
    partial void OnSiteNameChanged(string? value) => SaveSiteExecutedCommand.NotifyCanExecuteChanged();
    partial void OnProjectChanged(string? value) => SaveSiteExecutedCommand.NotifyCanExecuteChanged();

    /// <summary>적용 분류군 체크박스 7종.</summary>
    [ObservableProperty] private ObservableCollection<TaxonCheck> _taxaChecks = [];

    /// <summary>연도별 지점명 이력 — (연도구간, 표기) 2열 그리드.</summary>
    [ObservableProperty] private ObservableCollection<KeyValueRow> _yearAliases = [];

    /// <summary>연도별 지도 번호.</summary>
    [ObservableProperty] private ObservableCollection<KeyValueRow> _mapNumbers = [];

    /// <summary>지점 사진 목록.</summary>
    [ObservableProperty] private ObservableCollection<PhotoRow> _photos = [];

    [ObservableProperty] private PhotoRow? _selectedPhoto;

    partial void OnSelectedPhotoChanged(PhotoRow? value)
    {
        RemovePhotoExecutedCommand.NotifyCanExecuteChanged();
        OpenPhotoExecutedCommand.NotifyCanExecuteChanged();
    }

    /// <summary>지점 노드를 고른 상태에서만 상세 폼이 열린다.</summary>
    public bool HasCurrent => Current is not null;

    /// <summary>안내 문구용 역조건. Telerik BooleanToVisibilityConverter 는
    /// ConverterParameter 를 무시하므로(검증됨) 반대 조건을 속성으로 따로 둔다.</summary>
    public bool HasNoCurrent => Current is null;

    partial void OnCurrentChanged(SurveySite? value)
    {
        OnPropertyChanged(nameof(HasCurrent));
        OnPropertyChanged(nameof(HasNoCurrent));
    }

    #endregion

    #region 명령 — 파일

    /// <summary>마스터를 다시 읽는다.</summary>
    [RelayCommand]
    private void ReloadExecuted()
    {
        _catalog = SiteCatalogStore.Load();
        PhotoRoot = SiteCatalogStore.PhotoRoot;
        BuildTree();
        LoadDetail(null);

        StatusText = _catalog.Sites.Count == 0
            ? $"마스터가 비어 있습니다. [불러오기]로 기존 sites.json 을 읽어오세요.  ({MasterPath})"
            : $"{_catalog.Version} · 지점 {_catalog.Sites.Count}건";
    }

    /// <summary>다른 위치의 sites.json 을 읽어 마스터로 삼는다(조사자 앱 번들 등).</summary>
    [RelayCommand]
    private void ImportExecuted()
    {
        var dialog = new OpenFileDialog
        {
            Title = "지점 마스터 불러오기",
            Filter = "지점 마스터(*.json)|*.json"
        };
        if (dialog.ShowDialog() != true) return;

        var loaded = SiteCatalogStore.Load(dialog.FileName);
        if (loaded.Sites.Count == 0)
        {
            MessageBox.Show("지점이 없거나 형식이 맞지 않습니다.", "불러오기", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _catalog = loaded;
        BuildTree();
        LoadDetail(null);
        StatusText = $"불러옴 — {loaded.Version} · 지점 {loaded.Sites.Count}건 (아직 저장 전)";
    }

    /// <summary>마스터 파일에 저장한다.</summary>
    [RelayCommand]
    private void SaveCatalogExecuted()
    {
        try
        {
            SiteCatalogStore.Save(_catalog);
            StatusText = $"저장됨 — {_catalog.Version} · 지점 {_catalog.Sites.Count}건";
            WeakReferenceMessenger.Default.Send(new NotifyMessage((GB.SaveMessage, true)));
        }
        catch (IOException ex)
        {
            GB.LogWrite(GetType().Name, nameof(SaveCatalogExecuted), ex.Message);
            MessageBox.Show("파일이 실행중인지 확인하세요.", "파일 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(SaveCatalogExecuted), ex.Message);
            MessageBox.Show(GB.ErrorMessage, "저장 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>조사자 앱 배포용 `sites.json` 으로 내보낸다(기준자료 내보내기와 같은 방식).</summary>
    [RelayCommand]
    private void ExportExecuted()
    {
        var dialog = new SaveFileDialog
        {
            Title = "지점 마스터 내보내기",
            Filter = "지점 마스터(*.json)|*.json",
            FileName = "sites.json"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            SiteCatalogStore.Save(_catalog, dialog.FileName);
            StatusText = $"내보냄 — {Path.GetFileName(dialog.FileName)} · 지점 {_catalog.Sites.Count}건";
            WeakReferenceMessenger.Default.Send(new NotifyMessage(("지점 마스터를 내보냈습니다.", true)));
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(ExportExecuted), ex.Message);
            MessageBox.Show(GB.ErrorMessage, "내보내기 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>사진 루트 폴더를 바꾼다.</summary>
    [RelayCommand]
    private void ChangePhotoRootExecuted()
    {
        var dialog = new OpenFolderDialog { Title = "지점 사진 루트 선택" };
        if (Directory.Exists(PhotoRoot)) dialog.InitialDirectory = PhotoRoot;
        if (dialog.ShowDialog() != true) return;

        Properties.Settings.Default.SitePhotoRoot = dialog.FolderName;
        Properties.Settings.Default.Save();
        PhotoRoot = SiteCatalogStore.PhotoRoot;
        RefreshPhotoRows();
    }

    #endregion

    #region 명령 — 지점 등록·수정·삭제·병합

    /// <summary>새 지점을 만든다. 선택 중인 지점의 상위 4단계를 기본값으로 물려준다.</summary>
    [RelayCommand]
    private void NewSiteExecuted()
    {
        var seed = Current;
        var site = new SurveySite
        {
            Project = seed?.Project ?? Project,
            Workplace = seed?.Workplace ?? Workplace,
            River = seed?.River ?? River,
            SiteGroup = seed?.SiteGroup ?? SiteGroup,
            SiteName = string.Empty
        };

        _catalog.Sites.Add(site);
        BuildTree();
        SelectSite(site);
        StatusText = "새 지점 — 지점명을 입력하고 [지점 저장]을 누르세요.";
    }

    private bool CanSaveSite() =>
        Current is not null
        && !string.IsNullOrWhiteSpace(SiteName)
        && !string.IsNullOrWhiteSpace(Project);

    /// <summary>상세 폼을 현재 지점에 반영한다.</summary>
    [RelayCommand(CanExecute = nameof(CanSaveSite))]
    private void SaveSiteExecuted()
    {
        if (Current is not SurveySite site) return;

        var name = SiteName!.Trim();

        // 같은 대분류 · 같은 지점명 · 겹치는 연도구간이면 식별이 되지 않는다.
        var clash = _catalog.Sites.FirstOrDefault(s =>
            !ReferenceEquals(s, site)
            && s.Project == Project
            && string.Equals(s.SiteName, name, StringComparison.Ordinal)
            && Overlaps(s.ValidFrom, s.ValidTo, ValidFrom, ValidTo));

        if (clash is not null)
        {
            var period = clash.PeriodText.Length == 0 ? "(연도 구간 없음)" : clash.PeriodText;
            MessageBox.Show(
                $"같은 대분류에 '{name}' 지점이 이미 있고 연도 구간이 겹칩니다. {period}{Environment.NewLine}" +
                "연도 구간을 나누거나 지점명을 다르게 하세요.",
                "지점 중복", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        site.Project = Trim(Project);
        site.Workplace = Trim(Workplace);
        site.River = Trim(River);
        site.SiteGroup = Trim(SiteGroup);
        site.SiteName = name;
        site.StNo = Trim(StNo);
        site.Desc = Trim(Desc);
        site.Chasu = Chasu;
        site.Lat = ParseCoord(LatText);
        site.Lng = ParseCoord(LngText);
        site.ValidFrom = ValidFrom;
        site.ValidTo = ValidTo;

        site.Taxa = TaxaChecks.Where(t => t.IsChecked).Select(t => t.Name).ToList();
        site.YearAliases = ToDictionary(YearAliases);
        site.MapNumbers = ToDictionary(MapNumbers);
        site.Photos = Photos.Select(p => p.Model).ToList();

        BuildTree();
        SelectSite(site);
        StatusText = $"'{site.DisplayWithPeriod}' 반영됨 — 파일에 남기려면 [마스터 저장]을 누르세요.";
    }

    private bool CanDeleteSite() => Current is not null;

    [RelayCommand(CanExecute = nameof(CanDeleteSite))]
    private void DeleteSiteExecuted()
    {
        if (Current is not SurveySite site) return;

        var answer = MessageBox.Show(
            $"'{site.DisplayWithPeriod}' 지점을 마스터에서 지울까요?",
            "지점 삭제", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
        if (answer != MessageBoxResult.OK) return;

        _catalog.Sites.Remove(site);
        BuildTree();
        LoadDetail(null);
        StatusText = $"'{site.SiteName}' 삭제됨 — 파일에 남기려면 [마스터 저장]을 누르세요.";
    }

    private bool CanMergeSite() => Current is not null && MergeTarget is not null && !ReferenceEquals(Current, MergeTarget);

    /// <summary>
    /// 선택 지점(남는 쪽)에 병합 대상(사라지는 쪽)을 합친다.
    /// 비어 있는 항목만 채우고, 목록형(분류군·이력·사진)은 합집합으로 만든다.
    /// ⚠ 지점명·좌표 등 이미 값이 있는 항목은 덮어쓰지 않는다.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMergeSite))]
    private void MergeSiteExecuted()
    {
        if (Current is not SurveySite keep || MergeTarget is not SurveySite drop) return;

        var answer = MessageBox.Show(
            $"'{drop.DisplayWithPeriod}' 를 '{keep.DisplayWithPeriod}' 에 합칩니다.{Environment.NewLine}" +
            $"빈 항목만 채우고 분류군·연도이력·사진은 합칩니다. '{drop.SiteName}' 은 목록에서 사라집니다.{Environment.NewLine}" +
            "계속할까요?",
            "지점 병합", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
        if (answer != MessageBoxResult.OK) return;

        keep.Workplace ??= drop.Workplace;
        keep.River ??= drop.River;
        keep.SiteGroup ??= drop.SiteGroup;
        keep.StNo ??= drop.StNo;
        keep.Desc ??= drop.Desc;
        keep.Chasu ??= drop.Chasu;
        keep.Lat ??= drop.Lat;
        keep.Lng ??= drop.Lng;
        keep.ValidFrom ??= drop.ValidFrom;
        keep.ValidTo ??= drop.ValidTo;

        keep.Taxa = keep.Taxa.Union(drop.Taxa).Distinct().ToList();
        foreach (var kv in drop.YearAliases) keep.YearAliases.TryAdd(kv.Key, kv.Value);
        foreach (var kv in drop.MapNumbers) keep.MapNumbers.TryAdd(kv.Key, kv.Value);
        foreach (var photo in drop.Photos)
            if (!keep.Photos.Any(p => p.RelPath == photo.RelPath)) keep.Photos.Add(photo);

        _catalog.Sites.Remove(drop);
        BuildTree();
        SelectSite(keep);
        StatusText = $"'{drop.SiteName}' 을 '{keep.SiteName}' 에 병합했습니다 — [마스터 저장] 필요.";
    }

    #endregion

    #region 명령 — 연도 이력 · 사진

    [RelayCommand] private void AddAliasExecuted() => YearAliases.Add(new KeyValueRow());
    [RelayCommand] private void RemoveAliasExecuted(KeyValueRow? row) { if (row is not null) YearAliases.Remove(row); }
    [RelayCommand] private void AddMapNumberExecuted() => MapNumbers.Add(new KeyValueRow());
    [RelayCommand] private void RemoveMapNumberExecuted(KeyValueRow? row) { if (row is not null) MapNumbers.Remove(row); }

    private bool CanAddPhoto() => Current is not null;

    /// <summary>사진을 골라 사진 루트로 복사하고 상대경로만 기록한다.</summary>
    [RelayCommand(CanExecute = nameof(CanAddPhoto))]
    private void AddPhotoExecuted()
    {
        if (Current is not SurveySite site) return;

        var dialog = new OpenFileDialog
        {
            Title = "지점 사진 추가",
            Filter = "이미지(*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
            Multiselect = true
        };
        if (dialog.ShowDialog() != true) return;

        int added = 0;
        foreach (var file in dialog.FileNames)
        {
            try
            {
                var rel = SiteCatalogStore.CopyPhoto(file, site);
                Photos.Add(new PhotoRow(new SitePhoto { RelPath = rel, Year = DateTime.Now.Year }));
                added++;
            }
            catch (IOException ex)
            {
                GB.LogWrite(GetType().Name, nameof(AddPhotoExecuted), ex.Message);
                MessageBox.Show($"'{Path.GetFileName(file)}' 복사에 실패했습니다. 파일이 실행중인지 확인하세요.",
                    "파일 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                GB.LogWrite(GetType().Name, nameof(AddPhotoExecuted), ex.Message);
            }
        }
        if (added > 0) StatusText = $"사진 {added}장 추가 — [지점 저장] 후 [마스터 저장]을 눌러야 기록됩니다.";
    }

    private bool CanUsePhoto() => SelectedPhoto is not null;

    /// <summary>목록에서만 뺀다. 복사해 둔 파일은 지우지 않는다(다른 지점이 참조할 수 있다).</summary>
    [RelayCommand(CanExecute = nameof(CanUsePhoto))]
    private void RemovePhotoExecuted()
    {
        if (SelectedPhoto is PhotoRow row) Photos.Remove(row);
    }

    /// <summary>원본 뷰어 — 기본 사진 앱으로 연다.</summary>
    [RelayCommand(CanExecute = nameof(CanUsePhoto))]
    private void OpenPhotoExecuted()
    {
        if (SelectedPhoto is not PhotoRow row) return;
        if (!row.Exists)
        {
            MessageBox.Show("사진 파일을 찾을 수 없습니다.", "사진", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(row.FullPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(OpenPhotoExecuted), ex.Message);
        }
    }

    #endregion

    #region 매서드

    /// <summary>대분류 → 사업장 → 과업대표하천 → 지점 트리를 다시 만든다.</summary>
    private void BuildTree()
    {
        var q = Search?.Trim();
        var sites = _catalog.Sites.AsEnumerable();
        if (!string.IsNullOrEmpty(q))
        {
            sites = sites.Where(s =>
                Has(s.SiteName, q) || Has(s.Desc, q) || Has(s.River, q)
                || Has(s.StNo, q) || Has(s.Workplace, q) || Has(s.SiteGroup, q));
        }

        var list = sites.ToList();
        var tree = new ObservableCollection<SiteTreeNode>();

        foreach (var byProject in list.GroupBy(s => s.Project ?? "(대분류 없음)").OrderBy(g => g.Key))
        {
            var pNode = new SiteTreeNode(byProject.Key, $"{byProject.Count()}");
            foreach (var byWorkplace in byProject.GroupBy(s => s.Workplace ?? "(사업장 없음)").OrderBy(g => g.Key))
            {
                var wNode = new SiteTreeNode(byWorkplace.Key, $"{byWorkplace.Count()}");
                foreach (var byRiver in byWorkplace.GroupBy(s => s.River ?? "(하천 없음)").OrderBy(g => g.Key))
                {
                    var rNode = new SiteTreeNode(byRiver.Key, $"{byRiver.Count()}");
                    foreach (var site in byRiver.OrderBy(s => s.SiteName, new NaturalNameComparer())
                                                 .ThenBy(s => s.ValidFrom ?? 0))
                        rNode.Children.Add(new SiteTreeNode(site));
                    wNode.Children.Add(rNode);
                }
                pNode.Children.Add(wNode);
            }
            tree.Add(pNode);
        }

        Tree = tree;
        static bool Has(string? v, string q) => (v ?? "").Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>트리에서 특정 지점 노드를 찾아 선택한다.</summary>
    private void SelectSite(SurveySite site)
    {
        foreach (var node in Flatten(Tree))
        {
            if (ReferenceEquals(node.Site, site))
            {
                node.IsExpandedPath();
                SelectedNode = node;
                return;
            }
        }
        LoadDetail(site);
    }

    private static IEnumerable<SiteTreeNode> Flatten(IEnumerable<SiteTreeNode> nodes)
    {
        foreach (var n in nodes)
        {
            yield return n;
            foreach (var c in Flatten(n.Children)) yield return c;
        }
    }

    /// <summary>지점 → 상세 폼.</summary>
    private void LoadDetail(SurveySite? site)
    {
        _loadingDetail = true;
        try
        {
            Current = site;

            Project = site?.Project;
            Workplace = site?.Workplace;
            River = site?.River;
            SiteGroup = site?.SiteGroup;
            SiteName = site?.SiteName;
            StNo = site?.StNo;
            Desc = site?.Desc;
            Chasu = site?.Chasu;
            LatText = site?.Lat?.ToString("0.######");
            LngText = site?.Lng?.ToString("0.######");
            ValidFrom = site?.ValidFrom;
            ValidTo = site?.ValidTo;

            TaxaChecks = new ObservableCollection<TaxonCheck>(
                TaxonNames.Select(n => new TaxonCheck(n, site?.Taxa.Contains(n) == true)));

            YearAliases = new ObservableCollection<KeyValueRow>(
                site?.YearAliases.Select(kv => new KeyValueRow { Key = kv.Key, Value = kv.Value }) ?? []);
            MapNumbers = new ObservableCollection<KeyValueRow>(
                site?.MapNumbers.Select(kv => new KeyValueRow { Key = kv.Key, Value = kv.Value }) ?? []);
            Photos = new ObservableCollection<PhotoRow>(
                site?.Photos.Select(p => new PhotoRow(p)) ?? []);
            SelectedPhoto = null;

            MergeCandidates = site is null
                ? []
                : _catalog.Sites.Where(s => !ReferenceEquals(s, site) && s.Project == site.Project)
                                .OrderBy(s => s.SiteName, new NaturalNameComparer()).ToList();
            MergeTarget = null;

            UpdateCoordPreview();
        }
        finally
        {
            _loadingDetail = false;
        }
    }

    private void RefreshPhotoRows()
    {
        foreach (var row in Photos) row.Refresh();
    }

    /// <summary>십진 또는 DMS 문자열 → 십진수. `GB` 의 기존 변환기를 쓴다.</summary>
    private static double? ParseCoord(string? text)
    {
        var t = text?.Trim();
        if (string.IsNullOrEmpty(t)) return null;

        if (double.TryParse(t, out var dec)) return Math.Round(dec, 6);

        var (deg, min, sec) = GB.ParseDmsString(t);
        if (deg is int d && min is int m && sec is double s)
            return Math.Round(GB.DmsToDegress(d, m, s, t), 6);

        return null;
    }

    private void UpdateCoordPreview()
    {
        if (_loadingDetail && Current is null) { CoordPreview = string.Empty; return; }

        var lat = ParseCoord(LatText);
        var lng = ParseCoord(LngText);
        if (lat is null && lng is null) { CoordPreview = string.Empty; return; }

        // 저장되는 값은 항상 십진수다. DMS 로 입력해도 여기서 십진으로 정규화된 결과를 보여 준다.
        CoordPreview = $"저장값(십진) — 위도 {lat?.ToString("0.######") ?? "—"} · 경도 {lng?.ToString("0.######") ?? "—"}"
                     + (lat is null && !string.IsNullOrWhiteSpace(LatText) ? "   ⚠ 위도 형식을 읽지 못했습니다" : "")
                     + (lng is null && !string.IsNullOrWhiteSpace(LngText) ? "   ⚠ 경도 형식을 읽지 못했습니다" : "");
    }

    private static string? Trim(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private static Dictionary<string, string> ToDictionary(IEnumerable<KeyValueRow> rows)
    {
        var dict = new Dictionary<string, string>();
        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r.Key)) continue;
            dict[r.Key.Trim()] = (r.Value ?? string.Empty).Trim();
        }
        return dict;
    }

    /// <summary>두 연도 구간이 겹치는지. 한쪽이라도 구간이 없으면 겹치는 것으로 본다.</summary>
    private static bool Overlaps(int? aFrom, int? aTo, int? bFrom, int? bTo)
    {
        var a1 = aFrom ?? int.MinValue; var a2 = aTo ?? int.MaxValue;
        var b1 = bFrom ?? int.MinValue; var b2 = bTo ?? int.MaxValue;
        return a1 <= b2 && b1 <= a2;
    }

    /// <summary>적용 분류군 7종 — 조사자 앱 탭과 같은 순서·표기.</summary>
    private static readonly string[] TaxonNames =
        ["어류", "저서동물", "조류", "포유류", "양서파충류", "서식수변", "수질"];

    #endregion
}

#region 화면 전용 보조 모델

/// <summary>4단계 트리 노드. 지점 노드만 <see cref="Site"/> 를 갖는다.</summary>
public partial class SiteTreeNode : ObservableObject
{
    public SiteTreeNode(string title, string? badge = null)
    {
        Title = title;
        Badge = badge;
        IsExpanded = true;
    }

    public SiteTreeNode(SurveySite site)
    {
        Site = site;
        Title = site.DisplayWithPeriod;
        Badge = site.Desc;
    }

    public SurveySite? Site { get; }

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string? _badge;
    [ObservableProperty] private bool _isExpanded;

    public ObservableCollection<SiteTreeNode> Children { get; } = [];

    /// <summary>지점 노드인지(상세 편집 대상).</summary>
    public bool IsSite => Site is not null;

    /// <summary>선택 시 상위 노드가 접혀 있지 않도록.</summary>
    public void IsExpandedPath() => IsExpanded = true;
}

/// <summary>분류군 체크박스 1개.</summary>
public partial class TaxonCheck(string name, bool isChecked) : ObservableObject
{
    public string Name { get; } = name;
    [ObservableProperty] private bool _isChecked = isChecked;
}

/// <summary>(연도구간, 표기) 2열 그리드 행.</summary>
public partial class KeyValueRow : ObservableObject
{
    [ObservableProperty] private string? _key;
    [ObservableProperty] private string? _value;
}

/// <summary>사진 1행 — 모델 + 파일 존재 여부 표시.</summary>
public partial class PhotoRow(SitePhoto model) : ObservableObject
{
    public SitePhoto Model { get; } = model;

    public string RelPath => Model.RelPath;
    public string FileName => Model.FileName;
    public string FullPath => SiteCatalogStore.FullPath(Model);

    public string? Caption
    {
        get => Model.Caption;
        set { Model.Caption = value; OnPropertyChanged(); }
    }

    public int? Year
    {
        get => Model.Year;
        set { Model.Year = value; OnPropertyChanged(); }
    }

    /// <summary>파일이 실제로 있는지. 없어도 목록에는 남기고 오류를 내지 않는다.</summary>
    public bool Exists => SiteCatalogStore.Exists(Model);

    public string StateText => Exists ? string.Empty : "(파일 없음)";

    public void Refresh()
    {
        OnPropertyChanged(nameof(Exists));
        OnPropertyChanged(nameof(StateText));
        OnPropertyChanged(nameof(FullPath));
    }
}

/// <summary>`평택2` 가 `평택10` 앞에 오도록 숫자 꼬리를 수치로 비교한다.</summary>
public sealed class NaturalNameComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        var (xp, xn) = Split(x); var (yp, yn) = Split(y);
        var byPrefix = string.Compare(xp, yp, StringComparison.Ordinal);
        return byPrefix != 0 ? byPrefix : xn.CompareTo(yn);
    }

    private static (string, int) Split(string? v)
    {
        v ??= string.Empty;
        int i = v.Length;
        while (i > 0 && char.IsDigit(v[i - 1])) i--;
        return (v[..i], i < v.Length && int.TryParse(v[i..], out var n) ? n : -1);
    }
}

#endregion
