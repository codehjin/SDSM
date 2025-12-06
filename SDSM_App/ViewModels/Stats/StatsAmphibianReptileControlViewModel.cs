using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SDSM_App.Animations;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using SDSM_Models;
using System.Data;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace SDSM_App.ViewModels;

public partial class StatsAmphibianReptileControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public StatsAmphibianReptileControlViewModel()
    {
        InitialLoad();
    }

    #endregion

    #region 속성

    [ObservableProperty] private string _searchCondition = "전체";

    public List<AmphibianReptile> Data { get; set; } = [];
    public List<AmphibianReptile> SearchData { get; set; } = [];

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
    [ObservableProperty] private RadGridView _projectRadGridView = new();

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

        Data = await context.AmphibianReptiles.Where(m => m.SiteDivision.Project == "방류하천").ToListAsync();

        var years = await GetSearchData(Data);

        SearchCollection.AddRange(years);
        SearchSelectedItems.AddRange(years);

        await GetData();

        await GetModels();

        if (GB.IsBusy == true)
            GB.IsBusy = false;
    }

    //검색목록
    private async Task<List<int>> GetSearchData(List<AmphibianReptile> data)
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
                            .Select(m => new AmphibianReptile
                            {
                                SiteDivision = m.SiteDivision,
                                SurveyYear = m.SurveyYear,
                                SpeciesKo = m.SpeciesKo,
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

        var tasks = new[] { GetYearData(), GetProjectData() };

        try
        {
            await Task.WhenAll(tasks);

            await SetProjectColumns();

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
                // 연도별 자료(레코드) 수
                var recordCounts = SearchData.GroupBy(m => m.SurveyYear)
                                               .Select(g => new { Year = g.Key, Records = g.Count() })
                                               .ToDictionary(x => x.Year, x => x.Records);

                // 연도별 종수(중복 제거한 SpeciesKo 개수)
                var speciesCounts = SearchData.GroupBy(m => m.SurveyYear)
                                               .Select(g => new
                                               {
                                                   Year = g.Key,
                                                   SpeciesCount = g.Select(s => s.SpeciesKo).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().Count()
                                               })
                                               .ToDictionary(x => x.Year, x => x.SpeciesCount);

                // 모든 연도(레코드 또는 종 데이터에 등장하는 연도들의 합집합)를 오름차순으로 처리
                var allYears = recordCounts.Keys.Union(speciesCounts.Keys).OrderBy(y => y).ToList();

                foreach (var year in allYears)
                {
                    var recCount = recordCounts.TryGetValue(year, out var r) ? r : 0;
                    var spCount = speciesCounts.TryGetValue(year, out var s) ? s : 0;

                    models.Add(new PlotItem
                    {
                        XCat = year,
                        YVal1 = recCount,   // 자료(레코드) 수
                        YRatio1 = spCount   // 연도별 종수
                    });
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
                ProjectTotalWorkplace = SearchData.Select(m => m.SiteDivision.Workplace).Distinct().Count();
                ProjectTotalWorkplaceRiver = SearchData.Select(m => m.SiteDivision.WorkplaceRiver).Distinct().Count();

                //출현종
                speciesModels.AddRange(SearchData.Select(m => m.SpeciesKo).Distinct().OrderBy(m => m));
                ProjectTotalSpecies = speciesModels.Count;

                var years = SearchData.Select(m => m.SurveyYear).Distinct().OrderBy(m => m).ToList();
                var workplaces = SearchData.Select(m => m.SiteDivision.Workplace).Distinct().OrderBy(m => m).ToList();

                dt.Columns.Add(new DataColumn("사업장", typeof(string)));
                dt.Columns.Add(new DataColumn("과업대표하천", typeof(string)));

                //전체
                dt.Columns.Add(new DataColumn($"전체_종수", typeof(string)));
                dt.Columns.Add(new DataColumn($"전체_출현종", typeof(string)));

                foreach (var year in years)
                {
                    dt.Columns.Add(new DataColumn($"{year}년_종수", typeof(string)));
                    dt.Columns.Add(new DataColumn($"{year}년_출현종", typeof(string)));
                }

                foreach (var workplace in workplaces)
                {
                    var workplaceRivers = SearchData.Where(m => m.SiteDivision.Workplace == workplace).Select(m => m.SiteDivision.WorkplaceRiver).Distinct().OrderBy(m => m).ToList();

                    foreach (var workplaceRiver in workplaceRivers)
                    {
                        var row = dt.NewRow();

                        var allData = SearchData.Where(m => m.SiteDivision.Workplace == workplace && m.SiteDivision.WorkplaceRiver == workplaceRiver).ToList();
                        //출현종
                        var allSpeciesList = allData.Select(m => m.SpeciesKo).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().OrderBy(s => s).ToList();
                        // 콤마로 연결한 문자열로 변환
                        var allSpeciesCsv = allSpeciesList.Count > 0 ? string.Join(", ", allSpeciesList) : string.Empty;

                        row["사업장"] = workplace;
                        row["과업대표하천"] = workplaceRiver;
                        row["전체_종수"] = $"{allData.Select(m => m.SpeciesKo).Distinct().Count()}종";
                        row["전체_출현종"] = allSpeciesCsv;

                        foreach (var year in years)
                        {
                            var data = allData.Where(m => m.SurveyYear == year).ToList();

                            //출현종
                            var speciesList = data.Select(m => m.SpeciesKo).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
                            // 콤마로 연결한 문자열로 변환
                            var speciesCsv = speciesList.Count > 0 ? string.Join(", ", speciesList) : string.Empty;

                            var yearName = $"{year}년_종수";
                            var yearSpecies = $"{year}년_출현종";

                            row["사업장"] = workplace;
                            row["과업대표하천"] = workplaceRiver;
                            row[yearName] = $"{data.Select(m => m.SpeciesKo).Distinct().Count()}종";
                            row[yearSpecies] = speciesCsv;
                        }

                        dt.Rows.Add(row);
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

    //사업장 과업대표하천 컬럼그룹 동적생성
    private async Task SetProjectColumns()
    {
        var dt = ProjectDataTable;
        if (dt == null || dt.Rows.Count == 0)
            return;

        var headers = new List<string>();

        await Task.Run(() =>
        {
            foreach (DataColumn column in dt.Columns)
            {
                var header = column.ColumnName ?? "";
                if (header.Contains('_'))
                {
                    // "_" 다음 문자열만 남김
                    headers.Add(header[..(header.IndexOf('_'))]);
                }
            }
        });

        ProjectRadGridView.ColumnGroups.SuspendNotifications();

        ProjectRadGridView.ColumnGroups.Clear();

        foreach (var header in headers.Distinct())
        {
            ProjectRadGridView.ColumnGroups.Add(new GridViewColumnGroup { Name = header, Header = header });
        }

        ProjectRadGridView.ColumnGroups.ResumeNotifications();
    }

    #endregion 
}
