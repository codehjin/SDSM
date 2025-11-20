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

public partial class WaterQualityEditorControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public WaterQualityEditorControlViewModel()
    {
        //수정모델 받기
        WeakReferenceMessenger.Default.Register<ModelMessage<WaterQuality>>(this, (r, m) =>
        {
            ModelRecevice(m);
        });
    }

    #endregion

    #region 속성

    [ObservableProperty] private string _formTitle = "신규";
    [ObservableProperty] private EditStatusType _editStatus = EditStatusType.Add;

    [ObservableProperty] private WaterQuality _editModel = new();
    [ObservableProperty] private string _project = "방류하천";

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _yearChsu = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _site = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private DateTime? _surveyDate;
    [ObservableProperty] private string? _majorRegion;
    [ObservableProperty] private string? _middleRegion;
    [ObservableProperty] private string? _river;
    [ObservableProperty] private string? _riverType;
    [ObservableProperty] private double? _lat;
    [ObservableProperty] private double? _lng;
    [ObservableProperty] private string? _weather;
    [ObservableProperty] private string? _surveyAgency;
    [ObservableProperty] private string? _surveyor;
    [ObservableProperty] private double? _pH;
    [ObservableProperty] private double? _bOD;
    [ObservableProperty] private double? _cOD;
    [ObservableProperty] private double? _tOC;
    [ObservableProperty] private double? _sS;
    [ObservableProperty] private double? _dO;
    [ObservableProperty] private double? _tP;
    [ObservableProperty] private double? _ecotoxicity;
    [ObservableProperty] private double? _eColi;

    [ObservableProperty] private double? _tN;
    [ObservableProperty] private double? _eC;
    [ObservableProperty] private double? _cl;
    [ObservableProperty] private double? _sO42;
    [ObservableProperty] private double? _cu;
    [ObservableProperty] private double? _zn;
    [ObservableProperty] private double? _cr;
    [ObservableProperty] private double? _turbidity;
    [ObservableProperty] private double? _chla;
    [ObservableProperty] private double? _waterTemperature;
    [ObservableProperty] private double? _waterDepth;
    [ObservableProperty] private double? _flowVelocity;
    [ObservableProperty] private double? _flowSec;
    [ObservableProperty] private double? _flowDay;

    [ObservableProperty] private int? _pHGrade;
    [ObservableProperty] private string? _bODGrade;
    [ObservableProperty] private string? _cODGrade;
    [ObservableProperty] private string? _tOCGrade;
    [ObservableProperty] private int? _sSGrade;
    [ObservableProperty] private string? _dOGrade;
    [ObservableProperty] private string? _tPGrade;
    [ObservableProperty] private string? _eColiGrade;
    [ObservableProperty] private int? _siteDivisionId;

    #endregion

    #region ICommand 구현

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(YearChsu)
            && !string.IsNullOrWhiteSpace(Site)
            && SurveyDate != null;
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
            var repository = App.Current.Services.GetRequiredService<IWaterQualityRepository>();
            var addModel = await repository.InsertAsync(model);
            if (addModel != null)
            {
                var toast = (GB.SaveMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
                ClearProperty();

                WeakReferenceMessenger.Default.Send(new EditorMessage<WaterQuality>(addModel, EditStatusType.Add));
            }
        }
    }

    //수정저장
    private async void EditSave()
    {
        if (IsEditItemToModel())
        {
            var repository = App.Current.Services.GetRequiredService<IWaterQualityRepository>();
            var isEdit = await repository.UpdateAsync(EditModel);
            if (isEdit)
            {
                var toast = (GB.EditMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

                WeakReferenceMessenger.Default.Send(new EditorMessage<WaterQuality>(EditModel, EditStatusType.Edit));
            }
        }
    }

    //신규 항목값
    private WaterQuality? AddItemToModel()
    {
        try
        {
            var model = new WaterQuality
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
                PH = PH,
                BOD = BOD,
                COD = COD,
                TOC = TOC,
                SS = SS,
                DO = DO,
                TP = TP,
                Ecotoxicity = Ecotoxicity,
                EColi = EColi,
               
                TN = TN,
                EC = EC,
                Cl = Cl,
                SO42 = SO42,
                Cu = Cu,
                Zn = Zn,
                Cr = Cr,
                Turbidity = Turbidity,
                Chla = Chla,
                WaterTemperature = WaterTemperature,
                WaterDepth = WaterDepth,
                FlowVelocity = FlowVelocity,
                FlowSec = FlowSec,
                FlowDay = FlowDay,
                PHGrade = GB.GetPhGrade(PH),
                BODGrade = GB.GetBodGrade(BOD),
                CODGrade = GB.GetCodGrade(COD),
                TOCGrade = GB.GetTocGrade(TOC),
                SSGrade = GB.GetSsGrade(SS),
                DOGrade = GB.GetDoGrade(DO),
                TPGrade = GB.GetTpGrade(TP),
                EColiGrade = GB.GetEColiGrade(EColi),
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
            EditModel.PH = PH;
            EditModel.BOD = BOD;
            EditModel.COD = COD;
            EditModel.TOC = TOC;
            EditModel.SS = SS;
            EditModel.DO = DO;
            EditModel.TP = TP;
            EditModel.Ecotoxicity = Ecotoxicity;
            EditModel.EColi = EColi;
            EditModel.TN = TN;
            EditModel.EC = EC;
            EditModel.Cl = Cl;
            EditModel.SO42 = SO42;
            EditModel.Cu = Cu;
            EditModel.Zn = Zn;
            EditModel.Cr = Cr;
            EditModel.Turbidity = Turbidity;
            EditModel.Chla = Chla;
            EditModel.WaterTemperature = WaterTemperature;
            EditModel.WaterDepth = WaterDepth;
            EditModel.FlowVelocity = FlowVelocity;
            EditModel.FlowSec = FlowSec;
            EditModel.FlowDay = FlowDay;            
            EditModel.PHGrade = GB.GetPhGrade(PH);
            EditModel.BODGrade = GB.GetBodGrade(BOD);
            EditModel.CODGrade = GB.GetCodGrade(COD);
            EditModel.TOCGrade = GB.GetTocGrade(TOC);
            EditModel.SSGrade = GB.GetSsGrade(SS);
            EditModel.DOGrade = GB.GetDoGrade(DO);
            EditModel.TPGrade = GB.GetTpGrade(TP);
            EditModel.EColiGrade = GB.GetEColiGrade(EColi);
            EditModel.SiteDivisionId = SiteDivisionId;

            return true;
        }
        catch { return false; }
    }

    //신규, 수정 세팅
    private void SettingProperties(WaterQuality model)
    {
        if (model.WaterQualityId > 0)
        {
            EditModelToProperty(model);
        }
        else
        {
            ClearProperty();
        }

    }
    //목록에서 선택한 항목값
    private void EditModelToProperty(WaterQuality model)
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
        PH = EditModel.PH;
        BOD = EditModel.BOD;
        COD = EditModel.COD;
        TOC = EditModel.TOC;
        SS = EditModel.SS;
        DO = EditModel.DO;
        TP = EditModel.TP;
        Ecotoxicity = EditModel.Ecotoxicity;
        EColi = EditModel.EColi;
        TN = EditModel.TN;
        EC = EditModel.EC;
        Cl = EditModel.Cl;
        SO42 = EditModel.SO42;
        Cu = EditModel.Cu;
        Zn = EditModel.Zn;
        Cr = EditModel.Cr;
        Turbidity = EditModel.Turbidity;
        Chla = EditModel.Chla;
        WaterTemperature = EditModel.WaterTemperature;
        WaterDepth = EditModel.WaterDepth;
        FlowVelocity = EditModel.FlowVelocity;
        FlowSec = EditModel.FlowSec;
        FlowDay = EditModel.FlowDay;
        PHGrade = EditModel.PHGrade;
        BODGrade = EditModel.BODGrade;
        CODGrade = EditModel.CODGrade;
        TOCGrade = EditModel.TOCGrade;
        SSGrade = EditModel.SSGrade;
        DOGrade = EditModel.DOGrade;
        TPGrade = EditModel.TPGrade;
        EColiGrade = EditModel.EColiGrade;
        SiteDivisionId = EditModel.SiteDivisionId;
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
        River = null;
        RiverType = null;
        Lat = null;
        Lng = null;
        Weather = null;
        SurveyAgency = null;
        Surveyor = null;
        PH = null;
        BOD = null;
        COD = null;
        TOC = null;
        SS = null;
        DO = null;
        TP = null;
        Ecotoxicity = null;
        EColi = null;
        TN = null;
        EC = null;
        Cl = null;
        SO42 = null;
        Cu = null;
        Zn = null;
        Cr = null;
        Turbidity = null;
        Chla = null;
        WaterTemperature = null;
        WaterDepth = null;
        FlowVelocity = null;
        FlowSec = null;
        FlowDay = null;
        PHGrade = null;
        BODGrade = null;
        CODGrade = null;
        TOCGrade = null;
        SSGrade = null;
        DOGrade = null;
        TPGrade = null;
        EColiGrade = null;
        SiteDivisionId = null;
    }

    #endregion 

    #region Messages

    private void ModelRecevice(ModelMessage<WaterQuality> message)
    {
        SettingProperties(message.Model);
    }

    #endregion

}
