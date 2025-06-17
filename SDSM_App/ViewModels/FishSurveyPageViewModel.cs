using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using SDSM_Models;
using System.Windows;

namespace SDSM_App.ViewModels;

/// <summary>
/// 어류조사
/// </summary>
public partial class FishSurveyPageViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public FishSurveyPageViewModel()
    {
        InitialLoad();
    }

    #endregion

    #region 속성

    [ObservableProperty] List<FishSurveyView> _modelCollection = [];
    [ObservableProperty] FishSurveyView? _modelSelectedItem;

    [ObservableProperty] private FishSurvey _subData = new();

    private Visibility _subViewVisibility = Visibility.Collapsed;
    public Visibility SubViewVisibility
    {
        get { return _subViewVisibility; }
        set
        {
            SetProperty(ref _subViewVisibility, value);

            if (value == Visibility.Visible)
            {
                GridViewSelectionChangedExecutedCommand.Execute(null);
            }
        }
    }

    #endregion

    #region ICommand 구현

    [RelayCommand]
    private void GridViewSelectionChangedExecuted()
    {
        if (SubViewVisibility == Visibility.Visible && ModelSelectedItem != null)
        {
            using var context = new SDSMDbContext();

            var subData = context.FishSurveys.Where(m => m.FishSurveyId == ModelSelectedItem.FishSurveyId)
                                               .Include(m => m.Subs)
                                               .FirstOrDefault();

            SubData = subData ?? new();
        }
        else
        {
            if (SubData.FishSurveyId > 0)
                SubData = new();
        }
    }

    private bool CanSubViewOpen()
    {
        return ModelSelectedItem != null;
    }

    //종목록 열기
    [RelayCommand(CanExecute = nameof(CanSubViewOpen))]
    private void SubViewOpenExecuted()
    {
        if (ModelSelectedItem != null)
        {
            SubViewVisibility = Visibility.Visible;
        }
    }

    //종목록 닫기
    [RelayCommand]
    private void SubViewCloseExecuted()
    {
        SubViewVisibility = Visibility.Collapsed;
    }

    #endregion

    #region 매서드

    private async void InitialLoad()
    {
        await GetModels();
    }

    private async Task GetModels()
    {
        if (GB.IsBusy == false)
            GB.IsBusy = true;

        try
        {
            using var context = new SDSMDbContext();

            ModelCollection = await context.FishSurveyViews.OrderByDescending(m => m.FishSurveyId).ToListAsync();

        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetModels), ex.Message);
        }
        finally
        {
            if (GB.IsBusy == true)
                GB.IsBusy = false;
        }
    }

    #endregion
}
