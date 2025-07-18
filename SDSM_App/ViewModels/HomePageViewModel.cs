using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using SDSM_Models;
using System.Windows;
using System.Windows.Media;
using Telerik.Windows.Controls.Map;
using Telerik.Windows.Data;
using Telerik.Windows.Documents.Spreadsheet.Expressions.Functions;

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
                string speciesType = selectedModel.SpeciesType;
                bool isChecked = selectedModel.IsSelected;

                switch (speciesType)
                {
                    case "조류":
                        {
                            if (isChecked == true)
                            {
                                models = await GetBirdPoints();
                            }
                            else
                            {
                                var points = PointCollection.Where(m => m.Caption == caption);

                                if (points.Any())
                                {
                                    PointCollection.RemoveRange(points);
                                    //BirdModelCollection.Clear();

                                    SelectedRemovePointExecutedCommand.Execute([.. points]);
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

    //선택 제거 포인트
    [RelayCommand]
    private void SelectedRemovePointExecuted(List<MapModel> removeModels)
    {
        if (removeModels != null && removeModels.Count > 0)
        {
            //var plants = new List<Plant>();
            //var birds = new List<Bird>();
            //var anrs = new List<AnR>();
            //var mammals = new List<Mammal>();
            //var insects = new List<Insect>();
            //var fishs = new List<Fish>();
            //var bms = new List<BM>();
            //var endangereds = new List<Endangered>();
            //var nurseTrees = new List<NurseTree>();

            //foreach (var removeModel in removeModels)
            //{
            //    plants.AddRange(PlantModelCollection.Where(m => m.Lat == removeModel.Location.Latitude && m.Lng == removeModel.Location.Longitude));
            //    birds.AddRange(BirdModelCollection.Where(m => m.Lat == removeModel.Location.Latitude && m.Lng == removeModel.Location.Longitude));
            //    anrs.AddRange(AnRModelCollection.Where(m => m.Lat == removeModel.Location.Latitude && m.Lng == removeModel.Location.Longitude));
            //    mammals.AddRange(MammalModelCollection.Where(m => m.Lat == removeModel.Location.Latitude && m.Lng == removeModel.Location.Longitude));
            //    insects.AddRange(InsectModelCollection.Where(m => m.Lat == removeModel.Location.Latitude && m.Lng == removeModel.Location.Longitude));
            //    fishs.AddRange(FishModelCollection.Where(m => m.Lat == removeModel.Location.Latitude && m.Lng == removeModel.Location.Longitude));
            //    bms.AddRange(BMModelCollection.Where(m => m.Lat == removeModel.Location.Latitude && m.Lng == removeModel.Location.Longitude));
            //    endangereds.AddRange(EndangeredModelCollection.Where(m => m.Lat == removeModel.Location.Latitude && m.Lng == removeModel.Location.Longitude));
            //    nurseTrees.AddRange(NurseTreeModelCollection.Where(m => m.Lat == removeModel.Location.Latitude && m.Lng == removeModel.Location.Longitude));
            //}

            //if (plants.Count > 0)
            //    PlantModelCollection.RemoveRange(plants);

            //if (birds.Count > 0)
            //    BirdModelCollection.RemoveRange(birds);

            //if (anrs.Count > 0)
            //    AnRModelCollection.RemoveRange(anrs);

            //if (mammals.Count > 0)
            //    MammalModelCollection.RemoveRange(mammals);

            //if (insects.Count > 0)
            //    InsectModelCollection.RemoveRange(insects);

            //if (fishs.Count > 0)
            //    FishModelCollection.RemoveRange(fishs);

            //if (bms.Count > 0)
            //    BMModelCollection.RemoveRange(bms);

            //if (endangereds.Count > 0)
            //    EndangeredModelCollection.RemoveRange(endangereds);

            //if (nurseTrees.Count > 0)
            //    NurseTreeModelCollection.RemoveRange(nurseTrees);
        }
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

            SurveyDataCollection.Add(new MapModel { Caption = "식생", SpeciesType = "식생", MarkerFill = Application.Current.FindResource("VegetationBrush") as Brush });
            SurveyDataCollection.Add(new MapModel { Caption = "식물", SpeciesType = "식물", MarkerFill = Application.Current.FindResource("PlantBrush") as Brush });
            SurveyDataCollection.Add(new MapModel { Caption = "양서파충류", SpeciesType = "양서파충류", MarkerFill = Application.Current.FindResource("AmphibianReptileBrush") as Brush });
            SurveyDataCollection.Add(new MapModel { Caption = "어류", SpeciesType = "어류", MarkerFill = Application.Current.FindResource("FishBrush") as Brush });
            SurveyDataCollection.Add(new MapModel { Caption = "저서동물", SpeciesType = "저서동물", MarkerFill = Application.Current.FindResource("BenthosBrush") as Brush });
            SurveyDataCollection.Add(new MapModel { Caption = "조류", SpeciesType = "조류", MarkerFill = Application.Current.FindResource("BirdBrush") as Brush });
            SurveyDataCollection.Add(new MapModel { Caption = "포유류", SpeciesType = "포유류", MarkerFill = Application.Current.FindResource("MammalBrush") as Brush });
            SurveyDataCollection.Add(new MapModel { Caption = "서식수변", SpeciesType = "서식수변", MarkerFill = Application.Current.FindResource("HabitatWaterEdgeBrush") as Brush });
            SurveyDataCollection.Add(new MapModel { Caption = "수질", SpeciesType = "수질", MarkerFill = Application.Current.FindResource("WaterQualityBrush") as Brush });
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetData), ex.Message);
            throw;
        }
    }

    //조류
    private async Task<List<MapModel>> GetBirdPoints()
    {
        using var context = new SDSMDbContext();
        var models = new List<MapModel>();

        try
        {
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
                            Caption = $"조류_{item.SurveyDate?.Year}년",
                            Label = $"조{item.SurveyDate?.Year.ToString().Substring(2, 2)}",
                            MarkerFill = Application.Current.FindResource("BirdBrush") as Brush,
                        };
                        models.Add(model);
                    }
                }

                //if (query.Count == 0)
                //    return models;

                //models = context.Birds.Where( query.Any(m=> query)(m => new { m.Lat, m.Lng }).Select(m => new MapModel
                //{
                //    Location = new Location(m.Key.Lat ?? 0, m.Key.Lng ?? 0),
                //    Caption = $"조류_{m.}년",
                //    Label = $"조{year.ToString().Substring(2, 2)}",
                //    MarkerFill = Application.Current.FindResource("BirdBrush") as Brush,
                //});
            });

            return models;
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetBirdPoints), ex.Message);
            return models;
        }
    }

    #endregion 
}
