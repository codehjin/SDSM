using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SDSM_App.Animations;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using SDSM_Models;
using System.Data;
using System.Text;
using Telerik.Windows.Data;

namespace SDSM_App.ViewModels;

public partial class StatsWaterQualityControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public StatsWaterQualityControlViewModel()
    {
        InitialLoad();
    }

    #endregion

    #region 속성

    [ObservableProperty] private string _searchCondition = "전체";

    public List<WaterQuality> Data { get; set; } = [];
    public List<WaterQuality> SearchData { get; set; } = [];

    //출력
    [ObservableProperty] private RadObservableCollection<SearchItem> _exportCollection = [];

    //검색목록
    [ObservableProperty] private RadObservableCollection<int> _searchCollection = [];
    [ObservableProperty] private RadObservableCollection<int> _searchSelectedItems = [];

    //연도별 자료수
    [ObservableProperty] private RadObservableCollection<PlotItem> _yearCollection = [];

    //수질등급
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

        Data = await context.WaterQualitys.Where(m => m.SiteDivision.Project == "방류하천").ToListAsync();

        var years = await GetSearchData(Data);

        SearchCollection.AddRange(years);
        SearchSelectedItems.AddRange(years);

        await GetData();

        await GetModels();

        if (GB.IsBusy == true)
            GB.IsBusy = false;
    }

    //검색목록
    private async Task<List<int>> GetSearchData(List<WaterQuality> data)
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
                            .Select(m => new WaterQuality
                            {
                                SiteDivision = m.SiteDivision,
                                SurveyYear = m.SurveyYear,
                                YearChsu = m.YearChsu,
                                River = m.River,
                                PH = m.PH,
                                BOD = m.BOD,
                                COD = m.COD,
                                TOC = m.TOC,
                                SS = m.SS,
                                DO = m.DO,
                                TP = m.TP,
                                EColi = m.EColi,
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

    //수질등급
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
                string[] grades = ["pH", "BOD", "COD","TOC","SS","DO","T-P", "대장균군"];

                dt.Columns.Add(new DataColumn("연도", typeof(string)));

                //컬럼 추가 : 하천명
                foreach (var river in rivers)
                {
                    foreach (var grade in grades)
                    {
                        var riverName = string.IsNullOrEmpty(river) ? "기타" : river;

                        var column = new DataColumn($"{riverName}_{grade}", typeof(string)) { AllowDBNull = true };
                        dt.Columns.Add(column);
                    }
                }

                //기타 컬럼 목록 만들기
                var etcColumns = new List<DataColumn>();    
                foreach (DataColumn column in dt.Columns)
                {
                    if (column.ColumnName.Contains("기타"))
                    {
                        etcColumns.Add(column);
                    }
                }

                //기타 컬럼 지우고 추가하기
                foreach (var etcColumn in etcColumns)
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
                        var grade = string.Empty;
                        var columnName = string.Empty;
                        var data = new List<double?>();

                        //PH
                        columnName = $"{riverName}_pH";
                        data = [.. SearchData.Where(m => m.SurveyYear == year && m.River == river && m.PH != null).Select(m=>m.PH)];                        
                        if (data.Count != 0)
                        {
                            var scoreSum = data.Sum(m => m) ?? 0;
                            var score = Math.Round((double)scoreSum / data.Count, 1, MidpointRounding.AwayFromZero);
                            grade = GB.GetPhGrade(score).ToString();
                            row[columnName] = grade;
                        }
                        else
                            row[columnName] = "-";

                        //BOD
                        columnName = $"{riverName}_BOD";
                        data = [.. SearchData.Where(m => m.SurveyYear == year && m.River == river && m.BOD != null).Select(m => m.BOD)];
                        if (data.Count != 0)
                        {
                            var scoreSum = data.Sum(m => m) ?? 0;
                            var score = Math.Round((double)scoreSum / data.Count, 1, MidpointRounding.AwayFromZero);
                            grade = GB.GetBodGrade(score);
                            row[columnName] = grade;
                        }
                        else
                            row[columnName] = "-";

                        //COD
                        columnName = $"{riverName}_COD";
                        data = [.. SearchData.Where(m => m.SurveyYear == year && m.River == river && m.COD != null).Select(m => m.COD)];
                        if (data.Count != 0)
                        {
                            var scoreSum = data.Sum(m => m) ?? 0;
                            var score = Math.Round((double)scoreSum / data.Count, 1, MidpointRounding.AwayFromZero);
                            grade = GB.GetCodGrade(score);
                            row[columnName] = grade;
                        }
                        else
                            row[columnName] = "-";

                        //TOC
                        columnName = $"{riverName}_TOC";
                        data = [.. SearchData.Where(m => m.SurveyYear == year && m.River == river && m.TOC != null).Select(m => m.TOC)];
                        if (data.Count != 0)
                        {
                            var scoreSum = data.Sum(m => m) ?? 0;
                            var score = Math.Round((double)scoreSum / data.Count, 1, MidpointRounding.AwayFromZero);
                            grade = GB.GetTocGrade(score);
                            row[columnName] = grade;
                        }
                        else
                            row[columnName] = "-";

                        //SS
                        columnName = $"{riverName}_SS";
                        data = [.. SearchData.Where(m => m.SurveyYear == year && m.River == river && m.SS != null).Select(m => m.SS)];
                        if (data.Count != 0)
                        {
                            var scoreSum = data.Sum(m => m) ?? 0;
                            var score = Math.Round((double)scoreSum / data.Count, 1, MidpointRounding.AwayFromZero);
                            grade = GB.GetSsGrade(score).ToString();
                            row[columnName] = grade;
                        }
                        else
                            row[columnName] = "-";

                        //SS
                        columnName = $"{riverName}_SS";
                        data = [.. SearchData.Where(m => m.SurveyYear == year && m.River == river && m.SS != null).Select(m => m.SS)];
                        if (data.Count != 0)
                        {
                            var scoreSum = data.Sum(m => m) ?? 0;
                            var score = Math.Round((double)scoreSum / data.Count, 1, MidpointRounding.AwayFromZero);
                            grade = GB.GetSsGrade(score).ToString();
                            row[columnName] = grade;
                        }
                        else
                            row[columnName] = "-";

                        //DO
                        columnName = $"{riverName}_DO";
                        data = [.. SearchData.Where(m => m.SurveyYear == year && m.River == river && m.DO != null).Select(m => m.DO)];
                        if (data.Count != 0)
                        {
                            var scoreSum = data.Sum(m => m) ?? 0;
                            var score = Math.Round((double)scoreSum / data.Count, 1, MidpointRounding.AwayFromZero);
                            grade = GB.GetDoGrade(score);
                            row[columnName] = grade;
                        }
                        else
                            row[columnName] = "-";

                        //T-P
                        columnName = $"{riverName}_T-P";
                        data = [.. SearchData.Where(m => m.SurveyYear == year && m.River == river && m.TP != null).Select(m => m.TP)];
                        if (data.Count != 0)
                        {
                            var scoreSum = data.Sum(m => m) ?? 0;
                            var score = Math.Round((double)scoreSum / data.Count, 1, MidpointRounding.AwayFromZero);
                            grade = GB.GetTpGrade(score);
                            row[columnName] = grade;
                        }
                        else
                            row[columnName] = "-";

                        //대장균군
                        columnName = $"{riverName}_대장균군";
                        data = [.. SearchData.Where(m => m.SurveyYear == year && m.River == river && m.EColi != null).Select(m => m.EColi)];
                        if (data.Count != 0)
                        {
                            var scoreSum = data.Sum(m => m) ?? 0;
                            var score = Math.Round((double)scoreSum / data.Count, 1, MidpointRounding.AwayFromZero);
                            grade = GB.GetEColiGrade(score);
                            row[columnName] = grade;
                        }
                        else
                            row[columnName] = "-";

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
