using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SDSM_App.Enums;
using SDSM_App.Helpers;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.Models.Repositories.Interfases;
using SDSM_Models;
using System.Windows;

namespace SDSM_App.ViewModels;

public partial class VegetationEditorControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public VegetationEditorControlViewModel()
    {
        //수정모델 받기
        WeakReferenceMessenger.Default.Register<ModelMessage<Vegetation>>(this, (r, m) =>
        {
            ModelRecevice(m);
        });
    }

    #endregion

    #region 속성

    [ObservableProperty] private string _formTitle = "신규";
    [ObservableProperty] private EditStatusType _editStatus = EditStatusType.Add;

    [ObservableProperty] private Vegetation _editModel = new();
    [ObservableProperty] private string _project = "방류하천";

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _yearChsu = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _site = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private DateTime? _surveyDate;   
    [ObservableProperty] private string? _majorRegion;
    [ObservableProperty] private string? _middleRegion;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _river = string.Empty;
    [ObservableProperty] private string? _surveyor;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _colony = string.Empty;
    [ObservableProperty] private double? _area;
    [ObservableProperty] private int? _siteDivisionId;

    #endregion

    #region ICommand 구현

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(YearChsu)
            && SurveyDate != null
            && !string.IsNullOrWhiteSpace(Site)
            && !string.IsNullOrWhiteSpace(River)
            && !string.IsNullOrWhiteSpace(Colony);
    }

    //저장
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void SaveExecuted()
    {
        int? siteDivisionId = SiteDivisionHelper.FindSiteDivisionIdBySiteContains(Project == "생태현황" ? true : Project == "방류하천" ? false : null, Site);
        if (siteDivisionId != null)
        {
            SiteDivisionId = siteDivisionId;
        }
        else
        {
            MessageBox.Show($"지점명 : {Site}\n\n프로젝트 지점명 목록에 포함되어 있지 않습니다.", "지점명 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (EditStatus == EditStatusType.Add)
        {
            AddSave();
        }
        else
        {
            EditSave();
        }
    }

    #endregion

    #region 매서드

    //신규저장
    private async void AddSave()
    {
        var model = AddItemToModel();
        if (model != null)
        {
            var repository = App.Current.Services.GetRequiredService<IVegetationRepository>();
            var addModel = await repository.InsertAsync(model);
            if (addModel != null)
            {
                var toast = (GB.SaveMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
                ClearProperty();

                WeakReferenceMessenger.Default.Send(new EditorMessage<Vegetation>(addModel, EditStatusType.Add));
            }
        }
    }

    //수정저장
    private async void EditSave()
    {
        if (IsEditItemToModel())
        {
            var repository = App.Current.Services.GetRequiredService<IVegetationRepository>();
            var isEdit = await repository.UpdateAsync(EditModel);
            if (isEdit)
            {
                var toast = (GB.EditMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

                WeakReferenceMessenger.Default.Send(new EditorMessage<Vegetation>(EditModel, EditStatusType.Edit));
            }
        }
    }

    //신규 항목값
    private Vegetation? AddItemToModel()
    {
        try
        {
            var model = new Vegetation
            {
                YearChsu = YearChsu,
                SurveyDate = SurveyDate,
                MajorRegion = MajorRegion,
                MiddleRegion = MiddleRegion,
                River = River,
                Site = Site,
                Surveyor = Surveyor,
                Colony = Colony,
                Area = Area,
                SiteDivisionId = SiteDivisionId
            };
            return model;

        }
        catch { return null; }
    }

    //수정 항목값
    private bool IsEditItemToModel()
    {
        try
        {
            EditModel.YearChsu = YearChsu;
            EditModel.SurveyDate = SurveyDate;
            EditModel.MajorRegion = MajorRegion;
            EditModel.MiddleRegion = MiddleRegion;
            EditModel.River = River;
            EditModel.Site = Site;
            EditModel.Surveyor = Surveyor;
            EditModel.Colony = Colony;
            EditModel.Area = Area;
            EditModel.SiteDivisionId = SiteDivisionId;

            return true;
        }
        catch { return false; }
    }

    //신규, 수정 세팅
    private void SettingProperties(Vegetation model)
    {
        if (model.VegetationId > 0)
        {
            EditModelToProperty(model);
        }
        else
        {
            ClearProperty();
        }

    }
    //목록에서 선택한 항목값
    private void EditModelToProperty(Vegetation model)
    {
        EditModel = model;

        FormTitle = "수정";
        EditStatus = EditStatusType.Edit;
        Project = EditModel.SiteDivision.Project;

        YearChsu = model.YearChsu;
        SurveyDate = model.SurveyDate;
        MajorRegion = model.MajorRegion;
        MiddleRegion = model.MiddleRegion;
        River = model.River;
        Site = model.Site;
        Surveyor = model.Surveyor;
        Colony = model.Colony;
        Area = model.Area;
        SiteDivisionId = model.SiteDivisionId;
    }

    //항목초기화
    private void ClearProperty()
    {
        FormTitle = "신규";
        EditStatus = EditStatusType.Add;
        EditModel = new();

        YearChsu = string.Empty;
        SurveyDate = null;
        MajorRegion = null;
        MiddleRegion = null;
        River = string.Empty;
        Site = string.Empty;
        Surveyor = null;
        Colony = string.Empty;
        Area = null;
        SiteDivisionId = null;
    }

    #endregion 

    #region Messages

    private void ModelRecevice(ModelMessage<Vegetation> message)
    {
        SettingProperties(message.Model);
    }

    #endregion
}
