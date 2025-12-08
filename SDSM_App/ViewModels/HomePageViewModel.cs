using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using SDSM_Models;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Telerik.Windows.Controls.Map;
using Telerik.Windows.Data;

namespace SDSM_App.ViewModels;

/// <summary>
/// 홈
/// </summary>
public partial class HomePageViewModel : ObservableObject, ITransientService
{
    #region 필드

    //조류와 포유류 중 목록이 2개 이상인 경우 그중 아래 목록이 포함된 종이 라벨에 표시
    private readonly List<Char> _featureChars = ['Ⅰ', '1', 'Ⅱ', '2', '멸', '천', '하', '적'];

    #endregion

    #region 생성자

    public HomePageViewModel()
    {
        // 이벤트 연결 추가
        _latLngChangeSelectedItems.CollectionChanged += LatLngChangeSelectedItems_CollectionChanged;

        InitialLoad();
    }

    #endregion

    #region 속성

    [ObservableProperty] private double _zoom = 9;
    [ObservableProperty] private Location _center = new(37.031062, 127.044525);

    [ObservableProperty] private string _degress = string.Empty;

    [ObservableProperty] private List<MapDivision> _divisionData = [];
    [ObservableProperty] private CollectionViewSource _divisionDataView = new();

    [ObservableProperty] private RadObservableCollection<MapModel> _pointCollection = [];

    [ObservableProperty] private RadObservableCollection<MapData> _popupPointCollection = [];
    [ObservableProperty] private RadObservableCollection<ContentItem> _themeMapCollection = [];

    #endregion

    #region 검색 속성 및 ICommand 구현   

    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private RadObservableCollection<SearchItem> _searchValueCollection = [];
    [ObservableProperty] private RadObservableCollection<SearchItem> _searchValueSelectionCollection = [];

    //검색 자료 변경으로 인한 검색어 목록 변경, 검색어 값 초기화
    [RelayCommand]
    private async Task SearchSelectionChangedExecuted()
    {
        GB.IsBusy = true;

        try
        {
            IsSearching = SearchValueSelectionCollection.Count > 0;

            await GetPoints();
        }
        catch { }
        finally
        {
            GB.IsBusy = false;
        }
    }

    #endregion

    #region 불투명도 속성

    //비오톱 희소성 
    [ObservableProperty] private double _biotopesRarityOpacity = 1.0;
    [ObservableProperty] private double _invasiveSpeciesOpacity = 1.0;

    #endregion

    #region 좌표변경 속성 and ICommand 구현

    [ObservableProperty] private RadObservableCollection<MapData> _latLngChangeCollection = [];

    private RadObservableCollection<MapData> _latLngChangeSelectedItems = [];
    public RadObservableCollection<MapData> LatLngChangeSelectedItems
    {
        get => _latLngChangeSelectedItems;
        set
        {
            if (_latLngChangeSelectedItems != null)
                _latLngChangeSelectedItems.CollectionChanged -= LatLngChangeSelectedItems_CollectionChanged;

            SetProperty(ref _latLngChangeSelectedItems, value);

            if (_latLngChangeSelectedItems != null)
                _latLngChangeSelectedItems.CollectionChanged += LatLngChangeSelectedItems_CollectionChanged;
        }
    }

