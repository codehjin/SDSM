using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using SDSM_Models;

namespace SDSM_App.ViewModels;

/// <summary>
/// 어류조사
/// </summary>
public partial class ARSurveyPageViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public ARSurveyPageViewModel()
    {
        InitialLoad();
    }

    #endregion

    #region 속성

    [ObservableProperty] List<ARSurvey> _modelCollection = [];
    [ObservableProperty] ARSurvey? _modelSelectedItem;

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

            ModelCollection = await context.ARSurveys.OrderByDescending(m => m.ARSurveyId).ToListAsync();

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
