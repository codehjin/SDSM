using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SDSM_App.Animations;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using SDSM_Models;
using System.Data;
using Telerik.Windows.Data;

namespace SDSM_App.ViewModels;

public partial class StatsBirdControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public StatsBirdControlViewModel()
    {
        InitialLoad();
    }

    #endregion

    #region 속성

    [ObservableProperty] private string _searchCondition = "전체";

    public List<Bird> Data { get; set; } = [];
    public List<Bird> SearchData { get; set; } = [];

    //출력
    [ObservableProperty] private RadObservableCollection<SearchItem> _exportCollection = [];

    //검색목록
    [ObservableProperty] private RadObservableCollection<int> _searchCollection = [];
    [ObservableProperty] private RadObservableCollection<int> _searchSelectedItems = [];

    //연도별 자료수
    [ObservableProperty] private RadObservableCollection<PlotItem> _yearCollection = [];

    //전체 사업장, 대표하천, 종 수
    [ObservableProperty] private int _projectTotalSpecies;
    [ObservableProperty] private int _projectTotalWorkplace;
    [ObservableProperty] private int _projectTotalWorkplaceRiver;

    //사업장 대표하천별 과,종
    [ObservableProperty] private DataTable _projectDataTable = new();
    [ObservableProperty] private RadObservableCollection<string> _projectSpeciesCollection = [];

    //특징
    [ObservableProperty] private int _featureTotalSpecies;
    [ObservableProperty] private RadObservableCollection<PlotItem> _featureCollection = [];
    [ObservableProperty] private RadObservableCollection<PlotItem> _featureSpeciesCollection = [];

    #endregion

    #region ICommand 구현

    [RelayCommand]
    private async Task SearchExecuted()
    {
        if (GB.IsBusy == false)
            GB.IsBusy = true;

        await GetData();

        await GetModels();

        if (GB.IsBusy == true)
            GB.IsBusy = false;
    }

    #endregion

    #region 매서드

    private async void InitialLoad()
    {
        if (GB.IsBusy == false)
            GB.IsBusy = true;

        using var context = new SDSMDbContext();

        Data = await context.Birds.Where(m => m.SiteDivision.Project == "방류하천").ToListAsync();

        var years = await GetSearchData(Data);

        SearchCollection.AddRange(years);
        SearchSelectedItems.AddRange(years);

        await GetData();

        await GetModels();

        if (GB.IsBusy == true)
            GB.IsBusy = false;
    }

    //검색목록
    private async Task<List<int>> GetSearchData(List<Bird> data)
    {
        return await Task.Run(() =>
        {
            return data.Select(m => m.SurveyYear).Distinct().OrderBy(m => m).ToList();
        });
    }

    private async Task GetData()
    {
        await Task.Run(() =>
        {
            SearchData = [];

            if (SearchSelectedItems.Count != 0)
            {
                foreach (var item in SearchSelectedItems)
                {
                    SearchData.AddRange(
                        Data.Where(m => m.SurveyYear == item)
                            .Select(m => new Bird
                            {
                                SiteDivision = m.SiteDivision,
                                SurveyYear = m.SurveyYear,
                                SpeciesKo = m.SpeciesKo,
                                Feature = m.Feature,
                            })
                    );
                }
            }

            SearchCondition = SearchCollection.Count == SearchSelectedItems.Count ? "전체" : $"{SearchSelectedItems.Count}개";
        });
    }

    private async Task GetModels()
    {
        if (GB.IsBusy == false)
            GB.IsBusy = true;

        var tasks = new[] { GetYearData(), GetProjectData(), GetFeatureData() };

        try
        {
            await Task.WhenAll(tasks);

            if (GB.IsBusy == true)
                GB.IsBusy = false;
        }
        catch
        {
            if (GB.IsBusy == true)
                GB.IsBusy = false;
        }
    }

    //연도별 자료수
    private async Task GetYearData()
    {
        try
        {
            YearCollection.Clear();

            var models = new List<PlotItem>();

            await Task.Run(() =>
            {
                var items = SearchData.GroupBy(m => m.SurveyYear).Select(m => new PlotItem { XCat = m.Key, Count = m.Count() }).OrderBy(m => m.XCat).ToList();

                foreach (var item in items)
                {
                    var model = new PlotItem
                    {
                        XCat = item.XCat,
                        YVal1 = item.Count,
                    };
                    models.Add(model);
                }
            });

            YearCollection.AddRange(models);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetYearData), ex.Message);
        }
    }

    //사업장 대표하천별 과,종
    private async Task GetProjectData()
    {

        try
        {
            ProjectDataTable = new();
            var dt = new DataTable();

            var models = new List<PlotItem>();
            var speciesModels = new List<string>();

            await Task.Run(() =>
            {
                //사업장 대표하천
                ProjectTotalWorkplace = SearchData.GroupBy(m => m.SiteDivision.Workplace).Count();
                ProjectTotalWorkplaceRiver = SearchData.GroupBy(m => m.SiteDivision.WorkplaceRiver).Count();

                //출현종
                speciesModels.AddRange(SearchData.GroupBy(m => m.SpeciesKo).Select(m => m.Key).OrderBy(m => m));
                ProjectTotalSpecies = speciesModels.Count;

                var years = SearchData.GroupBy(m => m.SurveyYear).Select(m => m.Key).OrderBy(m => m).ToList();
                var workplaceGroups = SearchData.GroupBy(m => m.SiteDivision.Workplace).Select(m => m.Key).OrderBy(m => m).ToList();

                dt.Columns.Add(new DataColumn("사업장", typeof(string)));
                dt.Columns.Add(new DataColumn("과업대표하천", typeof(string)));

                foreach (var year in years)
                {
                    dt.Columns.Add(new DataColumn($"{year}년_종수", typeof(string)));
                    dt.Columns.Add(new DataColumn($"{year}년_출현종", typeof(string)));
                }

                foreach (var workplaceGroup in workplaceGroups)
                {
                    var workplaceRivers = SearchData.Where(m => m.SiteDivision.Workplace == workplaceGroup)
                                                      .GroupBy(m => m.SiteDivision.WorkplaceRiver)
                                                      .Select(m => m.Key)
                                                      .OrderBy(m => m)
                                                      .ToList();

                    foreach (var workplaceRiver in workplaceRivers)
                    {
                        foreach (var year in years)
                        {
                            var data = SearchData.Where(m => m.SiteDivision.Workplace == workplaceGroup && m.SiteDivision.WorkplaceRiver == workplaceRiver && m.SurveyYear == year).ToList();
                            var speciesCount = 0;

                            //출현종
                            var speciesList = data.Select(m => m.SpeciesKo).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
                            // 콤마로 연결한 문자열로 변환
                            var speciesCsv = speciesList.Count > 0 ? string.Join(", ", speciesList) : string.Empty;

                            if (data.Count > 0)
                            {
                                speciesCount = data.GroupBy(m => m.SpeciesKo).Count();
                            }

                            var yearName = $"{year}년_종수";
                            var yearSpecies = $"{year}년_출현종";

                            var row = dt.NewRow();

                            row["사업장"] = workplaceGroup;
                            row["과업대표하천"] = workplaceRiver;
                            row[yearName] = $"{speciesCount}종";
                            row[yearSpecies] = speciesCsv;

                            dt.Rows.Add(row);
                        }
                    }
                }
            });

            ProjectDataTable = dt;
            ProjectSpeciesCollection.AddRange(speciesModels);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetProjectData), ex.Message);
        }
    }

    //특징
    private async Task GetFeatureData()
    {
        try
        {
            FeatureCollection.Clear();
            FeatureSpeciesCollection.Clear();

            var models = new List<PlotItem>();
            var speciesModels = new List<PlotItem>();

            await Task.Run(() =>
            {
                var items = SearchData.Where(m => !string.IsNullOrWhiteSpace(m.Feature))
                                       .GroupBy(m => m.Feature)
                                       .Select(m => new PlotItem { XCatText = m.Key ?? "", Count = m.Count() })
                                       .ToList();

                //출현종수
                FeatureTotalSpecies = SearchData.Where(m => !string.IsNullOrWhiteSpace(m.Feature)).GroupBy(m => m.SpeciesKo).Count();

                //전체자료에서의 비율
                var totalCount = SearchData.Count;

                foreach (var item in items)
                {
                    models.Add(new PlotItem
                    {
                        XCatText = item.XCatText,
                        YVal1 = item.Count,
                        YRatio1 = Math.Round(((double)item.Count / totalCount) * 100, 2, MidpointRounding.AwayFromZero),
                    });

                    speciesModels.AddRange(SearchData.Where(m => m.Feature == item.XCatText).GroupBy(m => m.SpeciesKo).Select(m => new PlotItem { Category = item.XCatText, SubCategory = m.Key }));
                }
            });

            //비율로 내림차순 정렬
            FeatureCollection.AddRange(models.OrderByDescending(m => m.YVal2));

            FeatureSpeciesCollection.AddRange(speciesModels.OrderBy(m => m.Category).ThenBy(m => m.SubCategory));
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetFeatureData), ex.Message);
        }
    }

    #endregion 
}
