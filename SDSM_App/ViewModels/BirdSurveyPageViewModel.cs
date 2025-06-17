using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using SDSM_Models;

namespace SDSM_App.ViewModels;

/// <summary>
/// 조류 조사
/// </summary>
public partial class BirdSurveyPageViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public BirdSurveyPageViewModel()
    {
        InitialLoad();
    }

    #endregion

    #region 속성

    [ObservableProperty] List<BirdSurvey> _modelCollection = [];
    [ObservableProperty] BirdSurvey? _modelSelectedItem;

    #endregion

    #region ICommand 구현


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

            ModelCollection = await context.BirdSurveys.OrderByDescending(m => m.BirdSurveyId).ToListAsync();

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
