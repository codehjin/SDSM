using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SDSM_App.Enums;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.Models.Repositories.Interfases;
using SDSM_Models;

namespace SDSM_App.ViewModels;

public partial class AmphibianReptileEditorControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public AmphibianReptileEditorControlViewModel()
    {
        //수정모델 받기
        WeakReferenceMessenger.Default.Register<ModelMessage<AmphibianReptile>>(this, (r, m) =>
        {
            ModelRecevice(m);
        });
    }

    #endregion

    #region 속성

    [ObservableProperty] private string _formTitle = "신규";
    [ObservableProperty] private EditStatusType _editStatus = EditStatusType.Add;

    [ObservableProperty] private AmphibianReptile _editModel = new();

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _yearChsu = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private DateTime? _surveyDate;
    [ObservableProperty] private string? _weather;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _river = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _site = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _speciesKo = string.Empty;
    [ObservableProperty] private string? _speciesEn;
    [ObservableProperty] private string? _feature;
    [ObservableProperty] private int? _trace1;
    [ObservableProperty] private int? _trace2;
    [ObservableProperty] private int? _trace3;
    [ObservableProperty] private int? _trace4;
    [ObservableProperty] private int? _trace5;
    [ObservableProperty] private int? _trace6;
    [ObservableProperty] private double? _lat;
    [ObservableProperty] private double? _lng;
    [ObservableProperty] private string? _majorCategory;
    [ObservableProperty] private string? _middleCategory;
    [ObservableProperty] private string? _surveyor;
    [ObservableProperty] private string? _note;
    [ObservableProperty] private int? _siteDivisionId;

    #endregion

    #region ICommand 구현

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(YearChsu)
            && SurveyDate != null
            && !string.IsNullOrWhiteSpace(River)
            && !string.IsNullOrWhiteSpace(Site)
            && !string.IsNullOrWhiteSpace(SpeciesKo);
    }

    //수정
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void SaveExecuted()
    {
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
            var repository = App.Current.Services.GetRequiredService<IAmphibianReptileRepository>();
            var addModel = await repository.InsertAsync(model);
            if (addModel != null)
            {
                var toast = (GB.SaveMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
                ClearProperty();

                WeakReferenceMessenger.Default.Send(new EditorMessage<AmphibianReptile>(addModel, EditStatusType.Add));
            }
        }
    }

    //수정저장
    private async void EditSave()
    {
        if (IsEditItemToModel())
        {
            var repository = App.Current.Services.GetRequiredService<IAmphibianReptileRepository>();
            var isEdit = await repository.UpdateAsync(EditModel);
            if (isEdit)
            {
                var toast = (GB.EditMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

                WeakReferenceMessenger.Default.Send(new EditorMessage<AmphibianReptile>(EditModel, EditStatusType.Edit));
            }
        }
    }

    //신규 항목값
    private AmphibianReptile? AddItemToModel()
    {
        try
        {
            var model = new AmphibianReptile
            {
                YearChsu = YearChsu,
                SurveyDate = SurveyDate,
                Weather = Weather,
                River = River,
                Site = Site,                    
                SpeciesKo = SpeciesKo,
                SpeciesEn = SpeciesEn,
                Feature = Feature,
                Trace1 = Trace1,
                Trace2 = Trace2,
                Trace3 = Trace3,
                Trace4 = Trace4,
                Trace5 = Trace5,
                Trace6 = Trace6,
                Lat = Lat,
                Lng = Lng,
                MajorCategory = MajorCategory,
                MiddleCategory = MiddleCategory,
                Surveyor = Surveyor,
                Note = Note,
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
            EditModel.YearChsu = YearChsu;
            EditModel.SurveyDate = SurveyDate;
            EditModel.Weather = Weather;
            EditModel.River = River;
            EditModel.Site = Site;
            EditModel.SpeciesKo = SpeciesKo;
            EditModel.SpeciesEn = SpeciesEn;
            EditModel.Feature = Feature;
            EditModel.Trace1 = Trace1;
            EditModel.Trace2 = Trace2;
            EditModel.Trace3 = Trace3;
            EditModel.Trace4 = Trace4;
            EditModel.Trace5 = Trace5;
            EditModel.Trace6 = Trace6;
            EditModel.Lat = Lat;
            EditModel.Lng = Lng;
            EditModel.MajorCategory = MajorCategory;
            EditModel.MiddleCategory = MiddleCategory;
            EditModel.Surveyor = Surveyor;
            EditModel.Note = Note;
            EditModel.SiteDivisionId = SiteDivisionId;

            return true;
        }
        catch { return false; }
    }

    //신규, 수정 세팅
    private void SettingProperties(AmphibianReptile model)
    {
        if (model.AmphibianReptileId > 0)
        {
            EditModelToProperty(model);
        }
        else
        {
            ClearProperty();
        }

    }
    //목록에서 선택한 항목값
    private void EditModelToProperty(AmphibianReptile model)
    {
        EditModel = model;

        FormTitle = "수정";
        EditStatus = EditStatusType.Edit;

        YearChsu = EditModel.YearChsu;
        SurveyDate = EditModel.SurveyDate;
        Weather = EditModel.Weather;
        River = EditModel.River;
        Site = EditModel.Site;
        SpeciesKo = EditModel.SpeciesKo;
        SpeciesEn = EditModel.SpeciesEn;
        Feature = EditModel.Feature;
        Trace1 = EditModel.Trace1;
        Trace2 = EditModel.Trace2;
        Trace3 = EditModel.Trace3;
        Trace4 = EditModel.Trace4;
        Trace5 = EditModel.Trace5;
        Trace6 = EditModel.Trace6;
        Lat = EditModel.Lat;
        Lng = EditModel.Lng;
        MajorCategory = EditModel.MajorCategory;
        MiddleCategory = EditModel.MiddleCategory;
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
        River = string.Empty;
        Site = string.Empty;
        SpeciesKo = string.Empty;
        SpeciesEn = null;
        Feature = null;
        Trace1 = null;
        Trace2 = null;
        Trace3 = null;
        Trace4 = null;
        Trace5 = null;
        Trace6 = null;
        Lat = null;
        Lng = null;
        MajorCategory = null;
        MiddleCategory = null;
        Surveyor = null;
        Note = null;
        SiteDivisionId = null;
    }

    #endregion 

    #region Messages

    private void ModelRecevice(ModelMessage<AmphibianReptile> message)
    {
        SettingProperties(message.Model);
    }

    #endregion
}
