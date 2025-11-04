using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SDSM_App.Animations;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using SDSM_Models;
using Telerik.Windows.Controls.Charting;
using Telerik.Windows.Data;

namespace SDSM_App.ViewModels;

public partial class StatsVegetationControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public StatsVegetationControlViewModel()
    {
        InitialLoad();
    }

    #endregion

    #region 속성

    [ObservableProperty] private string _searchCondition = "전체";

    public List<Vegetation> Data { get; set; } = [];
    public List<Vegetation> SearchData { get; set; } = [];

    //출력
    [ObservableProperty] private RadObservableCollection<SearchItem> _exportCollection = [];

    //검색목록
    [ObservableProperty] private RadObservableCollection<int> _searchCollection = [];
    [ObservableProperty] private RadObservableCollection<int> _searchSelectedItems = [];

    //연도별 자료수
    [ObservableProperty] private RadObservableCollection<PlotItem> _yearCollection = [];

    //군락
    [ObservableProperty] private RadObservableCollection<PlotItem> _colonyCountCollection = [];
    [ObservableProperty] private RadObservableCollection<PlotItem> _colonyNameCollection = [];
    [ObservableProperty] private RadObservableCollection<PlotItem> _colonyNameChartCollection = [];

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
            new(){ Caption = "연도별군락수", IsCheckedTable = false, IsCheckedChart = false },
            new(){ Caption = "군락별면적", IsCheckedTable = false, IsCheckedChart = false },
        ];

        Data = await context.Vegetations.ToListAsync();

        var years = await GetSearchData(Data);

        SearchCollection.AddRange(years);
        SearchSelectedItems.AddRange(years);

        await GetData();

        await GetModels();

        if (GB.IsBusy == true)
            GB.IsBusy = false;
    }

    //검색목록
    private async Task<List<int>> GetSearchData(List<Vegetation> data)
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
                            .Select(m => new Vegetation
                            {
                                SurveyYear = m.SurveyYear,
                                YearChsu = m.YearChsu,
                                Colony = m.Colony,
                                Area = m.Area
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

        var tasks = new[] { GetYearData(), GetColonyCountData(), GetColonyNameData() };

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

    //연도별 군락수
    private async Task GetColonyCountData()
    {
        try
        {
            ColonyCountCollection.Clear();

            var models = new List<PlotItem>();

            await Task.Run(() =>
            {
                var items = SearchData.GroupBy(m => m.SurveyYear).Select(m => new PlotItem { XCat = m.Key, Count = m.Count() }).OrderBy(m => m.XCat).ToList();

                foreach (var item in items)
                {
                    var colonyCount = SearchData.Where(m => m.SurveyYear == item.XCat).GroupBy(m => m.Colony).Count();
                    var areaSum = SearchData.Where(m => m.SurveyYear == item.XCat).Sum(m => m.Area) ?? 0;

                    models.Add(new PlotItem { XCatText = $"{item.XCat}년", YVal1 = colonyCount, YRatio1 = areaSum });
                }

                var colonys = SearchData.GroupBy(m => m.Colony).Count();
                var areas = SearchData.Sum(m => m.Area) ?? 0;

                models.Add(new PlotItem { XCatText = "젼체(년)", YVal1 = colonys, YRatio1 = areas });
            });

            ColonyCountCollection.AddRange(models);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetColonyCountData), ex.Message);
        }
    }

    //군락별 면적
    private async Task GetColonyNameData()
    {
        try
        {
            ColonyNameCollection.Clear();
            ColonyNameChartCollection.Clear();

            var models = new List<PlotItem>();
            var chartModels = new List<PlotItem>();

            await Task.Run(() =>
            {
                var items = SearchData.GroupBy(m => m.Colony).Select(m => new PlotItem { XCatText = m.Key, Sum = m.Sum(m => m.Area) ?? 0 }).OrderByDescending(m => m.Sum).ToList();

                foreach (var item in items)
                {
                    models.Add(new PlotItem { XCatText = item.XCatText, YRatio1 = item.Sum });
                }

                if(models.Count > 10)
                {
                    var topItems = items.Take(10).ToList();
                    var othersSum = items.Skip(10).Sum(m => m.Sum);
                    foreach (var item in topItems)
                    {
                        chartModels.Add(new PlotItem { XCatText = item.XCatText, YRatio1 = item.Sum });
                    }
                    chartModels.Add(new PlotItem { XCatText = "기타", YRatio1 = othersSum });
                }
                else
                {
                    chartModels = models;
                }
            });

            ColonyNameCollection.AddRange(models);
            ColonyNameChartCollection.AddRange(chartModels);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetColonyNameData), ex.Message);
        }
    }

    #endregion 
}
