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
    [ObservableProperty] private RadObservableCollection<PlotItem> _gradeCollection = [new()];

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
                                Site = m.Site,
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
            GradeCollection.Clear();

            var models = new List<PlotItem>();

            await Task.Run(() =>
            {
                string[] osans = ["오산천3", "오산천03"];
                string[] woncheons = ["원천리천2", "원천리천02"];

                var years = SearchData.Select(m => m.SurveyYear).Distinct().OrderBy(m => m).ToList();
                var osanData = SearchData.Where(m => !string.IsNullOrEmpty(m.Site) && osans.Contains(m.Site)).ToList();
                var woncheonData = SearchData.Where(m => !string.IsNullOrEmpty(m.Site) && woncheons.Contains(m.Site)).ToList();

                // 년도별 그룹으로 만들어서 반복 시 Where 호출을 제거
                var osanByYear = osanData.GroupBy(m => m.SurveyYear).ToDictionary(g => g.Key, g => g.ToList());
                var woncheonByYear = woncheonData.GroupBy(m => m.SurveyYear).ToDictionary(g => g.Key, g => g.ToList());

                // 헬퍼: 리스트에서 선택한 필드의 평균과 등급을 계산하여 포맷된 문자열 반환
                static string FormatGrade(List<WaterQuality>? list, Func<WaterQuality, double?> selector, Func<double?, object?> gradeFunc, int decimals = 1)
                {
                    if (list == null || list.Count == 0)
                        return "-";

                    // 유효한 값만 추출
                    var vals = list.Select(selector).Where(v => v.HasValue).Select(v => v!.Value).ToArray();
                    if (vals.Length == 0)
                        return "-";

                    double sum = vals.Sum();
                    if (sum == 0)
                        return "-";

                    double avg = Math.Round(sum / vals.Length, decimals, MidpointRounding.AwayFromZero);
                    var grade = gradeFunc(avg);
                    var gradeStr = grade?.ToString() ?? "-";

                    return $"{gradeStr}({avg})";
                }

                foreach (var year in years)
                {
                    osanByYear.TryGetValue(year, out var osanYearData);
                    woncheonByYear.TryGetValue(year, out var woncheonYearData);

                    var model = new PlotItem
                    {
                        XCat = year,
                        
                        YString1 = FormatGrade(osanYearData, w => w.PH, v => GB.GetPhGrade(v)),
                        YString2 = FormatGrade(osanYearData, w => w.BOD, v => GB.GetBodGrade(v)),
                        YString3 = FormatGrade(osanYearData, w => w.COD, v => GB.GetCodGrade(v)),
                        YString4 = FormatGrade(osanYearData, w => w.TOC, v => GB.GetTocGrade(v)),
                        YString5 = FormatGrade(osanYearData, w => w.SS, v => GB.GetSsGrade(v)),
                        YString6 = FormatGrade(osanYearData, w => w.DO, v => GB.GetDoGrade(v)),
                        YString7 = FormatGrade(osanYearData, w => w.TP, v => GB.GetTpGrade(v)),
                        YString8 = FormatGrade(osanYearData, w => w.EColi, v => GB.GetEColiGrade(v)),

                        YString9 = FormatGrade(woncheonYearData, w => w.PH, v => GB.GetPhGrade(v)),
                        YString10 = FormatGrade(woncheonYearData, w => w.BOD, v => GB.GetBodGrade(v)),
                        YString11 = FormatGrade(woncheonYearData, w => w.COD, v => GB.GetCodGrade(v)),
                        YString12 = FormatGrade(woncheonYearData, w => w.TOC, v => GB.GetTocGrade(v)),
                        YString13 = FormatGrade(woncheonYearData, w => w.SS, v => GB.GetSsGrade(v)),
                        YString14 = FormatGrade(woncheonYearData, w => w.DO, v => GB.GetDoGrade(v)),
                        YString15 = FormatGrade(woncheonYearData, w => w.TP, v => GB.GetTpGrade(v)),
                        YString16 = FormatGrade(woncheonYearData, w => w.EColi, v => GB.GetEColiGrade(v)),
                    };

                    models.Add(model);
                }
            });

            GradeCollection.AddRange(models);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetGradeData), ex.Message);
        }
    }

    #endregion 
}
