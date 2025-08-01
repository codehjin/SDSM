using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using System.Windows;
using System.Windows.Media;
using Telerik.Windows.Controls.Gauge;
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
        InitialLoad();
    }

    #endregion

    #region 속성

    [ObservableProperty] private double _zoom = 9;
    [ObservableProperty] private Location _center = new(37.031062, 127.044525);

    [ObservableProperty] private string _degress = string.Empty;

    [ObservableProperty] private RadObservableCollection<MapModel> _surveyDataCollection = [];
    [ObservableProperty] private RadObservableCollection<MapModel> _pointCollection = [];
    [ObservableProperty] private RadObservableCollection<MapModel> _selectedCollection = [];

    [ObservableProperty] private RadObservableCollection<ContentItem> _themeMapCollection = [];

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
    }

    //포인트 표시 또는 제거
    [RelayCommand]
    private async Task AddAndRemovePointExecuted(object obj)
    {
        if (obj is MapModel selectedModel)
        {
            if (GB.IsBusy == false)
                GB.IsBusy = true;

            try
            {
                var models = new List<MapModel>();

                string caption = selectedModel.Caption;
                bool isChecked = selectedModel.IsSelected;

                switch (caption)
                {
                    case "어류":
                        {
                            if (isChecked == true)
                            {
                                models = await GetPoints(caption);
                            }
                            else
                            {
                                var points = PointCollection.Where(m => m.Caption == caption);

                                if (points.Any())
                                {
                                    PointCollection.RemoveRange(points);
                                }
                            }
                        }
                        break;
                    case "저서동물":
                        {
                            if (isChecked == true)
                            {
                                models = await GetPoints(caption);
                            }
                            else
                            {
                                var points = PointCollection.Where(m => m.Caption == caption);

                                if (points.Any())
                                {
                                    PointCollection.RemoveRange(points);
                                }
                            }
                        }
                        break;
                    case "조류":
                        {
                            if (isChecked == true)
                            {
                                models = await GetPoints(caption);
                            }
                            else
                            {
                                var points = PointCollection.Where(m => m.Caption == caption);

                                if (points.Any())
                                {
                                    PointCollection.RemoveRange(points);
                                }
                            }
                        }
                        break;
                    case "포유류":
                        {
                            if (isChecked == true)
                            {
                                models = await GetPoints(caption);
                            }
                            else
                            {
                                var points = PointCollection.Where(m => m.Caption == caption);

                                if (points.Any())
                                {
                                    PointCollection.RemoveRange(points);
                                }
                            }
                        }
                        break;
                    case "서식수변":
                        {
                            if (isChecked == true)
                            {
                                models = await GetPoints(caption);
                            }
                            else
                            {
                                var points = PointCollection.Where(m => m.Caption == caption);

                                if (points.Any())
                                {
                                    PointCollection.RemoveRange(points);
                                }
                            }
                        }
                        break;
                    case "수질":
                        {
                            if (isChecked == true)
                            {
                                models = await GetPoints(caption);
                            }
                            else
                            {
                                var points = PointCollection.Where(m => m.Caption == caption);

                                if (points.Any())
                                {
                                    PointCollection.RemoveRange(points);
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }

                if (models.Count > 0)
                    PointCollection.AddRange(models);
            }
            catch { }
            finally
            {
                if (GB.IsBusy == true)
                    GB.IsBusy = false;
            }
        }
    }

    [RelayCommand]
    private void AddSelectedPointsExecuted(MapModel model)
    {
        if (SelectedCollection.Count != 0)
            SelectedCollection.Clear();

        var seledtedModels = PointCollection.Where(m => m.Lat == model.Lat && m.Lng == model.Lng).ToList();
        SelectedCollection.AddRange(seledtedModels);
    }

    #endregion

    #region 매서드

    private async void InitialLoad()
    {
        GB.IsBusy = true;

        try
        {
            var tasks = new[] { GetData() };

            await Task.WhenAny(tasks);
        }
        catch { }
        finally
        {
            GB.IsBusy = false;
        }
    }

    //자료
    private async Task GetData()
    {
        try
        {
            using var context = new SDSMDbContext();

            var taxonCoordinateCount = await context.TaxonCoordinateCountViews.FirstOrDefaultAsync();

            SurveyDataCollection.Add(new MapModel { Caption = "식생" });
            SurveyDataCollection.Add(new MapModel { Caption = "식물" });
            SurveyDataCollection.Add(new MapModel { Caption = "양서파충류" });
            SurveyDataCollection.Add(new MapModel { Caption = "어류", Count = taxonCoordinateCount?.FishCount ?? 0 });
            SurveyDataCollection.Add(new MapModel { Caption = "저서동물", Count = taxonCoordinateCount?.BenthosCount ?? 0 });
            SurveyDataCollection.Add(new MapModel { Caption = "조류", Count = taxonCoordinateCount?.BirdCount ?? 0 });
            SurveyDataCollection.Add(new MapModel { Caption = "포유류", Count = taxonCoordinateCount?.MammalCount ?? 0 });
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

            //테마지도
            ThemeMapCollection.AddRange(themeMaps);

        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetData), ex.Message);
            throw;
        }
    }

    //조사자료 포인트
    private async Task<List<MapModel>> GetPoints(string caption)
    {
        using var context = new SDSMDbContext();
        var models = new List<MapModel>();

        try
        {
            switch (caption)
            {
                case "어류":
                    await Task.Run(() =>
                    {
                        var groupeds = context.Fishs.Where(m => m.Lat != null && m.Lng != null)
                                                      .Include(m => m.Subs)
                                                      .GroupBy(m => new { m.Lat, m.Lng })
                                                      .ToList();

                        foreach (var grouped in groupeds)
                        {
                            foreach (var item in grouped)
                            {
                                var model = new MapModel
                                {
                                    Location = new Location(item.Lat ?? 0, item.Lng ?? 0),
                                    Caption = caption,
                                    Label = $"어{ProcessYearChsu(item.YearChsu):00}",
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
                                    IsSpeciesCount = true,
                                    IsIndividualCount = true,
                                };
                                models.Add(model);
                            }
                        }
                    });
                    break;
                case "저서동물":
                    await Task.Run(() =>
                    {
                        var groupeds = context.Benthoss.Where(m => m.Lat != null && m.Lng != null)
                                                         .Include(m => m.Subs)
                                                         .GroupBy(m => new { m.Lat, m.Lng })
                                                         .ToList();

                        foreach (var grouped in groupeds)
                        {
                            foreach (var item in grouped)
                            {
                                var model = new MapModel
                                {
                                    Location = new Location(item.Lat ?? 0, item.Lng ?? 0),
                                    Caption = caption,
                                    Label = $"저{ProcessYearChsu(item.YearChsu):00}",
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
                                    IsSpeciesCount = true,
                                    IsIndividualCount = true,
                                };
                                models.Add(model);
                            }
                        }
                    });
                    break;
                case "조류":
                    await Task.Run(() =>
                    {
                        var groupeds = context.Birds.Where(m => m.Lat != null && m.Lng != null).GroupBy(m => new { m.Lat, m.Lng }).ToList();

                        foreach (var grouped in groupeds)
                        {
                            foreach (var item in grouped)
                            {
                                var model = new MapModel
                                {
                                    Location = new Location(item.Lat ?? 0, item.Lng ?? 0),
                                    Caption = caption,
                                    Label = $"조{ProcessYearChsu(item.YearChsu):00}",
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
                                models.Add(model);
                            }
                        }
                    });
                    break;
                case "포유류":
                    await Task.Run(() =>
                    {
                        var groupeds = context.Mammals.Where(m => m.Lat != null && m.Lng != null).GroupBy(m => new { m.Lat, m.Lng }).ToList();

                        foreach (var grouped in groupeds)
                        {
                            foreach (var item in grouped)
                            {
                                var model = new MapModel
                                {
                                    Location = new Location(item.Lat ?? 0, item.Lng ?? 0),
                                    Caption = caption,
                                    Label = $"포{ProcessYearChsu(item.YearChsu):00}",
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
                                models.Add(model);
                            }
                        }
                    });
                    break;
                case "서식수변":
                    await Task.Run(() =>
                    {
                        var groupeds = context.HabitatWaterEdges.Where(m => m.Lat != null && m.Lng != null).GroupBy(m => new { m.Lat, m.Lng }).ToList();

                        foreach (var grouped in groupeds)
                        {
                            foreach (var item in grouped)
                            {
                                var model = new MapModel
                                {
                                    Location = new Location(item.Lat ?? 0, item.Lng ?? 0),
                                    Caption = caption,
                                    Label = $"서{ProcessYearChsu(item.YearChsu):00}",
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
                                models.Add(model);
                            }
                        }
                    });
                    break;
                case "수질":
                    await Task.Run(() =>
                    {
                        var groupeds = context.WaterQualitys.Where(m => m.Lat != null && m.Lng != null).GroupBy(m => new { m.Lat, m.Lng }).ToList();

                        foreach (var grouped in groupeds)
                        {
                            foreach (var item in grouped)
                            {
                                var model = new MapModel
                                {
                                    Location = new Location(item.Lat ?? 0, item.Lng ?? 0),
                                    Caption = caption,
                                    Label = $"수{ProcessYearChsu(item.YearChsu):00}",
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
                                models.Add(model);
                            }
                        }
                    });
                    break;
                default:
                    break;
            }

            return models;
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetPoints), ex.Message);
            return models;
        }
    }

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
