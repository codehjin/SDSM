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
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _project = "방류하천";

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _yearChsu = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string? _site;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private DateTime? _surveyDate;
    [ObservableProperty] private string? _majorRegion;
    [ObservableProperty] private string? _middleRegion;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string? _river;
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
    [ObservableProperty] private int? _siteDivisionId;

    #endregion

    #region ICommand 구현

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(YearChsu)
            && SurveyDate != null
            && !string.IsNullOrWhiteSpace(River)   // 필수 표시(*)와 같게(2026-10-28 A4)
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
            EditModel.SiteDivisionId = SiteDivisionId;

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
        Project = EditModel.SiteDivision.Project;

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
        SiteDivisionId = EditModel.SiteDivisionId;

        //옛 기록에 하천명이 비어 있으면 저장 전에 알림 — [저장]은 하천명이 있어야 켜짐(2026-10-28 A4)
        if (string.IsNullOrWhiteSpace(River))
        {
            var toast = ("하천명이 비어 있습니다. 하천명을 넣어야 저장할 수 있습니다.", false);
            WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
        }
    }

    //항목초기화
    private void ClearProperty()
    {
        FormTitle = "신규";
        EditStatus = EditStatusType.Add;
        EditModel = new();

        YearChsu = string.Empty;
        Site = null;
        SurveyDate = null;
        MajorRegion = null;
        MiddleRegion = null;
        River = null;
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
        SiteDivisionId = null;
    }

    //평가등급
    // ⚠ 계산은 `GB.GetHabitatGrade`(= SDSM_Core\Ecology\HabitatEvaluator) **한 곳**에서 한다(2026-08-22).
    //   종전에는 이 화면과 일괄입력이 각자 같은 계산식을 들고 있어, 판정 기준을 한쪽만 고치면
    //   같은 자료가 입력 경로에 따라 다른 등급이 됐다. 근거: docs\_excel_formula_audit.md §5-5
    //
    //   반환형이 nullable 인 이유는 종전과 같다(2026-08-20). 평가항목을 하나도 고르지 않았으면
    //   점수·등급을 내지 않고 null 로 둔다 — 0점·E등급으로 저장하면 아직 평가하지 않은 지점이
    //   최하등급으로 DB에 남는다. 결측(null)과 0을 구분하는 원칙(CLAUDE.md §7.2). 근거: docs\ADMIN_TASKS.md §7-1
    //   단, **10개를 전부 0점으로 고른 경우는 미입력이 아니다.** 0점·E등급이 나온다(2026-08-22).
    private (double?, string?) GetGrade()
    {
        // 평가 항목을 배열로 묶어 넘긴다(순서 = 항목1~10)
        var items = new double?[]
        {
            EvaluationItem1, EvaluationItem2, EvaluationItem3, EvaluationItem4, EvaluationItem5,
            EvaluationItem6, EvaluationItem7, EvaluationItem8, EvaluationItem9, EvaluationItem10
        };

        return GB.GetHabitatGrade(items, SurveyUnavailableReason);
    }

    #endregion 

    #region Messages

    private void ModelRecevice(ModelMessage<HabitatWaterEdge> message)
    {
        SettingProperties(message.Model);
    }

    #endregion
}
