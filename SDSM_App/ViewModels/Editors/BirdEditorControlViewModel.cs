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

public partial class BirdEditorControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public BirdEditorControlViewModel()
    {
        //수정모델 받기
        WeakReferenceMessenger.Default.Register<ModelMessage<Bird>>(this, (r, m) =>
        {
            ModelRecevice(m);
        });
    }

    #endregion

    #region 속성

    [ObservableProperty] private string _formTitle = "신규";
    [ObservableProperty] private EditStatusType _editStatus = EditStatusType.Add;

    [ObservableProperty] private Bird _editModel = new();

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _yearChsu = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private DateTime? _surveyDate;
    [ObservableProperty] private string? _weather;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _river = string.Empty;
    [ObservableProperty] private string? _site;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _speciesKo = string.Empty;
    [ObservableProperty] private string? _speciesEn;
    [ObservableProperty] private string? _feature;
    [ObservableProperty] private double? _lat;
    [ObservableProperty] private double? _lng;
    [ObservableProperty] private int? _individualCount;
    [ObservableProperty] private string? _migratoryType;
    [ObservableProperty] private string? _category;
    [ObservableProperty] private string? _categoryDetail;
    [ObservableProperty] private string? _habitatType;
    [ObservableProperty] private string? _surveyor;
    [ObservableProperty] private string? _note;

    #endregion

    #region ICommand 구현

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(YearChsu)
            && SurveyDate != null
            && !string.IsNullOrWhiteSpace(River)
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
            var repository = App.Current.Services.GetRequiredService<IBirdRepository>();
            var addModel = await repository.InsertAsync(model);
            if (addModel != null)
            {
                var toast = (GB.SaveMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
                ClearProperty();

                WeakReferenceMessenger.Default.Send(new EditorMessage<Bird>(addModel, EditStatusType.Add));
            }
        }
    }

    //수정저장
    private async void EditSave()
    {
        if (IsEditItemToModel())
        {
            var repository = App.Current.Services.GetRequiredService<IBirdRepository>();
            var isEdit = await repository.UpdateAsync(EditModel);
            if (isEdit)
            {
                var toast = (GB.EditMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

                WeakReferenceMessenger.Default.Send(new EditorMessage<Bird>(EditModel, EditStatusType.Edit));
            }
        }
    }

    //신규 항목값
    private Bird? AddItemToModel()
    {
        try
        {
            var model = new Bird
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
                IndividualCount = IndividualCount,
                MigratoryType = MigratoryType,
                Category = Category,
                CategoryDetail = CategoryDetail,
                HabitatType = HabitatType,
                Surveyor = Surveyor,
                Note = Note,
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
            EditModel.IndividualCount = IndividualCount;
            EditModel.MigratoryType = MigratoryType;
            EditModel.Category = Category;
            EditModel.CategoryDetail = CategoryDetail;
            EditModel.HabitatType = HabitatType;
            EditModel.Surveyor = Surveyor;
            EditModel.Note = Note;

            return true;
        }
        catch { return false; }
    }

    //신규, 수정 세팅
    private void SettingProperties(Bird model)
    {
        if (model.BirdId > 0)
        {
            EditModelToProperty(model);
        }
        else
        {
            ClearProperty();
        }

    }
    //목록에서 선택한 항목값
    private void EditModelToProperty(Bird model)
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
        Lat = EditModel.Lat;
        Lng = EditModel.Lng;
        IndividualCount = EditModel.IndividualCount;
        MigratoryType = EditModel.MigratoryType;
        Category = EditModel.Category;
        CategoryDetail = EditModel.CategoryDetail;
        HabitatType = EditModel.HabitatType;
        Surveyor = EditModel.Surveyor;
        Note = EditModel.Note;
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
        Site = null;
        SpeciesKo = string.Empty;
        SpeciesEn = null;
        Feature = null;
        Lat = null;
        Lng = null;
        IndividualCount = null;
        MigratoryType = null;
        Category = null;
        CategoryDetail = null;
        HabitatType = null;
        Surveyor = null;
        Note = null;
    }

    #endregion 

    #region Messages

    private void ModelRecevice(ModelMessage<Bird> message)
    {
        SettingProperties(message.Model);
    }

    #endregion
}
