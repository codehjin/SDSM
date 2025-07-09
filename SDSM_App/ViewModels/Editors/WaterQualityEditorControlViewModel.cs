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
    [ObservableProperty] private double? _flow;
    [ObservableProperty] private double? _cl;
    [ObservableProperty] private double? _sO42;
    [ObservableProperty] private double? _waterTemperature;
    [ObservableProperty] private double? _cu;
    [ObservableProperty] private double? _zn;
    [ObservableProperty] private double? _cr;
    [ObservableProperty] private double? _temperature;
    [ObservableProperty] private double? _chla;
    [ObservableProperty] private double? _electricalConductivity;
    [ObservableProperty] private double? _turbidity;
    [ObservableProperty] private int? _pHGrade;
    [ObservableProperty] private string? _bODGrade;
    [ObservableProperty] private string? _cODGrade;
    [ObservableProperty] private string? _tOCGrade;
    [ObservableProperty] private int? _sSGrade;
    [ObservableProperty] private string? _dOGrade;
    [ObservableProperty] private string? _tPGrade;
    [ObservableProperty] private string? _eColiGrade;

    #endregion

    #region ICommand 구현

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(YearChsu)
            && !string.IsNullOrWhiteSpace(Site)
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
                Flow = Flow,
                Cl = Cl,
                SO42 = SO42,
                WaterTemperature = WaterTemperature,
                Cu = Cu,
                Zn = Zn,
                Cr = Cr,
                Temperature = Temperature,
                Chla = Chla,
                ElectricalConductivity = ElectricalConductivity,
                Turbidity = Turbidity,
                PHGrade = GetPhGrade(),
                BODGrade = GetBodGrade(),
                CODGrade = GetCodGrade(),
                TOCGrade = GetTocGrade(),
                SSGrade = GetSsGrade(),
                DOGrade = GetDoGrade(),
                TPGrade = GetTpGrade(),
                EColiGrade = GetEColiGrade(),
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
            EditModel.Flow = Flow;
            EditModel.Cl = Cl;
            EditModel.SO42 = SO42;
            EditModel.WaterTemperature = WaterTemperature;
            EditModel.Cu = Cu;
            EditModel.Zn = Zn;
            EditModel.Cr = Cr;
            EditModel.Temperature = Temperature;
            EditModel.Chla = Chla;
            EditModel.ElectricalConductivity = ElectricalConductivity;
            EditModel.Turbidity = Turbidity;
            EditModel.PHGrade = GetPhGrade();
            EditModel.BODGrade = GetBodGrade();
            EditModel.CODGrade = GetCodGrade();
            EditModel.TOCGrade = GetTocGrade();
            EditModel.SSGrade = GetSsGrade();
            EditModel.DOGrade = GetDoGrade();
            EditModel.TPGrade = GetTpGrade();
            EditModel.EColiGrade = GetEColiGrade();

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
        Flow = EditModel.Flow;
        Cl = EditModel.Cl;
        SO42 = EditModel.SO42;
        WaterTemperature = EditModel.WaterTemperature;
        Cu = EditModel.Cu;
        Zn = EditModel.Zn;
        Cr = EditModel.Cr;
        Temperature = EditModel.Temperature;
        Chla = EditModel.Chla;
        ElectricalConductivity = EditModel.ElectricalConductivity;
        Turbidity = EditModel.Turbidity;
        PHGrade = EditModel.PHGrade;
        BODGrade = EditModel.BODGrade;
        CODGrade = EditModel.CODGrade;
        TOCGrade = EditModel.TOCGrade;
        SSGrade = EditModel.SSGrade;
        DOGrade = EditModel.DOGrade;
        TPGrade = EditModel.TPGrade;
        EColiGrade = EditModel.EColiGrade;
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
        Flow = null;
        Cl = null;
        SO42 = null;
        WaterTemperature = null;
        Cu = null;
        Zn = null;
        Cr = null;
        Temperature = null;
        Chla = null;
        ElectricalConductivity = null;
        Turbidity = null;
        PHGrade = null;
        BODGrade = null;
        CODGrade = null;
        TOCGrade = null;
        SSGrade = null;
        DOGrade = null;
        TPGrade = null;
        EColiGrade = null;
    }

    #endregion 

    #region Messages

    private void ModelRecevice(ModelMessage<WaterQuality> message)
    {
        SettingProperties(message.Model);
    }

    #endregion

    #region 등급계산

    private int? GetPhGrade()
    {
        if (PH != null)
            return (PH >= 6.5 && PH <= 8.5) ? 1 : 4;
        return 4;
    }

    private string GetBodGrade()
    {
        if (BOD == null)
            return "6";
        if (BOD <= 1) return "1a";
        if (BOD <= 2) return "1b";
        if (BOD <= 3) return "2";
        if (BOD <= 5) return "3";
        if (BOD <= 8) return "4";
        if (BOD <= 10) return "5";
        return "6";
    }

    private string GetCodGrade()
    {
        if (COD == null)
            return "6";
        if (COD <= 2) return "1a";
        if (COD <= 4) return "1b";
        if (COD <= 5) return "2";
        if (COD <= 7) return "3";
        if (COD <= 9) return "4";
        if (COD <= 11) return "5";
        return "6";
    }

    private string GetTocGrade()
    {
        if (TOC == null)
            return "6";
        if (TOC <= 2) return "1a";
        if (TOC <= 3) return "1b";
        if (TOC <= 4) return "2";
        if (TOC <= 5) return "3";
        if (TOC <= 6) return "4";
        if (TOC <= 8) return "5";
        return "6";
    }

    private int? GetSsGrade()
    {
        if (SS == null)
            return 5;
        if (SS <= 25) return 1;
        if (SS <= 100) return 4;
        return 5;
    }

    private string GetDoGrade()
    {
        if (DO == null)
            return "6";
        if (DO >= 7.5) return "1a";
        if (DO >= 5) return "1b";
        if (DO >= 2) return "4";
        return "6";
    }

    private string GetTpGrade()
    {
        if (TP == null)
            return "6";
        if (TP <= 0.02) return "1a";
        if (TP <= 0.04) return "1b";
        if (TP <= 0.1) return "2";
        if (TP <= 0.2) return "3";
        if (TP <= 0.3) return "4";
        if (TP <= 0.5) return "5";
        return "6";
    }

    private string GetEColiGrade()
    {
        if (EColi == null)
            return "4";
        if (EColi <= 50) return "1a";
        if (EColi <= 500) return "1b";
        if (EColi <= 1000) return "2";
        if (EColi <= 5000) return "3";
        return "4";
    }

    #endregion
}
