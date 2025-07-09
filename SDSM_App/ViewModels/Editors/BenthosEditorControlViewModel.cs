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

public partial class BenthosEditorControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public BenthosEditorControlViewModel()
    {
        //수정모델 받기
        WeakReferenceMessenger.Default.Register<ModelMessage<Benthos>>(this, (r, m) =>
        {
            ModelRecevice(m);
        });
    }

    #endregion

    #region 속성

    [ObservableProperty] private string _formTitle = "신규";
    [ObservableProperty] private EditStatusType _editStatus = EditStatusType.Add;

    [ObservableProperty] private Benthos _editModel = new();

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
    [ObservableProperty] private int? _surbernet30;
    [ObservableProperty] private int? _surbernet50;
    [ObservableProperty] private int? _dredge;
    [ObservableProperty] private int? _ekman;
    [ObservableProperty] private string?_watershed;
    [ObservableProperty] private string? _pollutionSource;
    [ObservableProperty] private string? _camopyCover;
    [ObservableProperty] private string? _floodplain;
    [ObservableProperty] private string? _leveeLeft;
    [ObservableProperty] private string? _leveeRight;
    [ObservableProperty] private int? _bedrock;
    [ObservableProperty] private int? _concrete;
    [ObservableProperty] private int? _mud;
    [ObservableProperty] private int? _sand;
    [ObservableProperty] private int? _fineGravel;
    [ObservableProperty] private int? _gravel;
    [ObservableProperty] private int? _smallStone;
    [ObservableProperty] private int? _bigStone;
    [ObservableProperty] private string? _habitatRiverType;
    [ObservableProperty] private double? _riverWidth;
    [ObservableProperty] private double? _waterWidth;
    [ObservableProperty] private double? _averageDepth;
    [ObservableProperty] private double? _averageVelocity;
    [ObservableProperty] private double? _temperature;
    [ObservableProperty] private double? _waterTemperature;
    [ObservableProperty] private string? _flowState;
    [ObservableProperty] private int? _transparency;
    [ObservableProperty] private int? _smell;
    [ObservableProperty] private string? _note;
    [ObservableProperty] private string? _surveyUnavailableReason;
    [ObservableProperty] private string? _bMIGrade;
    [ObservableProperty] private double? _indexDI;
    [ObservableProperty] private double? _indexH;
    [ObservableProperty] private double? _indexR1;
    [ObservableProperty] private double? _indexJ;

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
            var repository = App.Current.Services.GetRequiredService<IBenthosRepository>();
            var addModel = await repository.InsertAsync(model);
            if (addModel != null)
            {
                var toast = (GB.SaveMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
                ClearProperty();

                WeakReferenceMessenger.Default.Send(new EditorMessage<Benthos>(addModel, EditStatusType.Add));
            }
        }
    }

    //수정저장
    private async void EditSave()
    {
        if (IsEditItemToModel())
        {
            var repository = App.Current.Services.GetRequiredService<IBenthosRepository>();
            var isEdit = await repository.UpdateAsync(EditModel);
            if (isEdit)
            {
                var toast = (GB.EditMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

                WeakReferenceMessenger.Default.Send(new EditorMessage<Benthos>(EditModel, EditStatusType.Edit));
            }
        }
    }

    //신규 항목값
    private Benthos? AddItemToModel()
    {
        try
        {
            var model = new Benthos
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
                Surbernet30 = Surbernet30,
                Surbernet50 = Surbernet50,
                Dredge = Dredge,
                Ekman = Ekman,
                Watershed = Watershed,
                PollutionSource = PollutionSource,
                CamopyCover = CamopyCover,
                Floodplain = Floodplain,
                LeveeLeft = LeveeLeft,
                LeveeRight = LeveeRight,
                Bedrock = Bedrock,
                Concrete = Concrete,
                Mud = Mud,
                Sand = Sand,
                FineGravel = FineGravel,
                Gravel = Gravel,
                SmallStone = SmallStone,
                BigStone = BigStone,
                HabitatRiverType = HabitatRiverType,
                RiverWidth = RiverWidth,
                WaterWidth = WaterWidth,
                AverageDepth = AverageDepth,
                AverageVelocity = AverageVelocity,
                Temperature = Temperature,
                WaterTemperature = WaterTemperature,
                FlowState = FlowState,
                Transparency = Transparency,
                Smell = Smell,
                Note = Note,
                SurveyUnavailableReason = SurveyUnavailableReason,
                BMIGrade = BMIGrade,
                IndexDI = IndexDI,
                IndexH = IndexH,
                IndexR1 = IndexR1,
                IndexJ = IndexJ,
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
            EditModel.Surbernet30 = Surbernet30;
            EditModel.Surbernet50 = Surbernet50;
            EditModel.Dredge = Dredge;
            EditModel.Ekman = Ekman;
            EditModel.Watershed = Watershed;
            EditModel.PollutionSource = PollutionSource;
            EditModel.CamopyCover = CamopyCover;
            EditModel.Floodplain = Floodplain;
            EditModel.LeveeLeft = LeveeLeft;
            EditModel.LeveeRight = LeveeRight;
            EditModel.Bedrock = Bedrock;
            EditModel.Concrete = Concrete;
            EditModel.Mud = Mud;
            EditModel.Sand = Sand;
            EditModel.FineGravel = FineGravel;
            EditModel.Gravel = Gravel;
            EditModel.SmallStone = SmallStone;
            EditModel.BigStone = BigStone;
            EditModel.HabitatRiverType = HabitatRiverType;
            EditModel.RiverWidth = RiverWidth;
            EditModel.WaterWidth = WaterWidth;
            EditModel.AverageDepth = AverageDepth;
            EditModel.AverageVelocity = AverageVelocity;
            EditModel.Temperature = Temperature;
            EditModel.WaterTemperature = WaterTemperature;
            EditModel.FlowState = FlowState;
            EditModel.Transparency = Transparency;
            EditModel.Smell = Smell;
            EditModel.Note = Note;
            EditModel.SurveyUnavailableReason = SurveyUnavailableReason;
            EditModel.BMIGrade = BMIGrade;
            EditModel.IndexDI = IndexDI;
            EditModel.IndexH = IndexH;
            EditModel.IndexR1 = IndexR1;
            EditModel.IndexJ = IndexJ;

            return true;
        }
        catch { return false; }
    }

    //신규, 수정 세팅
    private void SettingProperties(Benthos model)
    {
        if (model.BenthosId > 0)
        {
            EditModelToProperty(model);
        }
        else
        {
            ClearProperty();
        }

    }
    //목록에서 선택한 항목값
    private void EditModelToProperty(Benthos model)
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
        Surbernet30 = EditModel.Surbernet30;
        Surbernet50 = EditModel.Surbernet50;
        Dredge = EditModel.Dredge;
        Ekman = EditModel.Ekman;
        Watershed = EditModel.Watershed;
        PollutionSource = EditModel.PollutionSource;
        CamopyCover = EditModel.CamopyCover;
        Floodplain = EditModel.Floodplain;
        LeveeLeft = EditModel.LeveeLeft;
        LeveeRight = EditModel.LeveeRight;
        Bedrock = EditModel.Bedrock;
        Concrete = EditModel.Concrete;
        Mud = EditModel.Mud;
        Sand = EditModel.Sand;
        FineGravel = EditModel.FineGravel;
        Gravel = EditModel.Gravel;
        SmallStone = EditModel.SmallStone;
        BigStone = EditModel.BigStone;
        HabitatRiverType = EditModel.HabitatRiverType;
        RiverWidth = EditModel.RiverWidth;
        WaterWidth = EditModel.WaterWidth;
        AverageDepth = EditModel.AverageDepth;
        AverageVelocity = EditModel.AverageVelocity;
        Temperature = EditModel.Temperature;
        WaterTemperature = EditModel.WaterTemperature;
        FlowState = EditModel.FlowState;
        Transparency = EditModel.Transparency;
        Smell = EditModel.Smell;
        Note = EditModel.Note;
        SurveyUnavailableReason = EditModel.SurveyUnavailableReason;
        BMIGrade = EditModel.BMIGrade;
        IndexDI = EditModel.IndexDI;
        IndexH = EditModel.IndexH;
        IndexR1 = EditModel.IndexR1;
        IndexJ = EditModel.IndexJ;
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
        Surbernet30 = null;
        Surbernet50 = null;
        Dredge = null;
        Ekman = null;
        Watershed = null;
        PollutionSource = null;
        CamopyCover = null;
        Floodplain = null;
        LeveeLeft = null;
        LeveeRight = null;
        Bedrock = null;
        Concrete = null;
        Mud = null;
        Sand = null;
        FineGravel = null;
        Gravel = null;
        SmallStone = null;
        BigStone = null;
        HabitatRiverType = null;
        RiverWidth = null;
        WaterWidth = null;
        AverageDepth = null;
        AverageVelocity = null;
        Temperature = null;
        WaterTemperature = null;
        FlowState = null;
        Transparency = null;
        Smell = null;
        Note = null;
        SurveyUnavailableReason = null;
        BMIGrade = null;
        IndexDI = null;
        IndexH = null;
        IndexR1 = null;
        IndexJ = null;
    }

    #endregion 

    #region Messages

    private void ModelRecevice(ModelMessage<Benthos> message)
    {
        SettingProperties(message.Model);
    }

    #endregion

    #region 등급 및 군집지수

   
    #endregion
}
