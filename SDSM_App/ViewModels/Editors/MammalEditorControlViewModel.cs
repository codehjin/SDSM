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

public partial class MammalEditorControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public MammalEditorControlViewModel()
    {
        //수정모델 받기
        WeakReferenceMessenger.Default.Register<ModelMessage<Mammal>>(this, (r, m) =>
        {
            ModelRecevice(m);
        });
    }

    #endregion

    #region 속성

    [ObservableProperty] private string _formTitle = "신규";
    [ObservableProperty] private EditStatusType _editStatus = EditStatusType.Add;

    [ObservableProperty] private Mammal _editModel = new();
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _project = "방류하천";

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _yearChsu = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private DateTime? _surveyDate;
    [ObservableProperty] private string? _weather;
    [ObservableProperty] private string? _river;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string? _site;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _speciesKo = string.Empty;
    [ObservableProperty] private string? _speciesEn;
    [ObservableProperty] private string? _feature;
    [ObservableProperty] private double? _lat;
    [ObservableProperty] private double? _lng;
    [ObservableProperty] private string? _observationSite;
    [ObservableProperty] private int? _trace1;
    [ObservableProperty] private int? _trace2;
    [ObservableProperty] private int? _trace3;
    [ObservableProperty] private int? _trace4;
    [ObservableProperty] private int? _trace5;
    [ObservableProperty] private int? _trace6;
    [ObservableProperty] private int? _trace7;
    [ObservableProperty] private int? _trace8;
    [ObservableProperty] private int? _trace9;
    [ObservableProperty] private int? _trace10;
    [ObservableProperty] private int? _trace11;
    [ObservableProperty] private int? _trace12;
    [ObservableProperty] private string? _surveyor;
    [ObservableProperty] private string? _note;
    [ObservableProperty] private int? _siteDivisionId;

    #endregion

    #region ICommand 구현

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(YearChsu)
            && SurveyDate != null
            && !string.IsNullOrWhiteSpace(SpeciesKo)
            && (Project != "방류하천" || !string.IsNullOrWhiteSpace(Site));
    }

    //저장
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void SaveExecuted()
    {
        int? siteDivisionId = SiteDivisionHelper.FindSiteDivisionId(Project == "생태현황" ? true : Project == "방류하천" ? false : null, Site, YearChsu);
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
            var repository = App.Current.Services.GetRequiredService<IMammalRepository>();
            var addModel = await repository.InsertAsync(model);
            if (addModel != null)
            {
                var toast = (GB.SaveMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
                ClearProperty();

                WeakReferenceMessenger.Default.Send(new EditorMessage<Mammal>(addModel, EditStatusType.Add));
            }
        }
    }

    //수정저장
    private async void EditSave()
    {
        if (IsEditItemToModel())
        {
            var repository = App.Current.Services.GetRequiredService<IMammalRepository>();
            var isEdit = await repository.UpdateAsync(EditModel);
            if (isEdit)
            {
                var toast = (GB.EditMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

                WeakReferenceMessenger.Default.Send(new EditorMessage<Mammal>(EditModel, EditStatusType.Edit));
            }
        }
    }

    //신규 항목값
    private Mammal? AddItemToModel()
    {
        try
        {
            var model = new Mammal
            {
                YearChsu = YearChsu,
                SurveyDate = SurveyDate,
                Weather = Weather,
                River = River,
                Site = Site,                    
                SpeciesKo = SpeciesKo,
                SpeciesEn = SpeciesEn,
                Feature = Feature,
                Lat = Lat,
                Lng = Lng,
                ObservationSite = ObservationSite,
                Trace1 = Trace1,
                Trace2 = Trace2,
                Trace3 = Trace3,
                Trace4 = Trace4,
                Trace5 = Trace5,
                Trace6 = Trace6,
                Trace7 = Trace7,
                Trace8 = Trace8,
                Trace9 = Trace9,
                Trace10 = Trace10,
                Trace11 = Trace11,
                Trace12 = Trace12,
                Surveyor = Surveyor,
                Note = Note,
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
            EditModel.Weather = Weather;
            EditModel.River = River;
            EditModel.Site = Site;
            EditModel.SpeciesKo = SpeciesKo;
            EditModel.SpeciesEn = SpeciesEn;
            EditModel.Feature = Feature;
            EditModel.Lat = Lat;
            EditModel.Lng = Lng;
            EditModel.ObservationSite = ObservationSite;
            EditModel.Trace1 = Trace1;
            EditModel.Trace2 = Trace2;
            EditModel.Trace3 = Trace3;
            EditModel.Trace4 = Trace4;
            EditModel.Trace5 = Trace5;
            EditModel.Trace6 = Trace6;
            EditModel.Trace7 = Trace7;
            EditModel.Trace8 = Trace8;
            EditModel.Trace9 = Trace9;
            EditModel.Trace10 = Trace10;
            EditModel.Trace11 = Trace11;
            EditModel.Trace12 = Trace12;
            EditModel.Surveyor = Surveyor;
            EditModel.Note = Note;
            EditModel.SiteDivisionId = SiteDivisionId;

            return true;
        }
        catch { return false; }
    }

    //신규, 수정 세팅
    private void SettingProperties(Mammal model)
    {
        if (model.MammalId > 0)
        {
            EditModelToProperty(model);
        }
        else
        {
            ClearProperty();
        }

    }
    //목록에서 선택한 항목값
    private void EditModelToProperty(Mammal model)
    {
        EditModel = model;

        FormTitle = "수정";
        EditStatus = EditStatusType.Edit;
        Project = EditModel.SiteDivision.Project;

        YearChsu = EditModel.YearChsu;
        SurveyDate = EditModel.SurveyDate;
        Weather = EditModel.Weather;
        River = EditModel.River;
        Site = EditModel.Site;
        SpeciesKo = EditModel.SpeciesKo;
        SpeciesEn = EditModel.SpeciesEn;
        Feature = EditModel.Feature;
        Lat = EditModel.Lat;
        Lng = EditModel.Lng;
        ObservationSite = EditModel.ObservationSite;
        Trace1 = EditModel.Trace1;
        Trace2 = EditModel.Trace2;
        Trace3 = EditModel.Trace3;
        Trace4 = EditModel.Trace4;
        Trace5 = EditModel.Trace5;
        Trace6 = EditModel.Trace6;
        Trace7 = EditModel.Trace7;
        Trace8 = EditModel.Trace8;
        Trace9 = EditModel.Trace9;
        Trace10 = EditModel.Trace10;
        Trace11 = EditModel.Trace11;
        Trace12 = EditModel.Trace12;
        Surveyor = EditModel.Surveyor;
        Note = EditModel.Note;
        SiteDivisionId = EditModel.SiteDivisionId;
    }

    //항목초기화
    private void ClearProperty()
    {
        FormTitle = "신규";
        EditStatus = EditStatusType.Add;
        EditModel = new();

        YearChsu = string.Empty;
        SurveyDate = null;
        Weather = null;
        River = null;
        Site = null;
        SpeciesKo = string.Empty;
        SpeciesEn = null;
        Feature = null;
        Lat = null;
        Lng = null;
        ObservationSite = null;
        Trace1 = null;
        Trace2 = null;
        Trace3 = null;
        Trace4 = null;
        Trace5 = null;
        Trace6 = null;
        Trace7 = null;
        Trace8 = null;
        Trace9 = null;
        Trace10 = null;
        Trace11 = null;
        Trace12 = null;
        Surveyor = null;
        Note = null;
        SiteDivisionId = null;
    }

    #endregion 

    #region Messages

    private void ModelRecevice(ModelMessage<Mammal> message)
    {
        SettingProperties(message.Model);
    }

    #endregion
}
