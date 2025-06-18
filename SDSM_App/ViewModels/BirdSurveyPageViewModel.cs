using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NinjaNye.SearchExtensions;
using SDSM_App.Enums;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using SDSM_Models;
using System.Drawing.Printing;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace SDSM_App.ViewModels;

/// <summary>
/// 조류 조사
/// </summary>
public partial class BirdSurveyPageViewModel : ObservableObject, ITransientService
{
    #region 필드

    private CancellationTokenSource? _cts;

    #endregion

    #region 생성자

    public BirdSurveyPageViewModel()
    {
        InitialLoad();
    }

    #endregion

    #region 속성

    public string QueryText { get; set; } = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;

    public SearchModeType SearchModeSelectedValue { get; set; } = SearchModeType.포함;

    //검색모드
    public EnumDataSource SearchModeSource
    {
        get { return new EnumDataSource { EnumType = typeof(SearchModeType) }; }
    }

    [ObservableProperty] List<BirdSurvey> _modelCollection = [];
    [ObservableProperty] BirdSurvey? _modelSelectedItem;

    #endregion

    #region 페이징

    [ObservableProperty] private int _modelTotal;
    [ObservableProperty] private int _pageIndex;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PageSizeMinExecutedCommand))]
    [NotifyCanExecuteChangedFor(nameof(PageSizeAvgExecutedCommand))]
    [NotifyCanExecuteChangedFor(nameof(PageSizeMaxExecutedCommand))]
    private int _pageSize = GB.CurrentPageSize;

    [RelayCommand]
    private void PageIndexChangedExecuted(object? obj)
    {
        if (_cts == null && obj != null && obj is PageIndexChangedEventArgs e)
        {
            if (GB.IsBusy == false)
                GB.IsBusy = true;

            try
            {
                GetModels(e.NewPageIndex);

                if (GB.IsBusy == true)
                    GB.IsBusy = false;
            }
            catch
            {
                if (GB.IsBusy == true)
                    GB.IsBusy = false;
            }
        }
    }

    [RelayCommand]
    private void PageSizeChangedExecuted(object? obj)
    {
        if (_cts == null && obj != null && obj is RadRangeBaseValueChangedEventArgs e)
        {
            if (int.TryParse(e.NewValue.ToString(), out int pageSize))
            {
                GB.CurrentPageSize = pageSize;

                if (GB.IsBusy == false)
                    GB.IsBusy = true;

                try
                {
                    GetModels();

                    if (GB.IsBusy == true)
                        GB.IsBusy = false;
                }
                catch
                {
                    if (GB.IsBusy == true)
                        GB.IsBusy = false;
                }
            }
        }
    }

    private bool CanPageSizeMin()
    {
        return PageSize > GB.MinPageSize;
    }

    [RelayCommand(CanExecute = nameof(CanPageSizeMin))]
    private void PageSizeMinExecuted()
    {
        GB.CurrentPageSize = GB.MinPageSize;
        PageSize = GB.MinPageSize;
    }

    private bool CanPageSizeAvg()
    {
        return PageSize != GB.AvgPageSize;
    }

    [RelayCommand(CanExecute = nameof(CanPageSizeAvg))]
    private void PageSizeAvgExecuted()
    {
        GB.CurrentPageSize = GB.AvgPageSize;
        PageSize = GB.AvgPageSize;
    }

    private bool CanPageSizeMax()
    {
        return PageSize < GB.MaxPageSize;
    }

    [RelayCommand(CanExecute = nameof(CanPageSizeMax))]
    private void PageSizeMaxExecuted()
    {
        GB.CurrentPageSize = GB.MaxPageSize;
        PageSize = GB.MaxPageSize;
    }

    #endregion

    #region ICommand 구현

    //검색
    [RelayCommand]
    private void SearchExecuted(object? obj)
    {
        QueryText = SearchText;
        GetModels();
    }

    #endregion

    #region 매서드

    private void InitialLoad()
    {
        GetModels();
    }

    private async void GetModels(int index = 0)
    {
        if (GB.IsBusy == false)
            GB.IsBusy = true;

        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            PageIndex = index;

            using var context = new SDSMDbContext();
            IQueryable<BirdSurvey> query;

            await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(QueryText))
                {
                    query = context.BirdSurveys;
                }
                else
                {
                    QueryableStringSearch<BirdSurvey> searchQuery =
                        context.BirdSurveys.Search(m => m.SurveyYearChsu,
                                                  m => m.Site,
                                                  m => m.SurveyDate.ToString(),
                                                  m => m.River
                                                  );

                    if (SearchModeSelectedValue == SearchModeType.일치)
                    {
                        query = searchQuery.EqualTo(QueryText);
                    }
                    else if (SearchModeSelectedValue == SearchModeType.시작문자)
                    {
                        query = searchQuery.StartsWith(QueryText);
                    }
                    else if (SearchModeSelectedValue == SearchModeType.끝문자)
                    {
                        query = searchQuery.EndsWith(QueryText);
                    }
                    else
                    {
                        query = searchQuery.Containing(QueryText);
                    }
                }

                ModelTotal = query.Count();

                ModelCollection = [.. query.OrderByDescending(m => m.BirdSurveyId).Skip(index * PageSize).Take(PageSize)];

            }, _cts.Token);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetModels), ex.Message);
        }
        finally
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            if (GB.IsBusy == true)
                GB.IsBusy = false;
        }
    }

    #endregion
}
