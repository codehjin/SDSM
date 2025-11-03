using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SDSM_App.Commons;
using SDSM_App.Enums;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.Models;
using SDSM_App.Models.Repositories.Interfases;
using SDSM_Models;
using System.Data.SQLite;
using System.Windows;
using Telerik.Windows.Data;

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
    [ObservableProperty] private string? _watershed;
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
    [ObservableProperty] private double? _bMIScore;
    [ObservableProperty] private string? _bMIGrade;
    [ObservableProperty] private double? _indexDI;
    [ObservableProperty] private double? _indexH;
    [ObservableProperty] private double? _indexR1;
    [ObservableProperty] private double? _indexJ;

    #endregion

    #region 생물종 속성, ICommand

    [ObservableProperty] private RadObservableCollection<BenthosSpeciesList> _addSpeciesCollection = [];
    [ObservableProperty] private BenthosSpeciesList? _addSpeciesSelectedItem;

    [ObservableProperty] private int _addSpeciesCount;
    [ObservableProperty] private double _addIndividualCountSum;

    [RelayCommand]
    private void IndividualCountValueChangedExecuted()
    {
        AddSpeciesCount = AddSpeciesCollection.Count(m => m.IndividualCount > 0);
        AddIndividualCountSum = AddSpeciesCollection.Sum(m => m.IndividualCount) ?? 0;
    }

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
                var speciess = AddSpeciesCollection.Where(m => m.IndividualCount > 0).ToList();
                if (speciess.Count != 0)
                {
                    await Task.Run(() => SubBatchSave(addModel.BenthosId, speciess, true));
                }

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
                var speciess = AddSpeciesCollection.Where(m => m.IndividualCount > 0).ToList();

                await Task.Run(() => SubBatchSave(EditModel.BenthosId, speciess, false));

                var toast = (GB.EditMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

                WeakReferenceMessenger.Default.Send(new EditorMessage<Benthos>(EditModel, EditStatusType.Edit));
            }
        }
    }

    //종목록 일괄저장
    private async void SubBatchSave(int parentId, List<BenthosSpeciesList> speciess, bool isAdd)
    {
        try
        {
            if (isAdd == false)
            {
                //종목록 삭제 후 다시 추가
                var itemsDelete = new ItemsDelete<BenthosSpecies>();
                bool isSuccess = await Task.Run(() => itemsDelete.DeleteChild($"{nameof(Benthos)}Id", parentId));

                if (!isSuccess)
                {
                    GB.LogWrite(GetType().Name, nameof(SubBatchSave), "종목록 저장 실패");
                    MessageBox.Show("종목목록 저장에 실패했습니다.", "저장 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            string strConn = GB.SqliteDataSource;

            using var conn = new SQLiteConnection(strConn);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            foreach (var model in speciess)
            {
                string strSql = $"INSERT INTO BenthosSpecies" +
                                 $"(SaprobicValue, IndicatorWeight, PhylumKo, ClassKo, OrderKo, FamilyKo, SpeciesKo, SpeciesEn, IndividualCount, BenthosId)  " +
                                 $"VALUES " +
                                 $"(@SaprobicValue, @IndicatorWeight, @PhylumKo, @ClassKo, @OrderKo, @FamilyKo, @SpeciesKo, @SpeciesEn, @IndividualCount, @BenthosId)";

                using var command = new SQLiteCommand(strSql, conn);

                command.Parameters.AddWithValue("@SaprobicValue", model.SaprobicValue ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IndicatorWeight", model.IndicatorWeight ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PhylumKo", model.PhylumKo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ClassKo", model.ClassKo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@OrderKo", model.OrderKo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@FamilyKo", model.FamilyKo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SpeciesKo", model.SpeciesKo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SpeciesEn", model.SpeciesEn ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IndividualCount", model.IndividualCount ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@BenthosId", parentId);

                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(SubBatchSave), ex.Message);
            throw;
        }
    }

    //신규 항목값
    private Benthos? AddItemToModel()
    {
        try
        {
            var (indexDI, indexH, indexR1, indexJ, score, grade) = GetIndexAndGrade();

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
                IndexDI = indexDI,
                IndexH = indexH,
                IndexR1 = indexR1,
                IndexJ = indexJ,
                BMIScore = score,
                BMIGrade = grade,
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
            var (indexDI, indexH, indexR1, indexJ, score, grade) = GetIndexAndGrade();

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
            EditModel.IndexDI = indexDI;
            EditModel.IndexH = indexH;
            EditModel.IndexR1 = indexR1;
            EditModel.IndexJ = indexJ;
            EditModel.BMIScore = score;
            EditModel.BMIGrade = grade;

            return true;
        }
        catch { return false; }
    }

    //신규, 수정 세팅
    private void SettingProperties(Benthos model)
    {
        if (model.BenthosId > 0)
        {
            GetSpeciesData();

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
        IndexDI = EditModel.IndexDI;
        IndexH = EditModel.IndexH;
        IndexR1 = EditModel.IndexR1;
        IndexJ = EditModel.IndexJ;
        BMIScore = EditModel.BMIScore;
        BMIGrade = EditModel.BMIGrade;

        foreach (var sub in EditModel.Subs.ToList())
        {
            var speciesKoEn = $"{sub.SpeciesKo} {sub.SpeciesEn}";
            foreach (var item in AddSpeciesCollection)
            {
                if (item.SearchText == speciesKoEn)
                {
                    item.IndividualCount = sub.IndividualCount;
                    break;
                }
            }
        }

        AddSpeciesSelectedItem = null;
        AddSpeciesCount = AddSpeciesCollection.Count(m => m.IndividualCount > 0);
        AddIndividualCountSum = AddSpeciesCollection.Sum(m => m.IndividualCount) ?? 0;
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
        IndexDI = null;
        IndexH = null;
        IndexR1 = null;
        IndexJ = null;
        BMIScore = null;
        BMIGrade = null;

        GetSpeciesData();

        AddSpeciesSelectedItem = null;
        AddSpeciesCount = 0;
        AddIndividualCountSum = 0;
    }

    //종목록
    private async void GetSpeciesData()
    {
        try
        {
            using var context = new SDSMDbContext();

            AddSpeciesCollection.Clear();

            //종목록
            AddSpeciesCollection.AddRange(await context.BenthosSpeciesLists.ToListAsync());
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetSpeciesData), ex.Message);
        }
    }

    //군집지수 및 등급
    private (double, double, double, double, double?, string? ) GetIndexAndGrade()
    {
        try
        {
            //종목록
            var speciesData = AddSpeciesCollection.Where(m => m.IndividualCount > 0).Select(m => new ImportBenthosSpecies
            {
                SaprobicValue = m.SaprobicValue.ToString(),
                IndicatorWeight = m.IndicatorWeight.ToString(),
                PhylumKo = m.PhylumKo,
                ClassKo = m.ClassKo,
                OrderKo = m.OrderKo,
                FamilyKo = m.FamilyKo,
                SpeciesKo = m.SpeciesKo,
                SpeciesEn = m.SpeciesEn,
                IndividualCount = m.IndividualCount,
            }).ToList();

            // 1. IndividualCount 값만 추출 (null은 0으로 처리)
            int[] individualCounts = [.. speciesData.Select(x => x.IndividualCount.HasValue ? (int)x.IndividualCount.Value : 0)];

            // 2. 각 개체별로 점수 계산
            foreach (var species in speciesData)
            {
                int value = species.IndividualCount.HasValue ? (int)species.IndividualCount.Value : 0;
                int rankScorer = GB.GetRankScorer(individualCounts, value);

                // 점수 RankScorer 프로퍼티에 할당
                species.RankScorer = rankScorer;
            }

            var surveyUnavailableReason = SurveyUnavailableReason;

            var indexDI = GB.GetDI(speciesData);
            var indexH = GB.GetH(speciesData);
            var indexR1 = GB.GetR1(speciesData);
            var indexJ = GB.GetJ(speciesData);
            var (score, grade) = GB.GetBMI(speciesData, surveyUnavailableReason);

            return (indexDI, indexH, indexR1, indexJ, score, grade);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetIndexAndGrade), ex.Message);
            return (0, 0, 0, 0, null, null);
        }
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
