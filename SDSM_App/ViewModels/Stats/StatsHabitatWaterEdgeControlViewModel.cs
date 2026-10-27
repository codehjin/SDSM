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

public partial class StatsHabitatWaterEdgeControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public StatsHabitatWaterEdgeControlViewModel()
    {
        InitialLoad();
    }

    #endregion

    #region 속성

    [ObservableProperty] private string _searchCondition = "전체";

    public List<HabitatWaterEdge> Data { get; set; } = [];
    public List<HabitatWaterEdge> SearchData { get; set; } = [];

    //출력
    [ObservableProperty] private RadObservableCollection<SearchItem> _exportCollection = [];

    //검색목록
    [ObservableProperty] private RadObservableCollection<int> _searchCollection = [];
    [ObservableProperty] private RadObservableCollection<int> _searchSelectedItems = [];

    //연도별 자료수
    [ObservableProperty] private RadObservableCollection<PlotItem> _yearCollection = [];

    //평가등급
    [ObservableProperty] private DataTable _gradeCollection = new();

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

        Data = await context.HabitatWaterEdges.Where(m => m.SiteDivision.Project == "방류하천").ToListAsync();

        var years = await GetSearchData(Data);

        SearchCollection.AddRange(years);
        SearchSelectedItems.AddRange(years);

        await GetData();

        await GetModels();

        if (GB.IsBusy == true)
            GB.IsBusy = false;
    }

    //검색목록
    private async Task<List<int>> GetSearchData(List<HabitatWaterEdge> data)
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
                            .Select(m => new HabitatWaterEdge
                            {
                                SiteDivision = m.SiteDivision,
                                SurveyYear = m.SurveyYear,
                                YearChsu = m.YearChsu,
                                River = m.River,
                                EvaluationScore = m.EvaluationScore,
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

        var tasks = new[] { GetYearData(), GetGradeData() };

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

    //평가등급
    private async Task GetGradeData()
    {
        try
        {
            GradeCollection = new();
            var dt = new DataTable();

            await Task.Run(() =>
            {
                var years = SearchData.GroupBy(m => m.SurveyYear).Select(m => m.Key).OrderBy(m => m).ToList();
                var rivers = SearchData.GroupBy(m => m.River).Select(m => m.Key).OrderBy(m => m).ToList();

                dt.Columns.Add(new DataColumn("연도", typeof(string)));

                //컬럼 추가 : 하천명
                foreach (var river in rivers)
                {
                    var riverName = string.IsNullOrEmpty(river) ? "기타" : river;

                    var column = new DataColumn(riverName, typeof(string))
                    {
                        AllowDBNull = true
                    };
                    dt.Columns.Add(column);
                }

                //기타 컬럼 맨뒤로
                var etcColumn = dt.Columns["기타"];
                if (etcColumn != null)
                {
                    dt.Columns.Remove(etcColumn);
                    dt.Columns.Add(etcColumn);
                }

                //데이터 추가 : 연도별, 하천명별
                foreach (var year in years)
                {
                    var row = dt.NewRow();
                    row["연도"] = $"{year}년";

                    foreach (var river in rivers)
                    {
                        var riverName = string.IsNullOrEmpty(river) ? "기타" : river;
                        //평가점수가 있는 기록만 평균 — 빈 점수(조사불가 · 미입력)는 합 · 나누는 수 모두 제외(CLAUDE.md §7.2 · 2026-10-28 A4)
                        var scores = SearchData.Where(m => m.SurveyYear == year && m.River == river && m.EvaluationScore.HasValue)
                                               .Select(m => m.EvaluationScore!.Value).ToList();

                        if (scores.Count != 0)
                        {
                            var grade = string.Empty;
                            var score = Math.Round(scores.Sum() / scores.Count, 1, MidpointRounding.AwayFromZero);

                            if (score >= 80) grade = "A";
                            else if (score >= 60) grade = "B";
                            else if (score >= 40) grade = "C";
                            else if (score >= 20) grade = "D";
                            else if (score >= 0) grade = "E";

                            row[riverName] = $"{grade}({score:00.0})";
                        }
                        else
                        {
                            row[riverName] = "-";
                        }
                    }
                    dt.Rows.Add(row);
                }

                GradeCollection = dt;
            });
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetGradeData), ex.Message);
        }
    }

    #endregion
}
