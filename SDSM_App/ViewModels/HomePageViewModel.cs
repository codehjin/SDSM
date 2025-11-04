using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using System.Collections.Specialized;
using System.Text;
using System.Windows;
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

    [ObservableProperty] private RadObservableCollection<MapModel> _surveyDataCollection = [];

    public List<MapModel> PointData { get; set; } = [];
    [ObservableProperty] private RadObservableCollection<MapModel> _pointCollection = [];

    [ObservableProperty] private RadObservableCollection<MapData> _popupPointCollection = [];
    [ObservableProperty] private RadObservableCollection<ContentItem> _themeMapCollection = [];

    #endregion

    #region 검색 속성 및 ICommand 구현   

    [ObservableProperty] private RadObservableCollection<string> _searchCaptionCollection = [];
    [ObservableProperty] private string? _searchCaptionSelectedItem;
    public List<SearchItem> SearchValueData = [];
    [ObservableProperty] private RadObservableCollection<SearchItem> _searchValueCollection = [];
    [ObservableProperty] private RadObservableCollection<SearchItem> _searchValueSelectionCollection = [];

    //검색 자료 변경으로 인한 검색어 목록 변경, 검색어 값 초기화
    [RelayCommand]
    private void SearchCaptionSelectionChangedExecuted()
    {
        SearchValueCollection.Clear();
        SearchValueSelectionCollection.Clear();

        if (!string.IsNullOrEmpty(SearchCaptionSelectedItem))
        {
            SearchValueCollection.AddRange([.. SearchValueData.Where(m => m.Caption == SearchCaptionSelectedItem)]);
        }
    }

    //검색어 값 변경으로 인한 해당 조사 포인트 제거 후 추가 
    [RelayCommand]
    private void SearchValueSelectionChangedExecuted()
    {
        if (SearchCaptionSelectedItem != null && PointCollection.Count != 0)
        {
            //해당 조사 포인트 제거
            var points = PointCollection.Where(m => m.Caption == SearchCaptionSelectedItem).ToList();
            PointCollection.RemoveRange(points);

            if (SearchValueSelectionCollection.Count != 0)
            {
                /// 검색어 목록 추가 및 하위 자료 필터링 (어류, 저서동물, 식생단면은 하위 종목으로 필터링)
                var items = PointData
                    .Where(m => m.Caption == SearchCaptionSelectedItem && SearchValueSelectionCollection.Any(x => m.SearchText.Contains(x.SearchText ?? "")))
                    .Select(m =>
                    {
                        // MapModel 복사
                        var clone = new MapModel
                        {
                            Location = m.Location,
                            Caption = m.Caption,
                            Label = m.Label,
                            IsSelected = m.IsSelected,
                            IsVisible = m.IsVisible,
                            MarkerFill = m.MarkerFill,
                            Count = m.Count,
                            PointCount = m.PointCount,
                            IsSearch = m.IsSearch,
                            IsSpecies = m.IsSpecies,
                            IsScoreGrade = m.IsScoreGrade,
                            IsSpeciesCount = m.IsSpeciesCount,
                            IsIndividualCount = m.IsIndividualCount,
                            IsSubSpecies = m.IsSubSpecies,
                            IsWaterQuality = m.IsWaterQuality,
                            SearchText = m.SearchText,
                            Data = [.. m.Data]
                        };

                        if (SearchCaptionSelectedItem == "어류" || SearchCaptionSelectedItem == "저서동물" || SearchCaptionSelectedItem == "식생단면")
                        {
                            clone.Data = [.. clone.Data.Where(d => SearchValueSelectionCollection.Any(x => d.SubSpecies != null && d.SubSpecies.Contains(x.SearchText ?? "")))];
                        }
                        else
                        {
                            clone.Data = [.. clone.Data.Where(d => SearchValueSelectionCollection.Any(x => d.SpeciesKo != null && d.SpeciesKo.Contains(x.SearchText ?? "")))];
                        }
                        return clone;
                    })
                    .Where(m => m.Data.Count > 0) // 데이터가 남아있는 것만 추가
                    .ToList();

                PointCollection.AddRange(items);
            }
            else
            {
                var items = PointData.Where(m => m.Caption == SearchCaptionSelectedItem).ToList();
                PointCollection.AddRange(items);
            }
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
        BeforeLat = null;
        BeforeLng = null;
        AfterLat = null;
        AfterLng = null;
        LatLngChangeCollection.Clear();
        LatLngChangeSelectedItems.Clear();
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
    private async Task LatLngChangedSaveExecuted()
    {
        GB.IsBusy = true;

        try
        {
            var groups = LatLngChangeSelectedItems.GroupBy(m => m.Caption)
                                                   .Select(g => new
                                                   {
                                                       Caption = g.Key,
                                                       Ids = g.Select(x => x.Id).ToList()
                                                   })
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

            // 위경도 변경 초기화
            LatLngChangeReset();
            LatLngChangeViewVisibility = Visibility.Collapsed;

            //선택된 조사자료
            var selectedSurveys = SurveyDataCollection.Where(m => m.IsSelected).ToList();

            //조사자료와 포인트 다시 불러오기
            var tasks = new[] { GetSurveyData(), GetPoints(), };

            await Task.WhenAll(tasks);

            //검색 속성 초기화
            SearchCaptionCollection.Clear();
            SearchCaptionSelectionChangedExecuted();

            //포인트 목록 초기화
            PointCollection.Clear();

            //기존 조사자료 선택하기
            foreach (var item in SurveyDataCollection)
            {
                item.IsSelected = selectedSurveys.Any(m => m.Caption == item.Caption);
            }

            //선택된 조사자료 불러오기
            foreach (var item in SurveyDataCollection)
            {
                if (item.IsSelected)
                {
                    AddAndRemovePointExecutedCommand.Execute(item);
                }
            }
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

    //조사자료 전체 선택해제
    [RelayCommand]
    private void AllPointUnCheckedExecuted()
    {
        foreach (var item in SurveyDataCollection.Where(m => m.IsSelected))
        {
            item.IsSelected = false;
        }

        PointCollection.Clear();

        SearchCaptionCollection.Clear();
        SearchValueCollection.Clear();
    }

    //포인트 표시 또는 제거
    [RelayCommand]
    private void AddAndRemovePointExecuted(object obj)
    {
        try
        {
            if (obj is MapModel selectedModel)
            {

                string caption = selectedModel.Caption;
                bool isChecked = selectedModel.IsSelected;
                bool isSearch = selectedModel.IsSearch;

                if (isChecked == true)
                {
                    if (isSearch)
                    {
                        SearchCaptionCollection.Add(caption);
                    }

                    PointCollection.AddRange(PointData.Where(m => m.Caption == caption));
                }
                else
                {
                    SearchCaptionCollection.Remove(caption);

                    var removeItems = PointCollection.Where(m => m.Caption == caption);
                    PointCollection.RemoveRange(removeItems);
                }
            }
        }
        catch { }
    }

    //선택 팝업 목록
    [RelayCommand]
    private void PopupPointsExecuted(MapModel model)
    {
        if (PopupPointCollection.Count != 0)
            PopupPointCollection.Clear();

        var items = PointCollection.Where(m => m.Location.Latitude == model.Location.Latitude && m.Location.Longitude == model.Location.Longitude)
                                    .OrderBy(m => m.Caption)
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

            var tasks = new[] { GetSurveyData(), GetPoints(), GetSearchData() };

            await Task.WhenAll(tasks);
        }
        catch { }
        finally
        {
            GB.IsBusy = false;
        }
    }

    //조사자료 목록, 주제도 목록
    private async Task GetSurveyData()
    {
        try
        {
            using var context = new SDSMDbContext();

            var taxonCoordinateCount = await context.TaxonCoordinateCountViews.FirstOrDefaultAsync();

            if (SurveyDataCollection.Count != 0)
                SurveyDataCollection.Clear();

            SurveyDataCollection.Add(new MapModel { Caption = "어류", Count = taxonCoordinateCount?.FishCount ?? 0, PointCount = taxonCoordinateCount?.FishDistinctCount ?? 0, IsSearch = true });
            SurveyDataCollection.Add(new MapModel { Caption = "저서동물", Count = taxonCoordinateCount?.BenthosCount ?? 0, PointCount = taxonCoordinateCount?.BenthosDistinctCount ?? 0, IsSearch = true });
            SurveyDataCollection.Add(new MapModel { Caption = "조류", Count = taxonCoordinateCount?.BirdCount ?? 0, PointCount = taxonCoordinateCount?.BirdDistinctCount ?? 0, IsSearch = true });
            SurveyDataCollection.Add(new MapModel { Caption = "포유류", Count = taxonCoordinateCount?.MammalCount ?? 0, PointCount = taxonCoordinateCount?.MammalDistinctCount ?? 0, IsSearch = true });
            SurveyDataCollection.Add(new MapModel { Caption = "식물상", Count = taxonCoordinateCount?.PlantCount ?? 0, PointCount = taxonCoordinateCount?.PlantDistinctCount ?? 0, IsSearch = true });
            SurveyDataCollection.Add(new MapModel { Caption = "식생단면", Count = taxonCoordinateCount?.VegetationENVCount ?? 0, PointCount = taxonCoordinateCount?.VegetationENVDistinctCount ?? 0, IsSearch = true });
            SurveyDataCollection.Add(new MapModel { Caption = "양서파충류" });
            SurveyDataCollection.Add(new MapModel { Caption = "서식수변", Count = taxonCoordinateCount?.HabitatWaterEdgeCount ?? 0, PointCount = taxonCoordinateCount?.HabitatWaterEdgeDistinctCount ?? 0, });
            SurveyDataCollection.Add(new MapModel { Caption = "수질", Count = taxonCoordinateCount?.WaterQualityCount ?? 0, PointCount = taxonCoordinateCount?.WaterQualityDistinctCount ?? 0, });
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetSurveyData), ex.Message);
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
            if (PointData.Count != 0)
                PointData.Clear();

            //어류
            var fishGroups = await context.Fishs.Where(m => m.Lat != null && m.Lng != null)
                                            .Include(m => m.Subs)
                                            .GroupBy(m => new { m.Lat, m.Lng })
                                            .Select(g => new
                                            {
                                                Items = g.OrderBy(m => m.YearChsu).ToList()
                                            }).ToListAsync();

            foreach (var group in fishGroups)
            {
                var items = group.Items;
                string? label = null;
                var lat = items.FirstOrDefault()?.Lat ?? 0;
                var lng = items.FirstOrDefault()?.Lng ?? 0;
                var mapData = new List<MapData>();
                StringBuilder sb = new();

                if (items.Count > 1)
                {
                    var firstValue = $"{ProcessYearChsu(items.First().YearChsu):00}";
                    var lastValue = $"{ProcessYearChsu(items.Last().YearChsu):00}";

                    label = firstValue == lastValue ? $"어({firstValue})" : $"어({firstValue}~{lastValue})";
                }
                else
                {
                    label = $"어({ProcessYearChsu(items.First().YearChsu):00})";
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
                        Caption = "어류",
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
                    sb.Append(subSpecies);
                    mapData.Add(data);
                }

                var model = new MapModel
                {
                    Location = new Location(lat, lng),
                    Caption = "어류",
                    Label = label,
                    MarkerFill = Application.Current.FindResource("FishBrush") as Brush,
                    Data = mapData,
                    IsScoreGrade = true,
                    IsSpeciesCount = true,
                    IsIndividualCount = true,
                    IsSubSpecies = true,
                    SearchText = sb.ToString(),
                };

                PointData.Add(model);
            }

            //저서동물
            var benthosGroups = await context.Benthoss.Where(m => m.Lat != null && m.Lng != null)
                                                         .Include(m => m.Subs)
                                                         .GroupBy(m => new { m.Lat, m.Lng })
                                                         .Select(g => new
                                                         {
                                                             Items = g.OrderBy(m => m.YearChsu).ToList()
                                                         }).ToListAsync();

            foreach (var group in benthosGroups)
            {
                var items = group.Items;
                string? label = null;
                var lat = items.FirstOrDefault()?.Lat ?? 0;
                var lng = items.FirstOrDefault()?.Lng ?? 0;
                var mapData = new List<MapData>();
                StringBuilder sb = new();

                if (items.Count > 1)
                {
                    var firstValue = $"{ProcessYearChsu(items.First().YearChsu):00}";
                    var lastValue = $"{ProcessYearChsu(items.Last().YearChsu):00}";

                    label = firstValue == lastValue ? $"저({firstValue})" : $"저({firstValue}~{lastValue})";
                }
                else
                {
                    label = $"저({ProcessYearChsu(items.First().YearChsu):00})";
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
                        Caption = "저서동물",
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
                    sb.Append(subSpecies);
                    mapData.Add(data);
                }

                var model = new MapModel
                {
                    Location = new Location(lat, lng),
                    Caption = "저서동물",
                    Label = label,
                    MarkerFill = Application.Current.FindResource("BenthosBrush") as Brush,
                    Data = mapData,
                    IsScoreGrade = true,
                    IsSpeciesCount = true,
                    IsIndividualCount = true,
                    IsSubSpecies = true,
                    SearchText = sb.ToString(),
                };

                PointData.Add(model);
            }

            //조류
            var birdGroups = await context.Birds.Where(m => m.Lat != null && m.Lng != null)
                                                   .GroupBy(m => new { m.Lat, m.Lng })
                                                   .Select(g => new
                                                   {
                                                       Items = g.OrderBy(m => m.YearChsu).ToList()
                                                   }).ToListAsync();

            foreach (var group in birdGroups)
            {
                var items = group.Items;
                string? label = null;
                var lat = items.FirstOrDefault()?.Lat ?? 0;
                var lng = items.FirstOrDefault()?.Lng ?? 0;
                var mapData = new List<MapData>();
                StringBuilder sb = new();

                if (items.Count > 1)
                {
                    var firstValue = $"{ProcessYearChsu(items.First().YearChsu):00}";
                    var lastValue = $"{ProcessYearChsu(items.Last().YearChsu):00}";

                    label = firstValue == lastValue ? $"조({firstValue})" : $"조({firstValue}~{lastValue})";
                }
                else
                {
                    label = $"조({ProcessYearChsu(items.First().YearChsu):00})";
                }

                foreach (var item in items)
                {
                    var data = new MapData
                    {
                        Id = item.BirdId,
                        Caption = "조류",
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
                    sb.Append(item.SpeciesKo);
                    mapData.Add(data);
                }

                var model = new MapModel
                {
                    Location = new Location(lat, lng),
                    Caption = "조류",
                    Label = label,
                    MarkerFill = Application.Current.FindResource("BirdBrush") as Brush,
                    Data = mapData,
                    IsSpecies = true,
                    IsIndividualCount = true,
                    SearchText = sb.ToString(),
                };

                PointData.Add(model);
            }

            //포유류
            var mammalGroups = await context.Mammals.Where(m => m.Lat != null && m.Lng != null)
                                                       .GroupBy(m => new { m.Lat, m.Lng })
                                                       .Select(g => new
                                                       {
                                                           Items = g.OrderBy(m => m.YearChsu).ToList()
                                                       }).ToListAsync();

            foreach (var group in mammalGroups)
            {
                var items = group.Items;
                string? label = null;
                var lat = items.FirstOrDefault()?.Lat ?? 0;
                var lng = items.FirstOrDefault()?.Lng ?? 0;
                var mapData = new List<MapData>();
                StringBuilder sb = new();

                if (items.Count > 1)
                {
                    var firstValue = $"{ProcessYearChsu(items.First().YearChsu):00}";
                    var lastValue = $"{ProcessYearChsu(items.Last().YearChsu):00}";

                    label = firstValue == lastValue ? $"포({firstValue})" : $"포({firstValue}~{lastValue})";
                }
                else
                {
                    label = $"포({ProcessYearChsu(items.First().YearChsu):00})";
                }

                foreach (var item in items)
                {
                    var data = new MapData
                    {
                        Id = item.MammalId,
                        Caption = "포유류",
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
                    sb.Append(item.SpeciesKo);
                    mapData.Add(data);
                }

                var model = new MapModel
                {
                    Location = new Location(lat, lng),
                    Caption = "포유류",
                    Label = label,
                    MarkerFill = Application.Current.FindResource("MammalBrush") as Brush,
                    Data = mapData,
                    IsSpecies = true,
                    SearchText = sb.ToString(),
                };

                PointData.Add(model);
            }

            //식물상
            var plantGroups = await context.Plants.Where(m => m.Lat != null && m.Lng != null)
                                                    .GroupBy(m => new { m.Lat, m.Lng })
                                                    .Select(g => new
                                                    {
                                                        Items = g.OrderBy(m => m.Chsu).ToList()
                                                    }).ToListAsync();

            foreach (var group in plantGroups)
            {
                var items = group.Items;
                string? label = null;
                var lat = items.FirstOrDefault()?.Lat ?? 0;
                var lng = items.FirstOrDefault()?.Lng ?? 0;
                var mapData = new List<MapData>();
                StringBuilder sb = new();

                if (items.Count > 1)
                {
                    var firstValue = $"{ProcessSurveyYear(items.First().SurveyYear):00}";
                    var lastValue = $"{ProcessSurveyYear(items.Last().SurveyYear):00}";

                    label = firstValue == lastValue ? $"식({firstValue})" : $"식({firstValue}~{lastValue})";
                }
                else
                {
                    label = $"식({ProcessSurveyYear(items.First().SurveyYear):00})";
                }

                foreach (var item in items)
                {
                    var data = new MapData
                    {
                        Id = item.PlantId,
                        Caption = "식물상",
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
                    sb.Append(item.SpeciesKo);
                    mapData.Add(data);
                }

                var model = new MapModel
                {
                    Location = new Location(lat, lng),
                    Caption = "식물상",
                    Label = label,
                    MarkerFill = Application.Current.FindResource("PlantBrush") as Brush,
                    Data = mapData,
                    IsSpecies = true,
                    SearchText = sb.ToString(),
                };

                PointData.Add(model);
            }

            //식생단면
            var veENVGroups = await context.VegetationENVs.Where(m => m.StartLat != null && m.StartLng != null)
                                                             .Include(m => m.Subs)
                                                             .GroupBy(m => new { m.StartLat, m.StartLng })
                                                             .Select(g => new
                                                             {
                                                                 Items = g.OrderBy(m => m.Chsu).ToList()
                                                             }).ToListAsync();

            foreach (var group in veENVGroups)
            {
                var items = group.Items;
                string? label = null;
                var lat = items.FirstOrDefault()?.StartLat ?? 0;
                var lng = items.FirstOrDefault()?.StartLng ?? 0;
                var mapData = new List<MapData>();
                StringBuilder sb = new();

                if (items.Count > 1)
                {
                    var firstValue = $"{ProcessSurveyYear(items.First().SurveyYear):00}";
                    var lastValue = $"{ProcessSurveyYear(items.Last().SurveyYear):00}";

                    label = firstValue == lastValue ? $"식단({firstValue})" : $"식단({firstValue}~{lastValue})";
                }
                else
                {
                    label = $"식단({ProcessSurveyYear(items.First().SurveyYear):00})";
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
                        Caption = "식생단면",
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
                    sb.Append(subSpecies);
                    mapData.Add(data);
                }

                var model = new MapModel
                {
                    Location = new Location(lat, lng),
                    Caption = "식생단면",
                    Label = label,
                    MarkerFill = Application.Current.FindResource("VegetationBrush") as Brush,
                    Data = mapData,
                    IsSpeciesCount = true,
                    IsIndividualCount = true,
                    IsSubSpecies = true,
                    SearchText = sb.ToString(),
                };

                PointData.Add(model);
            }

            //서식수변
            var habitatWaterEdgeGroups = await context.HabitatWaterEdges.Where(m => m.Lat != null && m.Lng != null)
                                                                            .GroupBy(m => new { m.Lat, m.Lng })
                                                                            .Select(g => new
                                                                            {
                                                                                Items = g.OrderBy(m => m.YearChsu).ToList()
                                                                            }).ToListAsync();

            foreach (var group in habitatWaterEdgeGroups)
            {
                var items = group.Items;
                string? label = null;
                var lat = items.FirstOrDefault()?.Lat ?? 0;
                var lng = items.FirstOrDefault()?.Lng ?? 0;
                var mapData = new List<MapData>();

                if (items.Count > 1)
                {
                    var firstValue = $"{ProcessYearChsu(items.First().YearChsu):00}";
                    var lastValue = $"{ProcessYearChsu(items.Last().YearChsu):00}";

                    label = firstValue == lastValue ? $"서({firstValue})" : $"서({firstValue}~{lastValue})";
                }
                else
                {
                    label = $"서({ProcessYearChsu(items.First().YearChsu):00})";
                }

                foreach (var item in items)
                {
                    var data = new MapData
                    {
                        Id = item.HabitatWaterEdgeId,
                        Caption = "서식수변",
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
                    Caption = "서식수변",
                    Label = label,
                    MarkerFill = Application.Current.FindResource("HabitatWaterEdgeBrush") as Brush,
                    Data = mapData,
                    IsScoreGrade = true,
                };

                PointData.Add(model);
            }

            //수질
            var waterQualityGroups = await context.WaterQualitys.Where(m => m.Lat != null && m.Lng != null)
                                                                   .GroupBy(m => new { m.Lat, m.Lng })
                                                                   .Select(g => new
                                                                   {
                                                                       Items = g.OrderBy(m => m.YearChsu).ToList()
                                                                   }).ToListAsync();

            foreach (var group in waterQualityGroups)
            {
                var items = group.Items;
                string? label = null;
                var lat = items.FirstOrDefault()?.Lat ?? 0;
                var lng = items.FirstOrDefault()?.Lng ?? 0;
                var mapData = new List<MapData>();

                if (items.Count > 1)
                {
                    var firstValue = $"{ProcessYearChsu(items.First().YearChsu):00}";
                    var lastValue = $"{ProcessYearChsu(items.Last().YearChsu):00}";

                    label = firstValue == lastValue ? $"수({firstValue})" : $"수({firstValue}~{lastValue})";
                }
                else
                {
                    label = $"수({ProcessYearChsu(items.First().YearChsu):00})";
                }

                foreach (var item in items)
                {
                    var data = new MapData
                    {
                        Id = item.WaterQualityId,
                        Caption = "수질",
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
                    Caption = "수질",
                    Label = label,
                    MarkerFill = Application.Current.FindResource("WaterQualityBrush") as Brush,
                    Data = mapData,
                    IsWaterQuality = true,
                };

                PointData.Add(model);
            }
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetPoints), ex.Message);
        }
    }

    //검색자료
    private async Task GetSearchData()
    {
        try
        {
            using var context = new SDSMDbContext();

            //어류
            var fishs = await context.FishSpeciesViews.Where(m => m.Lat != null && m.Lng != null && !string.IsNullOrEmpty(m.SpeciesKo))
                                                        .Select(m => m.SpeciesKo)
                                                        .Distinct()
                                                        .Select(s => new SearchItem { Caption = "어류", SearchText = s })
                                                        .ToListAsync();

            // 저서동물
            var benthos = await context.BenthosSpeciesViews.Where(m => m.Lat != null && m.Lng != null && !string.IsNullOrEmpty(m.SpeciesKo))
                                                             .Select(m => m.SpeciesKo)
                                                             .Distinct()
                                                             .OrderBy(s => s)
                                                             .Select(s => new SearchItem { Caption = "저서동물", SearchText = s })
                                                             .ToListAsync();

            // 조류
            var birds = await context.Birds.Where(m => m.Lat != null && m.Lng != null && !string.IsNullOrEmpty(m.SpeciesKo))
                                             .Select(m => m.SpeciesKo)
                                             .Distinct()
                                             .OrderBy(s => s)
                                             .Select(s => new SearchItem { Caption = "조류", SearchText = s })
                                             .ToListAsync();

            // 포유류
            var mammals = await context.Mammals.Where(m => m.Lat != null && m.Lng != null && !string.IsNullOrEmpty(m.SpeciesKo))
                                                 .Select(m => m.SpeciesKo)
                                                 .Distinct()
                                                 .OrderBy(s => s)
                                                 .Select(s => new SearchItem { Caption = "포유류", SearchText = s })
                                                 .ToListAsync();

            // 식물상
            var plants = await context.Plants.Where(m => m.Lat != null && m.Lng != null && !string.IsNullOrEmpty(m.SpeciesKo))
                                               .Select(m => m.SpeciesKo)
                                               .Distinct()
                                               .OrderBy(s => s)
                                               .Select(s => new SearchItem { Caption = "식물상", SearchText = s })
                                               .ToListAsync();

            // 식생단면
            var veENVs = await context.VegetationENVSpeciesViews.Where(m => m.StartLat != null && m.StartLng != null && !string.IsNullOrEmpty(m.SpeciesKo))
                                                                  .Select(m => m.SpeciesKo)
                                                                  .Distinct()
                                                                  .OrderBy(s => s)
                                                                  .Select(s => new SearchItem { Caption = "식생단면", SearchText = s })
                                                                  .ToListAsync();

            SearchValueData.AddRange(fishs);
            SearchValueData.AddRange(benthos);
            SearchValueData.AddRange(birds);
            SearchValueData.AddRange(mammals);
            SearchValueData.AddRange(plants);
            SearchValueData.AddRange(veENVs);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetSearchData), ex.Message);
            throw;
        }
    }

    //연도에서 뒤 2자리 추출
    private static int ProcessSurveyYear(int year)
    {
        if (year <= 0)
            return 0;

        return Math.Abs(year) % 100;
    }

    //연도차수에서 연도의 뒤 2자리 추출
    private static int ProcessYearChsu(string? yearChsu)
    {
        if (string.IsNullOrEmpty(yearChsu) || yearChsu.Length < 2)
            return 0;

        // 앞 4글자가 모두 숫자인지 확인
        bool first4IsDigit = yearChsu[..4].All(char.IsDigit);

        if (first4IsDigit)
        {
            // 3~4번째 글자를 int로 변환하여 반환
            if (int.TryParse(yearChsu.AsSpan(2, 2), out int result))
                return result;
        }
        else
        {
            // 앞 2글자가 모두 숫자인지 확인
            bool first2IsDigit = yearChsu[..2].All(char.IsDigit);
            if (first2IsDigit)
            {
                if (int.TryParse(yearChsu.AsSpan(0, 2), out int result))
                    return result;
            }
        }

        return 0;
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
