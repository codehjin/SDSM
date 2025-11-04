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

public partial class HabitatWaterEdgeEditorControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public HabitatWaterEdgeEditorControlViewModel()
    {
        //수정모델 받기
        WeakReferenceMessenger.Default.Register<ModelMessage<HabitatWaterEdge>>(this, (r, m) =>
        {
            ModelRecevice(m);
        });
    }

    #endregion

    #region 속성

    [ObservableProperty] private string _formTitle = "신규";
    [ObservableProperty] private EditStatusType _editStatus = EditStatusType.Add;

    [ObservableProperty] private HabitatWaterEdge _editModel = new();

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _yearChsu = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _site = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private DateTime? _surveyDate;
    [ObservableProperty] private string? _majorRegion;
    [ObservableProperty] private string? _middleRegion;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _river = string.Empty;
    [ObservableProperty] private string? _riverType;
    [ObservableProperty] private double? _lat;
    [ObservableProperty] private double? _lng;
    [ObservableProperty] private string? _weather;
    [ObservableProperty] private string? _surveyAgency;
    [ObservableProperty] private string? _surveyor;
    [ObservableProperty] private string? _surveyUnavailableReason;
    [ObservableProperty] private string? _note;
    [ObservableProperty] private double? _evaluationItem1;
    [ObservableProperty] private double? _evaluationItem2;
    [ObservableProperty] private double? _evaluationItem3;
    [ObservableProperty] private double? _evaluationItem4;
    [ObservableProperty] private double? _evaluationItem5;
    [ObservableProperty] private double? _evaluationItem6;
    [ObservableProperty] private double? _evaluationItem7;
    [ObservableProperty] private double? _evaluationItem8;
    [ObservableProperty] private double? _evaluationItem9;
    [ObservableProperty] private double? _evaluationItem10;
    [ObservableProperty] private double? _evaluationScore;
    [ObservableProperty] private string? _evaluationGrade;

    #endregion

    #region ICommand 구현

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(YearChsu)
            && !string.IsNullOrWhiteSpace(Site)
            && !string.IsNullOrWhiteSpace(River)
            && SurveyDate != null;
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
            var repository = App.Current.Services.GetRequiredService<IHabitatWaterEdgeRepository>();
            var addModel = await repository.InsertAsync(model);
            if (addModel != null)
            {
                var toast = (GB.SaveMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
                ClearProperty();

                WeakReferenceMessenger.Default.Send(new EditorMessage<HabitatWaterEdge>(addModel, EditStatusType.Add));
            }
        }
    }

    //수정저장
    private async void EditSave()
    {
        if (IsEditItemToModel())
        {
            var repository = App.Current.Services.GetRequiredService<IHabitatWaterEdgeRepository>();
            var isEdit = await repository.UpdateAsync(EditModel);
            if (isEdit)
            {
                var toast = (GB.EditMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

                WeakReferenceMessenger.Default.Send(new EditorMessage<HabitatWaterEdge>(EditModel, EditStatusType.Edit));
            }
        }
    }

    //신규 항목값
    private HabitatWaterEdge? AddItemToModel()
    {
        try
        {
            var (score, grade) = GetGrade();

            var model = new HabitatWaterEdge
            {
                YearChsu = YearChsu,
                Site = Site,
                SurveyDate = SurveyDate,
                MajorRegion = MajorRegion,
                MiddleRegion = MiddleRegion,
                River = River,
                RiverType = RiverType,
                Lat = Lat,
                Lng = Lng,
                Weather = Weather,
                SurveyAgency = SurveyAgency,
                Surveyor = Surveyor,
                SurveyUnavailableReason = SurveyUnavailableReason,
                Note = Note,
                EvaluationItem1 = EvaluationItem1,
                EvaluationItem2 = EvaluationItem2,
                EvaluationItem3 = EvaluationItem3,
                EvaluationItem4 = EvaluationItem4,
                EvaluationItem5 = EvaluationItem5,
                EvaluationItem6 = EvaluationItem6,
                EvaluationItem7 = EvaluationItem7,
                EvaluationItem8 = EvaluationItem8,
                EvaluationItem9 = EvaluationItem9,
                EvaluationItem10 = EvaluationItem10,
                EvaluationScore = score,
                EvaluationGrade = grade,
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
            var (score, grade) = GetGrade();

            EditModel.YearChsu = YearChsu;
            EditModel.Site = Site;
            EditModel.SurveyDate = SurveyDate;
            EditModel.MajorRegion = MajorRegion;
            EditModel.MiddleRegion = MiddleRegion;
            EditModel.River = River;
            EditModel.RiverType = RiverType;
            EditModel.Lat = Lat;
            EditModel.Lng = Lng;
            EditModel.Weather = Weather;
            EditModel.SurveyAgency = SurveyAgency;
            EditModel.Surveyor = Surveyor;
            EditModel.SurveyUnavailableReason = SurveyUnavailableReason;
            EditModel.Note = Note;
            EditModel.EvaluationItem1 = EvaluationItem1;
            EditModel.EvaluationItem2 = EvaluationItem2;
            EditModel.EvaluationItem3 = EvaluationItem3;
            EditModel.EvaluationItem4 = EvaluationItem4;
            EditModel.EvaluationItem5 = EvaluationItem5;
            EditModel.EvaluationItem6 = EvaluationItem6;
            EditModel.EvaluationItem7 = EvaluationItem7;
            EditModel.EvaluationItem8 = EvaluationItem8;
            EditModel.EvaluationItem9 = EvaluationItem9;
            EditModel.EvaluationItem10 = EvaluationItem10;
            EditModel.EvaluationScore = score;
            EditModel.EvaluationGrade = grade;

            return true;
        }
        catch { return false; }
    }

    //신규, 수정 세팅
    private void SettingProperties(HabitatWaterEdge model)
    {
        if (model.HabitatWaterEdgeId > 0)
        {
            EditModelToProperty(model);
        }
        else
        {
            ClearProperty();
        }

    }
    //목록에서 선택한 항목값
    private void EditModelToProperty(HabitatWaterEdge model)
    {
        EditModel = model;

        FormTitle = "수정";
        EditStatus = EditStatusType.Edit;

        YearChsu = EditModel.YearChsu;
        Site = EditModel.Site;
        SurveyDate = EditModel.SurveyDate;
        MajorRegion = EditModel.MajorRegion;
        MiddleRegion = EditModel.MiddleRegion;
        River = EditModel.River;
        RiverType = EditModel.RiverType;
        Lat = EditModel.Lat;
        Lng = EditModel.Lng;
        Weather = EditModel.Weather;
        SurveyAgency = EditModel.SurveyAgency;
        Surveyor = EditModel.Surveyor;
        SurveyUnavailableReason = EditModel.SurveyUnavailableReason;
        Note = EditModel.Note;
        EvaluationItem1 = EditModel.EvaluationItem1;
        EvaluationItem2 = EditModel.EvaluationItem2;
        EvaluationItem3 = EditModel.EvaluationItem3;
        EvaluationItem4 = EditModel.EvaluationItem4;
        EvaluationItem5 = EditModel.EvaluationItem5;
        EvaluationItem6 = EditModel.EvaluationItem6;
        EvaluationItem7 = EditModel.EvaluationItem7;
        EvaluationItem8 = EditModel.EvaluationItem8;
        EvaluationItem9 = EditModel.EvaluationItem9;
        EvaluationItem10 = EditModel.EvaluationItem10;
        EvaluationScore = EditModel.EvaluationScore;
        EvaluationGrade = EditModel.EvaluationGrade;
    }

    //항목초기화
    private void ClearProperty()
    {
        FormTitle = "신규";
        EditStatus = EditStatusType.Add;
        EditModel = new();

        YearChsu = string.Empty;
        Site = string.Empty;
        SurveyDate = null;
        MajorRegion = null;
        MiddleRegion = null;
        River = string.Empty;
        RiverType = null;
        Lat = null;
        Lng = null;
        Weather = null;
        SurveyAgency = null;
        Surveyor = null;
        SurveyUnavailableReason = null;
        Note = null;
        EvaluationItem1 = null;
        EvaluationItem2 = null;
        EvaluationItem3 = null;
        EvaluationItem4 = null;
        EvaluationItem5 = null;
        EvaluationItem6 = null;
        EvaluationItem7 = null;
        EvaluationItem8 = null;
        EvaluationItem9 = null;
        EvaluationItem10 = null;
        EvaluationScore = null;
        EvaluationGrade = null;
    }

    //평가등급
    private (double, string) GetGrade()
    {
        string grade = "-";
        double score = 0;

        // 평가 항목을 배열로 묶어 합산
        var items = new double?[]
        {
            EvaluationItem1, EvaluationItem2, EvaluationItem3, EvaluationItem4, EvaluationItem5,
            EvaluationItem6, EvaluationItem7, EvaluationItem8, EvaluationItem9, EvaluationItem10
        };
        double total = items.Sum(x => x ?? 0);

        score = total != 0 ? Math.Round(total / 2, 1, MidpointRounding.AwayFromZero) : 0;

        // "접근불가"가 아니면 등급 산정
        if ((SurveyUnavailableReason ?? string.Empty).Contains("접근불가") == false)
        {
            if (score >= 80) grade = "A";
            else if (score >= 60) grade = "B";
            else if (score >= 40) grade = "C";
            else if (score >= 20) grade = "D";
            else if (score >= 0) grade = "E";
        }
        return (score, grade);
    }

    #endregion 

    #region Messages

    private void ModelRecevice(ModelMessage<HabitatWaterEdge> message)
    {
        SettingProperties(message.Model);
    }

    #endregion
}
