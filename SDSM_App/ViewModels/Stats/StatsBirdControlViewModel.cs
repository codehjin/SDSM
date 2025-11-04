using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SDSM_App.Animations;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using SDSM_Models;
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

    //출현종
    [ObservableProperty] private RadObservableCollection<PlotItem> _speciesCollection = [];

    //특징
    [ObservableProperty] private RadObservableCollection<PlotItem> _featureCollection = [];

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

        Data = await context.Birds.ToListAsync();

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

        var tasks = new[] { GetYearData(), GetSpeciesData(), GetFeatureData() };

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

    //출현종
    private async Task GetSpeciesData()
    {
        try
        {
            SpeciesCollection.Clear();

            var models = new List<PlotItem>();

            await Task.Run(() =>
            {
                var items = SearchData.GroupBy(m => m.SurveyYear).Select(m => new PlotItem { XCat = m.Key, Count = m.Count() }).OrderBy(m => m.XCat).ToList();

                foreach (var item in items)
                {
                    var speciesCount = SearchData.Where(m => m.SurveyYear == item.XCat).GroupBy(m => m.SpeciesKo).Count();

                    models.Add(new PlotItem { XCatText = $"{item.XCat}년", YVal1 = speciesCount });
                }

                var speciess = SearchData.GroupBy(m => m.SpeciesKo).Count();

                models.Add(new PlotItem { XCatText = "젼체(년)", YVal1 = speciess });
            });

            SpeciesCollection.AddRange(models);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetSpeciesData), ex.Message);
        }
    }

    //특징
    private async Task GetFeatureData()
    {
        try
        {
            FeatureCollection.Clear();

            var models = new List<PlotItem>();

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
                        YRatio1 = Math.Round(((double)item.Count / totalCount) * 100, 2),
                    });
                }
            });

            //비율로 내림차순 정렬
            FeatureCollection.AddRange(models.OrderByDescending(m => m.YVal2));
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetFeatureData), ex.Message);
        }
    }

    #endregion 
}