    private void LatLngChangeSelectedItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        LatLngChangeSelectionCount = _latLngChangeSelectedItems.Count;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LatLngChangedSaveExecutedCommand))]
    private int _latLngChangeSelectionCount;

    [ObservableProperty] private Visibility _latLngChangeViewVisibility = Visibility.Collapsed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LatLngChangedSaveExecutedCommand))]
    private double? _beforeLat;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LatLngChangedSaveExecutedCommand))]
    private double? _beforeLng;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LatLngChangedSaveExecutedCommand))]
    private double? _afterLat;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LatLngChangedSaveExecutedCommand))]
    private double? _afterLng;

    [RelayCommand]
    private void LatLngChangeViewOpenExected(MapModel model)
    {
        if (LatLngChangeCollection.Count != 0)
            LatLngChangeCollection.Clear();

        if (LatLngChangeSelectedItems.Count != 0)
            LatLngChangeSelectedItems.Clear();

        if (model == null)
            return;

        BeforeLat = model.Location.Latitude;
        BeforeLng = model.Location.Longitude;
        LatLngChangeCollection.AddRange([.. PopupPointCollection]);
        LatLngChangeViewVisibility = Visibility.Visible;
    }

    [RelayCommand]
    private void LatLngChangeViewCloseExected()
    {
        LatLngChangeViewVisibility = Visibility.Collapsed;
        LatLngChangeReset();
    }

    //위경도 변경 속성 초기화
    [RelayCommand]
    private void LatLngChangeResetExecuted()
    {
        LatLngChangeReset();
    }

    private bool CanLatLngChangedSave()
    {
        return BeforeLat != null
            && BeforeLng != null
            && AfterLat != null
            && AfterLng != null
            && LatLngChangeSelectionCount > 0;
    }

    //위경도 변경 저장
    [RelayCommand(CanExecute = nameof(CanLatLngChangedSave))]
    private void LatLngChangedSaveExecuted()
    {
        GB.IsBusy = true;

        try
        {
            var groups = LatLngChangeSelectedItems.GroupBy(m => m.Caption)
                                                   .Select(g => new { Caption = g.Key, Ids = g.Select(x => x.Id).ToList() })
                                                   .ToList();

            using var context = new SDSMDbContext();

            foreach (var group in groups)
            {
                switch (group.Caption)
                {
                    case "어류":
                        var fishList = context.Fishs.Where(f => group.Ids.Contains(f.FishId)).ToList();
                        foreach (var fish in fishList)
                        {
                            fish.Lat = AfterLat;
                            fish.Lng = AfterLng;
                        }
                        break;
                    case "저서동물":
                        var benthosList = context.Benthoss.Where(b => group.Ids.Contains(b.BenthosId)).ToList();
                        foreach (var benthos in benthosList)
                        {
                            benthos.Lat = AfterLat;
                            benthos.Lng = AfterLng;
                        }
                        break;
                    case "조류":
                        var birdList = context.Birds.Where(b => group.Ids.Contains(b.BirdId)).ToList();
                        foreach (var bird in birdList)
                        {
                            bird.Lat = AfterLat;
                            bird.Lng = AfterLng;
                        }
                        break;
                    case "포유류":
                        var mammalList = context.Mammals.Where(m => group.Ids.Contains(m.MammalId)).ToList();
                        foreach (var mammal in mammalList)
                        {
                            mammal.Lat = AfterLat;
                            mammal.Lng = AfterLng;
                        }
                        break;
                    case "식물상":
                        var plantList = context.Plants.Where(m => group.Ids.Contains(m.PlantId)).ToList();
                        foreach (var plant in plantList)
                        {
                            plant.Lat = AfterLat;
                            plant.Lng = AfterLng;
                        }
                        break;
                    case "식생단면":
                        var veENVList = context.VegetationENVs.Where(m => group.Ids.Contains(m.VegetationENVId)).ToList();
                        foreach (var veENV in veENVList)
                        {
                            veENV.StartLat = AfterLat;
                            veENV.StartLng = AfterLng;
                        }
                        break;
                    case "양서파충류":
                        var arList = context.AmphibianReptiles.Where(m => group.Ids.Contains(m.AmphibianReptileId)).ToList();
                        foreach (var ar in arList)
                        {
                            ar.Lat = AfterLat;
                            ar.Lng = AfterLng;
                        }
                        break;
                    case "서식수변":
                        var habitatList = context.HabitatWaterEdges.Where(h => group.Ids.Contains(h.HabitatWaterEdgeId)).ToList();
                        foreach (var habitat in habitatList)
                        {
                            habitat.Lat = AfterLat;
                            habitat.Lng = AfterLng;
                        }
                        break;
                    case "수질":
                        var waterList = context.WaterQualitys.Where(w => group.Ids.Contains(w.WaterQualityId)).ToList();
                        foreach (var water in waterList)
                        {
                            water.Lat = AfterLat;
                            water.Lng = AfterLng;
                        }
                        break;
                }
            }

            context.SaveChanges();

            ResetExecutedCommand.Execute(null);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(LatLngChangedSaveExecuted), ex.Message);
        }
        finally
        {
            GB.IsBusy = false;
        }
    }

    #endregion

    #region ICommand 구현

    [RelayCommand]
    private async Task ResetExecuted()
    {
        GB.IsBusy = true;

        try
        {
            if (SearchValueSelectionCollection.Count != 0)
                SearchValueSelectionCollection.Clear();

            IsSearching = false;
            LatLngChangeViewCloseExectedCommand.Execute(null);

            await GetPoints();
        }
        catch { }
        finally
        {
            GB.IsBusy = false;
        }
    }

    [RelayCommand]
    private void MapDivisionChecked(object obj)
    {
        SetMapPoints();
    }

    [RelayCommand]
    private void MapDivisionUnchecked(object obj)
    {
        SetMapPoints();
    }

    //선택 팝업 목록
    [RelayCommand]
    private void PopupPointsExecuted(MapModel model)
    {
        if (PopupPointCollection.Count != 0)
            PopupPointCollection.Clear();

        var items = PointCollection.Where(m => m.Location.Latitude == model.Location.Latitude && m.Location.Longitude == model.Location.Longitude)
                                    .OrderBy(m => m.Division)
                                    .ThenBy(m => m.Caption)
                                    .ToList();

        foreach (var item in items)
        {
            PopupPointCollection.AddRange(item.Data);
        }
    }

    #endregion

    #region 매서드

    private async void InitialLoad()
    {
        GB.IsBusy = true;

        try
        {
            GetThemeMapData();

            var tasks = new[] { GetSearchValueData(), GetPoints() };

            await Task.WhenAll(tasks);
        }
        catch { }
        finally
        {
            GB.IsBusy = false;
        }
    }

    //검색 자료 목록
    private async Task GetSearchValueData()
    {
        try
        {
            //식물상, 식생단면 제외한 전체 종 목록 조회
            await using var context = new SDSMDbContext();

            var species = new List<string>();

            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT SpeciesKo FROM View_SpeciesUnion";
                command.CommandType = System.Data.CommandType.Text;

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (!reader.IsDBNull(0))
                    {
                        var value = reader.GetString(0);
                        if (!string.IsNullOrWhiteSpace(value))
                            species.Add(value);
                    }
                }
            }

            var allSpecies = species.Distinct().OrderBy(s => s).ToList();

            SearchValueCollection.Clear();
            if (allSpecies.Count > 0)
                SearchValueCollection.AddRange(allSpecies.Select(s => new SearchItem { SearchText = s }));
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetSearchValueData), ex.Message);
            throw;
        }
    }

    //주제도 목록
    private void GetThemeMapData()
    {
        try
        {
            var themeMaps = new List<ContentItem>();
            themeMaps =
            [
                new() {
                        Caption = "비오톱 희소성",
                        MbTilesName = "주제도_비오톱_희소성",
                        OpacityName = "BiotopesRarityOpacity",
                        IsBiotopesRarity = true,
                        ContentSubItems =
                            [
                                new() { Grade = "1등급", Count = 16_978, Area = 96155556.7779988/1_000_000, MarkerFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2b83ba")), IsBiotopesRarity = true },
                                new() { Grade = "2등급", Count = 48_588, Area = 106250153.032994/1_000_000, MarkerFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#abdda4")), IsBiotopesRarity = true },
                                new() { Grade = "3등급", Count = 53_149, Area = 101646920.864996/1_000_000, MarkerFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffffbf")) , IsBiotopesRarity = true},
                                new() { Grade = "4등급", Count = 95_496, Area = 148402707.012997/1_000_000, MarkerFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fdae61")), IsBiotopesRarity = true },
                                new() { Grade = "5등급", Count = 129_749, Area = 121767846.826001/1_000_000, MarkerFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d7191c")), IsBiotopesRarity = true },
                            ],
                    },
                    new() {
                        Caption = "생태계교란종",
                        MbTilesName = "주제도_생태계교란종",
                        OpacityName = "InvasiveSpeciesOpacity",
                        IsInvasiveSpecies =true,
                        ContentSubItems =
                            [
                                new() { Grade = "가시박", Count = 93, MarkerFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2b83ba")), IsInvasiveSpecies =true },
                                new() { Grade = "가시상추", Count = 111, MarkerFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5ea7b1")), IsInvasiveSpecies =true },
                                new() { Grade = "단풍잎돼지풀", Count = 105, MarkerFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#91cba9")), IsInvasiveSpecies =true },
                                new() { Grade = "도깨비가지", Count = 3, MarkerFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bce4aa")), IsInvasiveSpecies =true },
                                new() { Grade = "돼지풀", Count = 24, MarkerFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#def2b4")), IsInvasiveSpecies =true },
                                new() { Grade = "미국쑥부쟁이", Count = 93, MarkerFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffffbf")), IsInvasiveSpecies =true },
                                new() { Grade = "서양금혼초", Count = 2, MarkerFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffdf9a")), IsInvasiveSpecies =true },
                                new() { Grade = "애기수영", Count = 1, MarkerFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#febe74")), IsInvasiveSpecies =true },
                                new() { Grade = "양미역취", Count = 2, MarkerFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f69053")), IsInvasiveSpecies =true },
                                new() { Grade = "환삼덩굴", Count = 217, MarkerFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e75437")), IsInvasiveSpecies =true },
                            ],
                    },
            ];

            //주제도 지도
            ThemeMapCollection.AddRange(themeMaps);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetThemeMapData), ex.Message);
            throw;
        }
    }

    //조사자료 포인트
    private async Task GetPoints()
    {
        using var context = new SDSMDbContext();

        try
        {
            // 선택된 검색어 목록 (null/빈 문자열 제외)
            var searchValues = SearchValueSelectionCollection.Select(m => m.SearchText).Where(s => !string.IsNullOrEmpty(s)).ToList();

            if (PointCollection.Count != 0)
                PointCollection.Clear();

            var projects = new[] { "방류하천", "생태현황" };
            var mapDivisions = new List<MapDivision>();

            foreach (var project in projects)
            {
                var caption = "어류";
                var pointData = new List<MapModel>();
                var division = project == "방류하천" ? "방" : "생";

                //어류
                IQueryable<Fish> fishQuery = context.Fishs.AsNoTracking().Where(m => m.SiteDivision.Project == project && m.Lat != null && m.Lng != null);

                // 검색 항목이 있으면 Subs에 해당 종이 포함된 Fish 항목만 가져오도록 필터
                if (searchValues.Count > 0)
                {
                    fishQuery = fishQuery.Where(m => m.Subs.Any(s => s.SpeciesKo != null && searchValues.Contains(s.SpeciesKo)));
                }

                var fishGroups = await fishQuery.Include(m => m.Subs).GroupBy(m => new { m.Lat, m.Lng }).Select(g => new { Items = g.OrderBy(m => m.SurveyYear).ThenBy(m => m.SurveyDate).ToList() }).ToListAsync();

                var fishCount = fishGroups.Sum(g => g.Items.Count);

                foreach (var group in fishGroups)
                {
                    var items = group.Items;
                    string? label = null;
                    var lat = items.FirstOrDefault()?.Lat ?? 0;
                    var lng = items.FirstOrDefault()?.Lng ?? 0;
                    var mapData = new List<MapData>();

                    if (items.Count > 1)
                    {
                        var firstValue = $"{ProcessSurveyYear(items.First().SurveyYear):00}";
                        var lastValue = $"{ProcessSurveyYear(items.Last().SurveyYear):00}";

                        label = firstValue == lastValue ? $"{division}_어({firstValue})" : $"{division}_어({firstValue}~{lastValue})";
                    }
                    else
                    {
                        label = $"{division}_어({ProcessSurveyYear(items.First().SurveyYear):00})";
                    }

                    foreach (var item in items)
                    {
                        var subs = item.Subs.GroupBy(m => m.SpeciesKo)
                                             .Select(g => new NameValue { Name = g.Key ?? "", Value = g.Sum(x => x.IndividualCount ?? 0) })
                                             .OrderByDescending(m => m.Value)
                                             .ThenBy(m => m.Name)
                                             .ToList();

                        var subSpecies = string.Join(",", subs.Select(s => s.Name));

                        var data = new MapData
                        {
                            Id = item.FishId,
                            Caption = caption,
                            Project = item.SiteDivision.Project,
                            Workplace = item.SiteDivision.Workplace,
                            WorkplaceRiver = item.SiteDivision.WorkplaceRiver,
                            YearChsu = item.YearChsu,
                            SurveyDate = item.SurveyDate,
                            River = item.River,
                            Site = item.Site,
                            Lat = item.Lat,
                            Lng = item.Lng,
                            Surveyor = item.Surveyor,
                            Score = item.FAIScore,
                            Grade = item.FAIGrade,
                            SpeciesCount = item.Subs.Count,
                            IndividualCount = item.Subs.Sum(m => m.IndividualCount),
                            SubSpecies = subSpecies,
                            SubSpeciesData = subs,
                        };
                        mapData.Add(data);
                    }

                    var model = new MapModel
                    {
                        Location = new Location(lat, lng),
                        Division = $"{division}_어",
                        Caption = caption,
                        Label = label,
                        Data = mapData,
                        IsScoreGrade = true,
                        IsSpeciesCount = true,
                        IsIndividualCount = true,
                        IsSubSpecies = true,
                    };

                    pointData.Add(model);
                }

                if (IsSearching == false || (IsSearching && fishQuery.Any()))
                {
                    mapDivisions.Add(new MapDivision
                    {
                        Project = project,
                        Division = $"{division}_어",
                        Caption = caption,
                        DataCount = fishCount,
                        Subs = new RadObservableCollection<MapModel>(pointData)
                    });
                }

                //저서동물
                caption = "저서동물";
                pointData.Clear();

                IQueryable<Benthos> benthosQuery = context.Benthoss.AsNoTracking().Where(m => m.SiteDivision.Project == project && m.Lat != null && m.Lng != null);

                // 검색 항목이 있으면 Subs에 해당 종이 포함된 Fish 항목만 가져오도록 필터
                if (searchValues.Count > 0)
                {
                    benthosQuery = benthosQuery.Where(m => m.Subs.Any(s => s.SpeciesKo != null && searchValues.Contains(s.SpeciesKo)));
                }

                var benthosGroups = await benthosQuery.Include(m => m.Subs).GroupBy(m => new { m.Lat, m.Lng }).Select(g => new { Items = g.OrderBy(m => m.SurveyYear).ThenBy(m => m.SurveyDate).ToList() }).ToListAsync();

                var benthosCount = benthosGroups.Sum(g => g.Items.Count);

                foreach (var group in benthosGroups)
                {
                    var items = group.Items;
                    string? label = null;
                    var lat = items.FirstOrDefault()?.Lat ?? 0;
                    var lng = items.FirstOrDefault()?.Lng ?? 0;
                    var mapData = new List<MapData>();

                    if (items.Count > 1)
                    {
                        var firstValue = $"{ProcessSurveyYear(items.First().SurveyYear):00}";
                        var lastValue = $"{ProcessSurveyYear(items.Last().SurveyYear):00}";

                        label = firstValue == lastValue ? $"{division}_저({firstValue})" : $"{division}_저({firstValue}~{lastValue})";
                    }
                    else
                    {
                        label = $"{division}_저({ProcessSurveyYear(items.First().SurveyYear):00})";
                    }

                    foreach (var item in items)
                    {
                        var subs = item.Subs.GroupBy(m => m.SpeciesKo)
                                             .Select(g => new NameValue { Name = g.Key ?? "", Value = g.Sum(x => x.IndividualCount ?? 0) })
                                             .OrderByDescending(m => m.Value)
                                             .ThenBy(m => m.Name)
                                             .ToList();

                        var subSpecies = string.Join(",", subs.Select(s => s.Name));

                        var data = new MapData
                        {
                            Id = item.BenthosId,
                            Caption = caption,
                            Project = item.SiteDivision.Project,
                            Workplace = item.SiteDivision.Workplace,
                            WorkplaceRiver = item.SiteDivision.WorkplaceRiver,
                            YearChsu = item.YearChsu,
                            SurveyDate = item.SurveyDate,
                            River = item.River,
                            Site = item.Site,
                            Lat = item.Lat,
                            Lng = item.Lng,
                            Surveyor = item.Surveyor,
                            Score = item.BMIScore,
                            Grade = item.BMIGrade,
                            SpeciesCount = item.Subs.Count,
                            IndividualCount = item.Subs.Sum(m => m.IndividualCount),
                            SubSpecies = subSpecies,
                            SubSpeciesData = subs,
                        };
                        mapData.Add(data);
                    }

                    var model = new MapModel
                    {
                        Location = new Location(lat, lng),
                        Division = $"{division}_저",
                        Caption = caption,
                        Label = label,
                        Data = mapData,
                        IsScoreGrade = true,
                        IsSpeciesCount = true,
                        IsIndividualCount = true,
                        IsSubSpecies = true,
                    };

                    pointData.Add(model);
                }

                if (IsSearching == false || (IsSearching && benthosQuery.Any()))
                {
                    mapDivisions.Add(new MapDivision
                    {
                        Project = project,
                        Division = $"{division}_저",
                        Caption = caption,
                        DataCount = benthosCount,
                        Subs = new RadObservableCollection<MapModel>(pointData)
                    });
                }

                //조류
                caption = "조류";
                pointData.Clear();

                IQueryable<Bird> birdQuery = context.Birds.AsNoTracking().Where(m => m.SiteDivision.Project == project && m.Lat != null && m.Lng != null);

                // 검색 항목이 있으면 Subs에 해당 종이 포함된 Fish 항목만 가져오도록 필터
                if (searchValues.Count > 0)
                {
                    birdQuery = birdQuery.Where(m => !string.IsNullOrWhiteSpace(m.SpeciesKo) && searchValues.Contains(m.SpeciesKo));
                }

                var birdGroups = await birdQuery.GroupBy(m => new { m.Lat, m.Lng }).Select(g => new { Items = g.OrderBy(m => m.SurveyYear).ThenBy(m => m.SurveyDate).ToList() }).ToListAsync();

                var birdCount = birdGroups.Sum(g => g.Items.Count);

                foreach (var group in birdGroups)
                {
                    var items = group.Items;
                    string? label = null;
                    var lat = items.FirstOrDefault()?.Lat ?? 0;
                    var lng = items.FirstOrDefault()?.Lng ?? 0;
                    var mapData = new List<MapData>();
                    var firstSpeciesKo = "";

                    if (items.Count > 1)
                    {
                        var hasMultipleSpeciess = items.Select(i => i.SpeciesKo).Distinct().Count() > 1;
                        var abbr = hasMultipleSpeciess ? "..." : "";

                        var featureSpeciesKo = items.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m.Feature) && m.Feature.Any(c => _featureChars.Contains(c)))?.SpeciesKo ?? "";
                        if (featureSpeciesKo != "")
                        {
                            firstSpeciesKo = $" : {featureSpeciesKo}{abbr}";
                        }
                        else
                        {
                            firstSpeciesKo = items.First() != null ? $" : {items.First()?.SpeciesKo ?? ""}{abbr}" : "";
                        }

                        var firstValue = $"{ProcessSurveyYear(items.First().SurveyYear):00}";
                        var lastValue = $"{ProcessSurveyYear(items.Last().SurveyYear):00}";

                        label = firstValue == lastValue ? $"{division}_조({firstValue}){firstSpeciesKo}" : $"{division}_조({firstValue}~{lastValue}){firstSpeciesKo}";
                    }
                    else
                    {
                        firstSpeciesKo = items.First() != null ? $" : {items.First()?.SpeciesKo ?? ""}" : "";

                        label = $"{division}_조({ProcessSurveyYear(items.First().SurveyYear):00}){firstSpeciesKo}";
                    }

                    foreach (var item in items)
                    {
                        var data = new MapData
                        {
                            Id = item.BirdId,
                            Caption = caption,
                            Project = item.SiteDivision.Project,
                            Workplace = item.SiteDivision.Workplace,
                            WorkplaceRiver = item.SiteDivision.WorkplaceRiver,
                            YearChsu = item.YearChsu,
                            SurveyDate = item.SurveyDate,
                            River = item.River,
                            Site = item.Site,
                            Lat = item.Lat,
                            Lng = item.Lng,
                            Surveyor = item.Surveyor,
                            SpeciesKo = item.SpeciesKo,
                            SpeciesEn = item.SpeciesEn,
                            IndividualCount = item.IndividualCount,
                        };
                        mapData.Add(data);
                    }

                    var model = new MapModel
                    {
                        Location = new Location(lat, lng),
                        Division = $"{division}_조",
                        Caption = caption,
                        Label = label,
                        Data = mapData,
                        IsSpecies = true,
                        IsIndividualCount = true,
                    };

                    pointData.Add(model);
                }

                if (IsSearching == false || (IsSearching && birdQuery.Any()))
                {
                    mapDivisions.Add(new MapDivision
                    {
                        Project = project,
                        Division = $"{division}_조",
                        Caption = caption,
                        DataCount = birdCount,
                        Subs = new RadObservableCollection<MapModel>(pointData)
                    });
                }

                //포유류
                caption = "포유류";
                pointData.Clear();

                IQueryable<Mammal> mammalQuery = context.Mammals.AsNoTracking().Where(m => m.SiteDivision.Project == project && m.Lat != null && m.Lng != null);

                // 검색 항목이 있으면 Subs에 해당 종이 포함된 Fish 항목만 가져오도록 필터
                if (searchValues.Count > 0)
                {
                    mammalQuery = mammalQuery.Where(m => !string.IsNullOrWhiteSpace(m.SpeciesKo) && searchValues.Contains(m.SpeciesKo));
                }

                var mammalGroups = await mammalQuery.GroupBy(m => new { m.Lat, m.Lng }).Select(g => new { Items = g.OrderBy(m => m.SurveyYear).ThenBy(m => m.SurveyDate).ToList() }).ToListAsync();

                var mammalCount = mammalGroups.Sum(g => g.Items.Count);

                foreach (var group in mammalGroups)
                {
                    var items = group.Items;
                    string? label = null;
                    var lat = items.FirstOrDefault()?.Lat ?? 0;
                    var lng = items.FirstOrDefault()?.Lng ?? 0;
                    var mapData = new List<MapData>();
                    var firstSpeciesKo = "";
                    

                    if (items.Count > 1)
                    {
                        var hasMultipleSpeciess = items.Select(i => i.SpeciesKo).Distinct().Count() > 1;
                        var abbr = hasMultipleSpeciess ? "..." : "";

                        var featureSpeciesKo = items.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m.Feature) && m.Feature.Any(c => _featureChars.Contains(c)))?.SpeciesKo ?? "";
                        if (featureSpeciesKo != "")
                        {
                            firstSpeciesKo = $" : {featureSpeciesKo}{abbr}";
                        }
                        else
                        {
                            firstSpeciesKo = items.First() != null ? $" : {items.First()?.SpeciesKo ?? ""}{abbr}" : "";
                        }

                        var firstValue = $"{ProcessSurveyYear(items.First().SurveyYear):00}";
                        var lastValue = $"{ProcessSurveyYear(items.Last().SurveyYear):00}";

                        label = firstValue == lastValue ? $"{division}_포({firstValue}){firstSpeciesKo}" : $"{division}_포({firstValue}~{lastValue}){firstSpeciesKo}";
                    }
                    else
                    {
                        firstSpeciesKo = items.First() != null ? $" : {items.First()?.SpeciesKo ?? ""}" : "";

                        label = $"{division}_포({ProcessSurveyYear(items.First().SurveyYear):00}){firstSpeciesKo}";
                    }
                   
                    foreach (var item in items)
                    {
                        var data = new MapData
                        {
                            Id = item.MammalId,
                            Caption = caption,
                            Project = item.SiteDivision.Project,
                            Workplace = item.SiteDivision.Workplace,
                            WorkplaceRiver = item.SiteDivision.WorkplaceRiver,
                            YearChsu = item.YearChsu,
                            SurveyDate = item.SurveyDate,
                            River = item.River,
                            Site = item.Site,
                            Lat = item.Lat,
                            Lng = item.Lng,
                            Surveyor = item.Surveyor,
                            SpeciesKo = item.SpeciesKo,
                            SpeciesEn = item.SpeciesEn,
                        };
                        mapData.Add(data);
                    }

                    var model = new MapModel
                    {
                        Location = new Location(lat, lng),
                        Division = $"{division}_포",
                        Caption = caption,
                        Label = label,
                        Data = mapData,
                        IsSpecies = true,
                    };

                    pointData.Add(model);
                }

                if (IsSearching == false || (IsSearching && mammalQuery.Any()))
                {
                    mapDivisions.Add(new MapDivision
                    {
                        Project = project,
                        Division = $"{division}_포",
                        Caption = caption,
                        DataCount = mammalCount,
                        Subs = new RadObservableCollection<MapModel>(pointData)
                    });
                }

                //양서파충류
                caption = "양서파충류";
                pointData.Clear();

                IQueryable<AmphibianReptile> arQuery = context.AmphibianReptiles.AsNoTracking().Where(m => m.SiteDivision.Project == project && m.Lat != null && m.Lng != null);

                // 검색 항목이 있으면 Subs에 해당 종이 포함된 Fish 항목만 가져오도록 필터
                if (searchValues.Count > 0)
                {
                    arQuery = arQuery.Where(m => !string.IsNullOrWhiteSpace(m.SpeciesKo) && searchValues.Contains(m.SpeciesKo));
                }

                var arGroups = await arQuery.GroupBy(m => new { m.Lat, m.Lng }).Select(g => new { Items = g.OrderBy(m => m.SurveyYear).ThenBy(m => m.SurveyDate).ToList() }).ToListAsync();

                var arCount = arGroups.Sum(g => g.Items.Count);

                foreach (var group in arGroups)
                {
                    var items = group.Items;
                    string? label = null;
                    var lat = items.FirstOrDefault()?.Lat ?? 0;
                    var lng = items.FirstOrDefault()?.Lng ?? 0;
                    var mapData = new List<MapData>();

                    if (items.Count > 1)
                    {
                        var firstValue = $"{ProcessSurveyYear(items.First().SurveyYear):00}";
                        var lastValue = $"{ProcessSurveyYear(items.Last().SurveyYear):00}";

                        label = firstValue == lastValue ? $"{division}_양({firstValue})" : $"{division}_양({firstValue}~{lastValue})";
                    }
                    else
                    {
                        label = $"{division}_양({ProcessSurveyYear(items.First().SurveyYear):00})";
                    }

                    foreach (var item in items)
                    {
                        var data = new MapData
                        {
                            Id = item.AmphibianReptileId,
                            Caption = caption,
                            Project = item.SiteDivision.Project,
                            Workplace = item.SiteDivision.Workplace,
                            WorkplaceRiver = item.SiteDivision.WorkplaceRiver,
                            YearChsu = item.YearChsu,
                            SurveyDate = item.SurveyDate,
                            River = item.River,
                            Site = item.Site,
                            Lat = item.Lat,
                            Lng = item.Lng,
                            Surveyor = item.Surveyor,
                            SpeciesKo = item.SpeciesKo,
                            SpeciesEn = item.SpeciesEn,
                        };
                        mapData.Add(data);
                    }

                    var model = new MapModel
                    {
                        Location = new Location(lat, lng),
                        Division = $"{division}_양",
                        Caption = caption,
                        Label = label,
                        Data = mapData,
                        IsSpecies = true,
                    };

                    pointData.Add(model);
                }

                if (IsSearching == false || (IsSearching && arQuery.Any()))
                {
                    mapDivisions.Add(new MapDivision
                    {
                        Project = project,
                        Division = $"{division}_양",
                        Caption = caption,
                        DataCount = arCount,
                        Subs = new RadObservableCollection<MapModel>(pointData)
                    });
                }

                //식물상
                caption = "식물상";
                pointData.Clear();

                IQueryable<Plant> plantQuery = context.Plants.AsNoTracking().Where(m => m.SiteDivision.Project == project && m.Lat != null && m.Lng != null);

                // 검색 항목이 있으면 Subs에 해당 종이 포함된 Fish 항목만 가져오도록 필터
                if (searchValues.Count > 0)
                {
                    plantQuery = plantQuery.Where(m => !string.IsNullOrWhiteSpace(m.SpeciesKo) && searchValues.Contains(m.SpeciesKo));
                }

                var plantGroups = await plantQuery.GroupBy(m => new { m.Lat, m.Lng }).Select(g => new { Items = g.OrderBy(m => m.SurveyYear).ThenBy(m => m.SurveyDate).ToList() }).ToListAsync();

                var plantCount = plantGroups.Sum(g => g.Items.Count);

                foreach (var group in plantGroups)
                {
                    var items = group.Items;
                    string? label = null;
                    var lat = items.FirstOrDefault()?.Lat ?? 0;
                    var lng = items.FirstOrDefault()?.Lng ?? 0;
                    var mapData = new List<MapData>();

                    if (items.Count > 1)
                    {
                        var firstValue = $"{ProcessSurveyYear(items.First().SurveyYear):00}";
                        var lastValue = $"{ProcessSurveyYear(items.Last().SurveyYear):00}";

                        label = firstValue == lastValue ? $"{division}_식물({firstValue})" : $"{division}_식물({firstValue}~{lastValue})";
                    }
                    else
                    {
                        label = $"{division}_식물({ProcessSurveyYear(items.First().SurveyYear):00})";
                    }

                    foreach (var item in items)
                    {
                        var data = new MapData
                        {
                            Id = item.PlantId,
                            Caption = caption,
                            Project = item.SiteDivision.Project,
                            Workplace = item.SiteDivision.Workplace,
                            WorkplaceRiver = item.SiteDivision.WorkplaceRiver,
                            YearChsu = item.Chsu,
                            SurveyDate = item.SurveyDate,
                            River = item.River,
                            Site = item.Site,
                            Lat = item.Lat,
                            Lng = item.Lng,
                            Surveyor = item.Surveyor,
                            SpeciesKo = item.SpeciesKo,
                            SpeciesEn = item.SpeciesEn,
                        };
                        mapData.Add(data);
                    }

                    var model = new MapModel
                    {
                        Location = new Location(lat, lng),
                        Division = $"{division}_식물",
                        Caption = caption,
                        Label = label,
                        Data = mapData,
                        IsSpecies = true,
                    };

                    pointData.Add(model);
                }

                if (IsSearching == false || (IsSearching && plantQuery.Any()))
                {
                    mapDivisions.Add(new MapDivision
                    {
                        Project = project,
                        Division = $"{division}_식물",
                        Caption = caption,
                        DataCount = plantCount,
                        Subs = new RadObservableCollection<MapModel>(pointData)
                    });
                }

                //식생단면
                caption = "식생단면";
                pointData.Clear();

                IQueryable<VegetationENV> veENVQuery = context.VegetationENVs.AsNoTracking().Where(m => m.SiteDivision.Project == project && m.StartLat != null && m.StartLng != null);

                // 검색 항목이 있으면 Subs에 해당 종이 포함된 Fish 항목만 가져오도록 필터
                if (searchValues.Count > 0)
                {
                    veENVQuery = veENVQuery.Where(m => m.Subs.Any(s => s.SpeciesKo != null && searchValues.Contains(s.SpeciesKo)));
                }

                var veENVGroups = await veENVQuery.Include(m => m.Subs).GroupBy(m => new { m.StartLat, m.StartLng }).Select(g => new { Items = g.OrderBy(m => m.SurveyYear).ThenBy(m => m.SurveyDate).ToList() }).ToListAsync();

                var veENVCount = veENVGroups.Sum(g => g.Items.Count);

                foreach (var group in veENVGroups)
                {
                    var items = group.Items;
                    string? label = null;
                    var lat = items.FirstOrDefault()?.StartLat ?? 0;
                    var lng = items.FirstOrDefault()?.StartLng ?? 0;
                    var mapData = new List<MapData>();

                    if (items.Count > 1)
                    {
                        var firstValue = $"{ProcessSurveyYear(items.First().SurveyYear):00}";
                        var lastValue = $"{ProcessSurveyYear(items.Last().SurveyYear):00}";

                        label = firstValue == lastValue ? $"{division}_식단({firstValue})" : $"{division}_식단({firstValue}~{lastValue})";
                    }
                    else
                    {
                        label = $"{division}_식단({ProcessSurveyYear(items.First().SurveyYear):00})";
                    }

                    foreach (var item in items)
                    {
                        var subs = item.Subs.GroupBy(m => m.SpeciesKo)
                                             .Select(g => new NameValue { Name = g.Key ?? "", Value = g.Count() })
                                             .OrderByDescending(m => m.Value)
                                             .ThenBy(m => m.Name)
                                             .ToList();

                        var subSpecies = string.Join(",", subs.Select(s => s.Name));

                        var data = new MapData
                        {
                            Id = item.VegetationENVId,
                            Caption = caption,
                            Project = item.SiteDivision.Project,
                            Workplace = item.SiteDivision.Workplace,
                            WorkplaceRiver = item.SiteDivision.WorkplaceRiver,
                            YearChsu = item.Chsu,
                            SurveyDate = item.SurveyDate,
                            River = item.River,
                            Site = item.Site,
                            Lat = item.StartLat,
                            Lng = item.StartLng,
                            Surveyor = item.Surveyor,
                            SpeciesCount = subs.Count, // subs 개수
                            IndividualCount = item.Subs.Count, // item의 Subs 개수
                            SubSpecies = subSpecies,
                            SubSpeciesData = subs,
                        };
                        mapData.Add(data);
                    }

                    var model = new MapModel
                    {
                        Location = new Location(lat, lng),
                        Division = $"{division}_식단",
                        Caption = caption,
                        Label = label,
                        Data = mapData,
                        IsSpeciesCount = true,
                        IsIndividualCount = true,
                        IsSubSpecies = true,
                    };

                    pointData.Add(model);
                }

                if (IsSearching == false || (IsSearching && veENVQuery.Any()))
                {
                    mapDivisions.Add(new MapDivision
                    {
                        Project = project,
                        Division = $"{division}_식단",
                        Caption = caption,
                        DataCount = veENVCount,
                        Subs = new RadObservableCollection<MapModel>(pointData)
                    });
                }

                //서식수변
                caption = "서식수변";
                pointData.Clear();

                IQueryable<HabitatWaterEdge> habitatWaterEdgetQuery = context.HabitatWaterEdges.AsNoTracking().Where(m => m.SiteDivision.Project == project && m.Lat != null && m.Lng != null);

                var habitatWaterEdgeGroups = await habitatWaterEdgetQuery.GroupBy(m => new { m.Lat, m.Lng }).Select(g => new { Items = g.OrderBy(m => m.SurveyYear).ThenBy(m => m.SurveyDate).ToList() }).ToListAsync();

                var habitatCount = habitatWaterEdgeGroups.Sum(g => g.Items.Count);

                foreach (var group in habitatWaterEdgeGroups)
                {
                    var items = group.Items;
                    string? label = null;
                    var lat = items.FirstOrDefault()?.Lat ?? 0;
                    var lng = items.FirstOrDefault()?.Lng ?? 0;
                    var mapData = new List<MapData>();

                    if (items.Count > 1)
                    {
                        var firstValue = $"{ProcessSurveyYear(items.First().SurveyYear):00}";
                        var lastValue = $"{ProcessSurveyYear(items.Last().SurveyYear):00}";

                        label = firstValue == lastValue ? $"{division}_서({firstValue})" : $"{division}_서({firstValue}~{lastValue})";
                    }
                    else
                    {
                        label = $"{division}_서({ProcessSurveyYear(items.First().SurveyYear):00})";
                    }

                    foreach (var item in items)
                    {
                        var data = new MapData
                        {
                            Id = item.HabitatWaterEdgeId,
                            Caption = caption,
                            Project = item.SiteDivision.Project,
                            Workplace = item.SiteDivision.Workplace,
                            WorkplaceRiver = item.SiteDivision.WorkplaceRiver,
                            YearChsu = item.YearChsu,
                            SurveyDate = item.SurveyDate,
                            River = item.River,
                            Site = item.Site,
                            Lat = item.Lat,
                            Lng = item.Lng,
                            Surveyor = item.Surveyor,
                            Score = item.EvaluationScore,
                            Grade = item.EvaluationGrade,
                        };
                        mapData.Add(data);
                    }

                    var model = new MapModel
                    {
                        Location = new Location(lat, lng),
                        Division = $"{division}_서",
                        Caption = caption,
                        Label = label,
                        Data = mapData,
                        IsScoreGrade = true,
                    };

                    pointData.Add(model);
                }

                if (IsSearching == false || (IsSearching && searchValues.Count == 0))
                {
                    mapDivisions.Add(new MapDivision
                    {
                        Project = project,
                        Division = $"{division}_서",
                        Caption = caption,
                        DataCount = habitatCount,
                        Subs = new RadObservableCollection<MapModel>(pointData)
                    });
                }

                //수질
                caption = "수질";
                pointData.Clear();

                IQueryable<WaterQuality> waterQualityQuery = context.WaterQualitys.AsNoTracking().Where(m => m.SiteDivision.Project == project && m.Lat != null && m.Lng != null);

                var waterQualityGroups = await waterQualityQuery.GroupBy(m => new { m.Lat, m.Lng }).Select(g => new { Items = g.OrderBy(m => m.SurveyYear).ThenBy(m => m.SurveyDate).ToList() }).ToListAsync();

                var waterQualityCount = waterQualityGroups.Sum(g => g.Items.Count);

                foreach (var group in waterQualityGroups)
                {
                    var items = group.Items;
                    string? label = null;
                    var lat = items.FirstOrDefault()?.Lat ?? 0;
                    var lng = items.FirstOrDefault()?.Lng ?? 0;
                    var mapData = new List<MapData>();

                    if (items.Count > 1)
                    {
                        var firstValue = $"{ProcessSurveyYear(items.First().SurveyYear):00}";
                        var lastValue = $"{ProcessSurveyYear(items.Last().SurveyYear):00}";

                        label = firstValue == lastValue ? $"{division}_수({firstValue})" : $"{division}_수({firstValue}~{lastValue})";
                    }
                    else
                    {
                        label = $"{division}_수({ProcessSurveyYear(items.First().SurveyYear):00})";
                    }

                    foreach (var item in items)
                    {
                        var data = new MapData
                        {
                            Id = item.WaterQualityId,
                            Caption = caption,
                            Project = item.SiteDivision.Project,
                            Workplace = item.SiteDivision.Workplace,
                            WorkplaceRiver = item.SiteDivision.WorkplaceRiver,
                            YearChsu = item.YearChsu,
                            SurveyDate = item.SurveyDate,
                            River = item.River,
                            Site = item.Site,
                            Lat = item.Lat,
                            Lng = item.Lng,
                            Surveyor = item.Surveyor,
                            PHGrade = item.PHGrade,
                            BODGrade = item.BODGrade,
                            CODGrade = item.CODGrade,
                            TOCGrade = item.TOCGrade,
                            SSGrade = item.SSGrade,
                            DOGrade = item.DOGrade,
                            TPGrade = item.TPGrade,
                            EColiGrade = item.EColiGrade,
                        };
                        mapData.Add(data);
                    }

                    var model = new MapModel
                    {
                        Location = new Location(lat, lng),
                        Division = $"{division}_수",
                        Caption = caption,
                        Label = label,
                        Data = mapData,
                        IsWaterQuality = true,
                    };

                    pointData.Add(model);
                }

                if (IsSearching == false || (IsSearching && searchValues.Count == 0))
                {
                    mapDivisions.Add(new MapDivision
                    {
                        Project = project,
                        Division = $"{division}_수",
                        Caption = caption,
                        DataCount = waterQualityCount,
                        Subs = new RadObservableCollection<MapModel>(pointData)
                    });
                }
            }

            string[] removeDivisionsArray = ["방_양", "방_식물", "방_식단", "생_저", "생_서", "생_수", "생_식물", "생_식단"];
            var removeDivisions = new HashSet<string>(removeDivisionsArray);
            mapDivisions = [.. mapDivisions.Where(m => !removeDivisions.Contains(m.Division))];

            // UI 컬렉션 반영 (백그라운드 작업 이후에 UI 컬렉션 변경)
            DivisionData = mapDivisions;
            DivisionDataView.Source = DivisionData;
            DivisionDataView.GroupDescriptions.Clear();
            DivisionDataView.GroupDescriptions.Add(new PropertyGroupDescription("Project"));
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetPoints), ex.Message);
        }
    }


    //연도에서 뒤 2자리 추출
    private static int ProcessSurveyYear(int year)
    {
        if (year <= 0)
            return 0;

        return Math.Abs(year) % 100;
    }

    //지도 포인트 설정
    private void SetMapPoints()
    {
        PointCollection.Clear();

        var items = DivisionData.Where(m => m.IsSelected).ToList();
        foreach (var item in items)
        {
            PointCollection.AddRange(item.Subs);
        }
    }

    private void LatLngChangeReset()
    {
        BeforeLat = null;
        BeforeLng = null;
        AfterLat = null;
        AfterLng = null;
        LatLngChangeCollection.Clear();
        LatLngChangeSelectedItems.Clear();
    }

    #endregion
}
