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

public partial class PlantEditorControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public PlantEditorControlViewModel()
    {
        //수정모델 받기
        WeakReferenceMessenger.Default.Register<ModelMessage<Plant>>(this, (r, m) =>
        {
            ModelRecevice(m);
        });
    }

    #endregion

    #region 속성

    [ObservableProperty] private string _formTitle = "신규";
    [ObservableProperty] private EditStatusType _editStatus = EditStatusType.Add;

    [ObservableProperty] private Plant _editModel = new();
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _project = "방류하천";

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private int? _surveyYear;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _chsu = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private DateTime? _surveyDate;
    [ObservableProperty] private string? _weather;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string? _site;
    [ObservableProperty] private string? _river;
    [ObservableProperty] private string? _surveyor;
    [ObservableProperty] private double? _lat;
    [ObservableProperty] private double? _lng;
    [ObservableProperty] private string? _surveyLocation;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _speciesKo = string.Empty;
    [ObservableProperty] private string? _speciesEn;
    [ObservableProperty] private string? _alienPlant;
    [ObservableProperty] private string? _invasiveSpecies;
    [ObservableProperty] private int? _siteDivisionId;

    #endregion

    #region ICommand 구현

    private bool CanSave()
    {
        return SurveyYear != null
            && !string.IsNullOrWhiteSpace(Chsu)
            && SurveyDate != null
            && !string.IsNullOrWhiteSpace(SpeciesKo)
            && (Project != "방류하천" || !string.IsNullOrWhiteSpace(Site));
    }

    //저장
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void SaveExecuted()
    {
        int? siteDivisionId = SiteDivisionHelper.FindSiteDivisionId(Project == "생태현황" ? true : Project == "방류하천" ? false : null, Site, SurveyYear?.ToString());
        if (siteDivisionId != null)
        {
            SiteDivisionId = siteDivisionId;
        }
        else
        {
            MessageBox.Show($"지점명 : {Site}\n\n방류하천 지점명 목록에 포함되어 있지 않습니다.", "지점명 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var repository = App.Current.Services.GetRequiredService<IPlantRepository>();
            var addModel = await repository.InsertAsync(model);
            if (addModel != null)
            {
                var toast = (GB.SaveMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
                ClearProperty();

                WeakReferenceMessenger.Default.Send(new EditorMessage<Plant>(addModel, EditStatusType.Add));
            }
        }
    }

    //수정저장
    private async void EditSave()
    {
        if (IsEditItemToModel())
        {
            var repository = App.Current.Services.GetRequiredService<IPlantRepository>();
            var isEdit = await repository.UpdateAsync(EditModel);
            if (isEdit)
            {
                var toast = (GB.EditMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

                WeakReferenceMessenger.Default.Send(new EditorMessage<Plant>(EditModel, EditStatusType.Edit));
            }
        }
    }

    //신규 항목값
    private Plant? AddItemToModel()
    {
        try
        {
            if(SurveyYear is not int year)
                return null;

            var model = new Plant
            {
                SurveyYear = year,
                Chsu = Chsu,
                SurveyDate = SurveyDate,
                Weather = Weather,
                Site = Site,
                River = River,
                Surveyor = Surveyor,
                Lat = Lat,
                Lng = Lng,
                SurveyLocation = SurveyLocation,
                SpeciesKo = SpeciesKo,
                SpeciesEn = SpeciesEn,
                AlienPlant = AlienPlant,
                InvasiveSpecies = InvasiveSpecies,
                SiteDivisionId = SiteDivisionId,
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
            if(SurveyYear is not int year)
                return false;

            EditModel.SurveyYear = year;
            EditModel.Chsu = Chsu;
            EditModel.SurveyDate = SurveyDate;
            EditModel.Weather = Weather;
            EditModel.Site = Site;
            EditModel.River = River;
            EditModel.Surveyor = Surveyor;
            EditModel.Lat = Lat;
            EditModel.Lng = Lng;
            EditModel.SurveyLocation = SurveyLocation;
            EditModel.SpeciesKo = SpeciesKo;
            EditModel.SpeciesEn = SpeciesEn;
            EditModel.AlienPlant = AlienPlant;
            EditModel.InvasiveSpecies = InvasiveSpecies;
            EditModel.SiteDivisionId = SiteDivisionId;

            return true;
        }
        catch { return false; }
    }

    //신규, 수정 세팅
    private void SettingProperties(Plant model)
    {
        if (model.PlantId > 0)
        {
            EditModelToProperty(model);
        }
        else
        {
            ClearProperty();
        }

    }
    //목록에서 선택한 항목값
    private void EditModelToProperty(Plant model)
    {
        EditModel = model;

        FormTitle = "수정";
        EditStatus = EditStatusType.Edit;
        Project = EditModel.SiteDivision.Project;

        SurveyYear = model.SurveyYear;
        Chsu = model.Chsu;
        SurveyDate = model.SurveyDate;
        Weather = model.Weather;
        Site = model.Site;
        River = model.River;
        Surveyor = model.Surveyor;
        Lat = model.Lat;
        Lng = model.Lng;
        SurveyLocation = model.SurveyLocation;
        SpeciesKo = model.SpeciesKo;
        SpeciesEn = model.SpeciesEn;
        AlienPlant = model.AlienPlant;
        InvasiveSpecies = model.InvasiveSpecies;
        SiteDivisionId = model.SiteDivisionId;
    }

    //항목초기화
    private void ClearProperty()
    {
        FormTitle = "신규";
        EditStatus = EditStatusType.Add;
        EditModel = new();

        SurveyYear = null;
        Chsu = string.Empty;
        SurveyDate = null;
        Weather = null;
        Site = null;
        River = null;
        Surveyor = null;
        Lat = null;
        Lng = null;
        SurveyLocation = null;
        SpeciesKo = string.Empty;
        SpeciesEn = null;
        AlienPlant = null;
        InvasiveSpecies = null;
        SiteDivisionId = null;
    }

    #endregion 

    #region Messages

    private void ModelRecevice(ModelMessage<Plant> message)
    {
        SettingProperties(message.Model);
    }

    #endregion
}
