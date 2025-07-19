using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
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
        InitialLoad();
    }

    #endregion

    #region 속성

    [ObservableProperty] private double _zoom = 9;
    [ObservableProperty] private Location _center = new(36.718243, 127.427757);

    [ObservableProperty] private string _degress = string.Empty;

    [ObservableProperty] private RadObservableCollection<MapModel> _surveyDataCollection = [];
    [ObservableProperty] private RadObservableCollection<MapModel> _pointCollection = [];
    [ObservableProperty] private RadObservableCollection<MapModel> _selectedCollection = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AllClearPointExecutedCommand))]
    private int _pointCount;

    #endregion

    #region ICommand 구현

    //전체 포인트 지우기
    private bool CanAllClearPoint()
    {
        return PointCount > 0;
    }

    //전체 포인트 지우기
    [RelayCommand(CanExecute = nameof(CanAllClearPoint))]
    private void AllClearPointExecuted()
    {
        foreach (var item in SurveyDataCollection.Where(m => m.IsSelected))
        {
            item.IsSelected = false;
        }

        PointCollection.Clear();
        PointCount = 0;
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
                PointCount = PointCollection.Count;

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

            SurveyDataCollection.Add(new MapModel { Caption = "식생"});
            SurveyDataCollection.Add(new MapModel { Caption = "식물"});
            SurveyDataCollection.Add(new MapModel { Caption = "양서파충류"});
            SurveyDataCollection.Add(new MapModel { Caption = "어류"});
            SurveyDataCollection.Add(new MapModel { Caption = "저서동물"});
            SurveyDataCollection.Add(new MapModel { Caption = "조류"});
            SurveyDataCollection.Add(new MapModel { Caption = "포유류"});
            SurveyDataCollection.Add(new MapModel { Caption = "서식수변"});
            SurveyDataCollection.Add(new MapModel { Caption = "수질"});
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetData), ex.Message);
            throw;
        }
    }

    //조류
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
                                    Label = $"어{item.SurveyDate?.Year.ToString().Substring(2, 2)}",
                                    MarkerFill = Application.Current.FindResource("FishBrush") as Brush,
                                    YearChsu = item.YearChsu,
                                    SurveyDate = item.SurveyDate,
                                    River = item.River,
                                    Site = item.Site,
                                    Lat = item.Lat,
                                    Lng = item.Lng,
                                    Surveyor = item.Surveyor,
                                    FAIScore = item.FAIScore,
                                    FAIGrade = item.FAIGrade,
                                    SpeciesCount = item.Subs.Count,
                                    IndividualCount = item.Subs.Sum(m => m.IndividualCount),
                                    IsFAI = true,
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
                                                         .Include(m=> m.Subs)
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
                                    Label = $"저{item.SurveyDate?.Year.ToString().Substring(2, 2)}",
                                    MarkerFill = Application.Current.FindResource("BenthosBrush") as Brush,
                                    YearChsu = item.YearChsu,
                                    SurveyDate = item.SurveyDate,
                                    River = item.River,
                                    Site = item.Site,
                                    Lat = item.Lat,
                                    Lng = item.Lng,
                                    Surveyor = item.Surveyor,
                                    BMIScore = item.BMIScore,
                                    BMIGrade = item.BMIGrade,
                                    SpeciesCount = item.Subs.Count,
                                    IndividualCount = item.Subs.Sum(m => m.IndividualCount),
                                    IsBMI = true,
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
                                    Label = $"조{item.SurveyDate?.Year.ToString().Substring(2, 2)}",
                                    MarkerFill = Application.Current.FindResource("BirdBrush") as Brush,
                                    YearChsu = item.YearChsu,
                                    SurveyDate = item.SurveyDate,
                                    River = item.River,
                                    Site = item.Site,
                                    Lat = item.Lat,
                                    Lng = item.Lng,
                                    Surveyor = item.Surveyor,
                                    SpeciesKo= item.SpeciesKo,
                                    SpeciesEn =item.SpeciesEn,
                                    IndividualCount =item.IndividualCount,
                                    IsSpecies =true,
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
                                    Label = $"포{item.SurveyDate?.Year.ToString().Substring(2, 2)}",
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
                                    Label = $"서{item.SurveyDate?.Year.ToString().Substring(2, 2)}",
                                    MarkerFill = Application.Current.FindResource("HabitatWaterEdgeBrush") as Brush,
                                    YearChsu = item.YearChsu,
                                    SurveyDate = item.SurveyDate,
                                    River = item.River,
                                    Site = item.Site,
                                    Lat = item.Lat,
                                    Lng = item.Lng,
                                    Surveyor = item.Surveyor,
                                    EvaluationScore = item.EvaluationScore,
                                    EvaluationGrade = item.EvaluationGrade,
                                    IsHabitatWaterEdge = true,
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
                                    Label = $"수{item.SurveyDate?.Year.ToString().Substring(2, 2)}",
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

    #endregion 
}
