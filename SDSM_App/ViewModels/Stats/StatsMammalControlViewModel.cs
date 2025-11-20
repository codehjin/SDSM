using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SDSM_App.Animations;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using SDSM_Models;
using System.Data;
using Telerik.Windows.Data;

namespace SDSM_App.ViewModels;

public partial class StatsMammalControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public StatsMammalControlViewModel()
    {
        InitialLoad();
    }

    #endregion

    #region 속성

    [ObservableProperty] private string _searchCondition = "전체";

    public List<Mammal> Data { get; set; } = [];
    public List<Mammal> SearchData { get; set; } = [];

    //출력
    [ObservableProperty] private RadObservableCollection<SearchItem> _exportCollection = [];

    //검색목록
    [ObservableProperty] private RadObservableCollection<int> _searchCollection = [];
    [ObservableProperty] private RadObservableCollection<int> _searchSelectedItems = [];

    //전체 사업장, 대표하천, 종 수
    [ObservableProperty] private int _totalSpecies;
    [ObservableProperty] private int _totalWorkplace;
    [ObservableProperty] private int _totalWorkplaceRiver;

    //사업장 대표하천별 과,종
    [ObservableProperty] private DataTable _projectDataTable = new();
    [ObservableProperty] private RadObservableCollection<string> _projectSpeciesCollection = [];

    //특징
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

        ExportCollection =
        [
            new(){ Caption = "연도별자료", IsCheckedTable = false, IsCheckedChart = false },
            new(){ Caption = "출현종", IsCheckedTable = false, IsCheckedChart = false },
            new(){ Caption = "특징", IsCheckedTable = false, IsCheckedChart = false },
        ];

        Data = await context.Mammals.Where(m => m.SiteDivision.Project == "방류하천").ToListAsync();

        var years = await GetSearchData(Data);

        SearchCollection.AddRange(years);
        SearchSelectedItems.AddRange(years);

        await GetData();

        await GetModels();

        if (GB.IsBusy == true)
            GB.IsBusy = false;
    }

    //검색목록
    private async Task<List<int>> GetSearchData(List<Mammal> data)
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
                            .Select(m => new Mammal
                            {
                                SiteDivision = m.SiteDivision,
                                SurveyYear = m.SurveyYear,
                                YearChsu = m.YearChsu,
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

        var tasks = new[] { GetProjectData(), GetFeatureData() };

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
                TotalSpecies = SearchData.GroupBy(m => m.SpeciesKo).Count();
                TotalWorkplace = SearchData.GroupBy(m => m.SiteDivision.Workplace).Count();
                TotalWorkplaceRiver = SearchData.GroupBy(m => m.SiteDivision.WorkplaceRiver).Count();

                speciesModels.AddRange(SearchData.GroupBy(m => m.SpeciesKo).Select(m => m.Key).OrderBy(m => m));

                var years = SearchData.GroupBy(m => m.SurveyYear).Select(m => m.Key).OrderBy(m => m).ToList();
                var workplaceGroups = SearchData.GroupBy(m => m.SiteDivision.Workplace).Select(m => m.Key).OrderBy(m => m).ToList();

                dt.Columns.Add(new DataColumn("사업장", typeof(string)));
                dt.Columns.Add(new DataColumn("과업_대표하천", typeof(string)));

                foreach (var year in years)
                {
                    dt.Columns.Add(new DataColumn($"{year}년", typeof(string)));
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

                            if (data.Count > 0)
                            {
                                speciesCount = data.GroupBy(m => m.SpeciesKo).Count();
                            }

                            var yearName = $"{year}년";

                            var row = dt.NewRow();

                            row["사업장"] = workplaceGroup;
                            row["과업_대표하천"] = workplaceRiver;
                            row[yearName] = $"{speciesCount}종";

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
