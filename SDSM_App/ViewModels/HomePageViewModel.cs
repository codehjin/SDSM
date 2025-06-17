using CommunityToolkit.Mvvm.ComponentModel;
using SDSM_App.InjectableServices;

namespace SDSM_App.ViewModels;

/// <summary>
/// 홈
/// </summary>
public partial class HomePageViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public HomePageViewModel()
    {
        InitialLoad();
    }

    #endregion

    #region 속성

   

    #endregion

    #region ICommand 구현

    

    #endregion

    #region 매서드

    private void InitialLoad()
    {
        
    }

    #endregion 
}
