using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using NinjaNye.SearchExtensions;
using SDSM_App.Enums;
using SDSM_App.InjectableServices;
using SDSM_App.Models;
using SDSM_Models;
using System.Collections.ObjectModel;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace SDSM_App.ViewModels;

/// <summary>
/// 일괄등록
/// </summary>
public partial class BulkRegisterPageViewModel : ObservableObject, ITransientService
{
    #region 필드


    #endregion

    #region 생성자

    public BulkRegisterPageViewModel()
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
