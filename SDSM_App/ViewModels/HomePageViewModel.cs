using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Telerik.Windows.Controls.Gauge;
using Telerik.Windows.Controls.Map;
using Telerik.Windows.Controls.Spreadsheet.Dialogs;
using Telerik.Windows.Data;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

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

    [ObservableProperty] private RadObservableCollection<MapModel> _popupPointCollection = [];
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
                //검색어 목록 추가
                var items = PointData.Where(m => m.Caption == SearchCaptionSelectedItem
                                                && !string.IsNullOrEmpty(m.SpeciesKo)
                                                && SearchValueSelectionCollection.Any(sv => m.SpeciesKo.Contains(sv.SearchText ?? "")))
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

    #region ICommand 구현

    //전체 포인트 지우기
    [RelayCommand]
    private void AllClearPointExecuted()
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
        if (obj is MapModel selectedModel)
        {

            try
            {
                string caption = selectedModel.Caption;
                bool isChecked = selectedModel.IsSelected;

                if (isChecked == true)
                {
                    SearchCaptionCollection.Add(caption);

                    PointCollection.AddRange([.. PointData.Where(m => m.Caption == caption)]);
                }
                else
                {
                    SearchCaptionCollection.Remove(caption);

                    var points = PointCollection.Where(m => m.Caption == caption);
                    if (points.Any())
                    {
                        PointCollection.RemoveRange(points);
                    }
                }
            }
            catch { }
        }
    }

    //선택 팝업 목록
    [RelayCommand]
    private void PopupPointsExecuted(MapModel model)
    {
        if (PopupPointCollection.Count != 0)
            PopupPointCollection.Clear();

        var seledtedModels = PointCollection.Where(m => m.Lat == model.Lat && m.Lng == model.Lng).ToList();
        PopupPointCollection.AddRange(seledtedModels);
    }

    #endregion

    #region 매서드

    private async void InitialLoad()
    {
        GB.IsBusy = true;

        try
        {
            var tasks = new[] { GetData(), GetPoints(), GetSearchData() };

            await Task.WhenAny(tasks);
        }
        catch { }
        finally
        {
            GB.IsBusy = false;
        }
    }

    //조사자료 목록, 주제도 목록
    private async Task GetData()
    {
        try
        {
            using var context = new SDSMDbContext();

            var taxonCoordinateCount = await context.TaxonCoordinateCountViews.FirstOrDefaultAsync();

            SurveyDataCollection.Add(new MapModel { Caption = "식생" });
            SurveyDataCollection.Add(new MapModel { Caption = "식물" });
            SurveyDataCollection.Add(new MapModel { Caption = "양서파충류" });
            SurveyDataCollection.Add(new MapModel { Caption = "어류", Count = taxonCoordinateCount?.FishCount ?? 0, IsSearch = true });
            SurveyDataCollection.Add(new MapModel { Caption = "저서동물", Count = taxonCoordinateCount?.BenthosCount ?? 0, IsSearch = true });
            SurveyDataCollection.Add(new MapModel { Caption = "조류", Count = taxonCoordinateCount?.BirdCount ?? 0, IsSearch = true });
            SurveyDataCollection.Add(new MapModel { Caption = "포유류", Count = taxonCoordinateCount?.MammalCount ?? 0, IsSearch = true });
            SurveyDataCollection.Add(new MapModel { Caption = "서식수변", Count = taxonCoordinateCount?.HabitatWaterEdgeCount ?? 0 });
            SurveyDataCollection.Add(new MapModel { Caption = "수질", Count = taxonCoordinateCount?.WaterQualityCount ?? 0 });

            var themeMaps = new List<ContentItem>
            {
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
            };

            //주제도 지도
            ThemeMapCollection.AddRange(themeMaps);

        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetData), ex.Message);
            throw;
        }
    }

    //조사자료 포인트
    private async Task GetPoints()
    {
        using var context = new SDSMDbContext();

        try
        {

            var fishGroups = await context.Fishs.Where(m => m.Lat != null && m.Lng != null).Include(m => m.Subs).GroupBy(m => new { m.Lat, m.Lng })
                .Select(g => new
                {
                    Items = g.OrderBy(m => m.YearChsu).ToList()
                }).ToListAsync();

            foreach (var group in fishGroups)
            {
                var items = group.Items;
                string? label = null;

                if (items.Count > 1)
                {
                    var firstValue = $"{ProcessYearChsu(items.First().YearChsu):00}";
                    var lastValue = $"{ProcessYearChsu(items.Last().YearChsu):00}";

                    label = firstValue == lastValue ? $"어({firstValue})" : $"어({firstValue}~{lastValue})";
                }

                foreach (var item in items)
                {
                    var model = new MapModel
                    {
                        Location = new Location(item.Lat ?? 0, item.Lng ?? 0),
                        Caption = "어류",
                        Label = label ?? $"어({ProcessYearChsu(item.YearChsu):00})",
                        MarkerFill = Application.Current.FindResource("FishBrush") as Brush,
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
                        IsScoreGrade = true,
                        IsSpecies = true,
                        IsSpeciesCount = true,
                        IsIndividualCount = true,
                        SpeciesKo = string.Join(",", item.Subs.OrderBy(m => m.SpeciesKo).Select(s => s.SpeciesKo ?? " ").Distinct()),
                    };
                    PointData.Add(model);
                }
            }

            var benthosGroups = await context.Benthoss.Where(m => m.Lat != null && m.Lng != null).Include(m => m.Subs).GroupBy(m => new { m.Lat, m.Lng })
                .Select(g => new
                {
                    Items = g.OrderBy(m => m.YearChsu).ToList()
                }).ToListAsync();

            foreach (var group in benthosGroups)
            {
                var items = group.Items;
                string? label = null;

                if (items.Count > 1)
                {
                    var firstValue = $"{ProcessYearChsu(items.First().YearChsu):00}";
                    var lastValue = $"{ProcessYearChsu(items.Last().YearChsu):00}";

                    label = firstValue == lastValue ? $"저({firstValue})" : $"저({firstValue}~{lastValue})";
                }

                foreach (var item in items)
                {
                    var model = new MapModel
                    {
                        Location = new Location(item.Lat ?? 0, item.Lng ?? 0),
                        Caption = "저서동물",
                        Label = label ?? $"저({ProcessYearChsu(item.YearChsu):00})",
                        MarkerFill = Application.Current.FindResource("BenthosBrush") as Brush,
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
                        IsScoreGrade = true,
                        IsSpecies = true,
                        IsSpeciesCount = true,
                        IsIndividualCount = true,
                        SpeciesKo = string.Join(",", item.Subs.OrderBy(m => m.SpeciesKo).Select(s => s.SpeciesKo ?? " ").Distinct()),
                    };
                    PointData.Add(model);
                }
            }

            var birdGroups = await context.Birds.Where(m => m.Lat != null && m.Lng != null).GroupBy(m => new { m.Lat, m.Lng })
                .Select(g => new
                {
                    Items = g.OrderBy(m => m.YearChsu).ToList()
                }).ToListAsync();

            foreach (var group in birdGroups)
            {
                var items = group.Items;
                string? label = null;

                if (items.Count > 1)
                {
                    var firstValue = $"{ProcessYearChsu(items.First().YearChsu):00}";
                    var lastValue = $"{ProcessYearChsu(items.Last().YearChsu):00}";

                    label = firstValue == lastValue ? $"조({firstValue})" : $"조({firstValue}~{lastValue})";
                }

                foreach (var item in items)
                {
                    var model = new MapModel
                    {
                        Location = new Location(item.Lat ?? 0, item.Lng ?? 0),
                        Caption = "조류",
                        Label = label ?? $"조({ProcessYearChsu(item.YearChsu):00})",
                        MarkerFill = Application.Current.FindResource("BirdBrush") as Brush,
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
                        IsSpecies = true,
                        IsIndividualCount = true,
                    };
                    PointData.Add(model);
                }
            }

            var mammalGroups = await context.Mammals.Where(m => m.Lat != null && m.Lng != null).GroupBy(m => new { m.Lat, m.Lng })
                .Select(g => new
                {
                    Items = g.OrderBy(m => m.YearChsu).ToList()
                }).ToListAsync();

            foreach (var group in mammalGroups)
            {
                var items = group.Items;
                string? label = null;

                if (items.Count > 1)
                {
                    var firstValue = $"{ProcessYearChsu(items.First().YearChsu):00}";
                    var lastValue = $"{ProcessYearChsu(items.Last().YearChsu):00}";

                    label = firstValue == lastValue ? $"포({firstValue})" : $"포({firstValue}~{lastValue})";
                }

                foreach (var item in items)
                {
                    var model = new MapModel
                    {
                        Location = new Location(item.Lat ?? 0, item.Lng ?? 0),
                        Caption = "포유류",
                        Label = label ?? $"포({ProcessYearChsu(item.YearChsu):00})",
                        MarkerFill = Application.Current.FindResource("MammalBrush") as Brush,
                        YearChsu = item.YearChsu,
                        SurveyDate = item.SurveyDate,
                        River = item.River,
                        Site = item.Site,
                        Lat = item.Lat,
                        Lng = item.Lng,
                        Surveyor = item.Surveyor,
                        SpeciesKo = item.SpeciesKo,
                        SpeciesEn = item.SpeciesEn,
                        IsSpecies = true,
                    };
                    PointData.Add(model);
                }
            }

            var habitatWaterEdgeGroups = await context.HabitatWaterEdges.Where(m => m.Lat != null && m.Lng != null).GroupBy(m => new { m.Lat, m.Lng })
                .Select(g => new
                {
                    Items = g.OrderBy(m => m.YearChsu).ToList()
                }).ToListAsync();

            foreach (var group in habitatWaterEdgeGroups)
            {
                var items = group.Items;
                string? label = null;

                if (items.Count > 1)
                {
                    var firstValue = $"{ProcessYearChsu(items.First().YearChsu):00}";
                    var lastValue = $"{ProcessYearChsu(items.Last().YearChsu):00}";

                    label = firstValue == lastValue ? $"서({firstValue})" : $"서({firstValue}~{lastValue})";
                }

                foreach (var item in items)
                {
                    var model = new MapModel
                    {
                        Location = new Location(item.Lat ?? 0, item.Lng ?? 0),
                        Caption = "서식수변",
                        Label = label ?? $"서({ProcessYearChsu(item.YearChsu):00})",
                        MarkerFill = Application.Current.FindResource("HabitatWaterEdgeBrush") as Brush,
                        YearChsu = item.YearChsu,
                        SurveyDate = item.SurveyDate,
                        River = item.River,
                        Site = item.Site,
                        Lat = item.Lat,
                        Lng = item.Lng,
                        Surveyor = item.Surveyor,
                        Score = item.EvaluationScore,
                        Grade = item.EvaluationGrade,
                        IsScoreGrade = true,
                    };
                    PointData.Add(model);
                }
            }

            var waterQualityGroups = await context.WaterQualitys.Where(m => m.Lat != null && m.Lng != null).GroupBy(m => new { m.Lat, m.Lng })
                .Select(g => new
                {
                    Items = g.OrderBy(m => m.YearChsu).ToList()
                }).ToListAsync();

            foreach (var group in waterQualityGroups)
            {
                var items = group.Items;
                string? label = null;

                if (items.Count > 1)
                {
                    var firstValue = $"{ProcessYearChsu(items.First().YearChsu):00}";
                    var lastValue = $"{ProcessYearChsu(items.Last().YearChsu):00}";

                    label = firstValue == lastValue ? $"수({firstValue})" : $"수({firstValue}~{lastValue})";
                }

                foreach (var item in items)
                {
                    var model = new MapModel
                    {
                        Location = new Location(item.Lat ?? 0, item.Lng ?? 0),
                        Caption = "수질",
                        Label = label ?? $"수({ProcessYearChsu(item.YearChsu):00})",
                        MarkerFill = Application.Current.FindResource("WaterQualityBrush") as Brush,
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
                        IsWaterQuality = true,
                    };
                    PointData.Add(model);
                }
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

            SearchValueData.AddRange(fishs);
            SearchValueData.AddRange(benthos);
            SearchValueData.AddRange(birds);
            SearchValueData.AddRange(mammals);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetSearchData), ex.Message);
            throw;
        }
    }

    //연도차수에서 연도(2자리) 추출
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

    #endregion 
}
