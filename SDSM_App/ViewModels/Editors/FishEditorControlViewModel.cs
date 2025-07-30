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
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using Telerik.Windows.Data;

namespace SDSM_App.ViewModels;

public partial class FishEditorControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public FishEditorControlViewModel()
    {
        //수정모델 받기
        WeakReferenceMessenger.Default.Register<ModelMessage<Fish>>(this, (r, m) =>
        {
            ModelRecevice(m);
        });
    }

    #endregion

    #region 속성

    [ObservableProperty] private string _formTitle = "신규";
    [ObservableProperty] private EditStatusType _editStatus = EditStatusType.Add;

    [ObservableProperty] private Fish _editModel = new();

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
    [ObservableProperty] private int? _collectionTime;
    [ObservableProperty] private string? _collectionTool;
    [ObservableProperty] private string? _collectionFlowState;
    [ObservableProperty] private int? _autoRiverChasu;
    [ObservableProperty] private string? _WatershedType;
    [ObservableProperty] private string? _drainageSystemType;
    [ObservableProperty] private string? _riverWidth;
    [ObservableProperty] private string? _yuWidth;
    [ObservableProperty] private string? _depth;
    [ObservableProperty] private string? _submerged;
    [ObservableProperty] private string? _floating;
    [ObservableProperty] private string? _emergent;
    [ObservableProperty] private int? _leftVacantLand;
    [ObservableProperty] private int? _leftH;
    [ObservableProperty] private int? _leftS;
    [ObservableProperty] private int? _leftT1;
    [ObservableProperty] private int? _rightVacantLand;
    [ObservableProperty] private int? _rightH;
    [ObservableProperty] private int? _rightS;
    [ObservableProperty] private int? _rightT1;
    [ObservableProperty] private string? _leveeLeft;
    [ObservableProperty] private string? _leveeRight;
    [ObservableProperty] private string? _confounder;
    [ObservableProperty] private string? _structure;
    [ObservableProperty] private string? _fishway;
    [ObservableProperty] private string? _etcNote;
    [ObservableProperty] private int? _bedrock;
    [ObservableProperty] private int? _concrete;
    [ObservableProperty] private int? _mud;
    [ObservableProperty] private int? _sand;
    [ObservableProperty] private int? _fineGravel;
    [ObservableProperty] private int? _gravel;
    [ObservableProperty] private int? _smallStone;
    [ObservableProperty] private int? _bigStone;
    [ObservableProperty] private string? _habitatRiverType;
    [ObservableProperty] private string? _habitatFlowState;
    [ObservableProperty] private string? _surveyUnavailableReason;
    [ObservableProperty] private string? _note;
    [ObservableProperty] private int? _dECount;
    [ObservableProperty] private int? _eFCount;
    [ObservableProperty] private int? _lECount;
    [ObservableProperty] private int? _tUCount;
    [ObservableProperty] private double? _fAIScore;
    [ObservableProperty] private string? _fAIGrade;

    #endregion

    #region 생물종 속성, ICommand

    [ObservableProperty] private RadObservableCollection<FishSpeciesList> _addSpeciesColleciton = [];
    [ObservableProperty] private FishSpeciesList? _addSpeciesSelectedItem;

    [ObservableProperty] private int _addSpeciesCount;
    [ObservableProperty] private double _addIndividualCountSum;

    [RelayCommand]
    private void IndividualCountValueChangedExecuted()
    {
        AddSpeciesCount = AddSpeciesColleciton.Count(m => m.IndividualCount > 0);
        AddIndividualCountSum = AddSpeciesColleciton.Sum(m => m.IndividualCount) ?? 0;
    }

    #endregion

    #region ICommand 구현

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(YearChsu)
            && !string.IsNullOrWhiteSpace(Site)
            && SurveyDate != null
            && AutoRiverChasu != null;
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
            var repository = App.Current.Services.GetRequiredService<IFishRepository>();
            var addModel = await repository.InsertAsync(model);
            if (addModel != null)
            {
                var speciess = AddSpeciesColleciton.Where(m => m.IndividualCount > 0).ToList();
                if (speciess.Count != 0)
                {
                    await Task.Run(() => SubBatchSave(addModel.FishId, speciess, true));
                }

                var toast = (GB.SaveMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
                ClearProperty();

                WeakReferenceMessenger.Default.Send(new EditorMessage<Fish>(addModel, EditStatusType.Add));
            }
        }
    }

    //수정저장
    private async void EditSave()
    {
        if (IsEditItemToModel())
        {
            var repository = App.Current.Services.GetRequiredService<IFishRepository>();
            var isEdit = await repository.UpdateAsync(EditModel);
            if (isEdit)
            {
                var speciess = AddSpeciesColleciton.Where(m => m.IndividualCount > 0).ToList();

                await Task.Run(() => SubBatchSave(EditModel.FishId, speciess, false));

                var toast = (GB.EditMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

                WeakReferenceMessenger.Default.Send(new EditorMessage<Fish>(EditModel, EditStatusType.Edit));
            }
        }
    }

    //종목록 일괄저장
    private async void SubBatchSave(int parentId, List<FishSpeciesList> speciess, bool isAdd)
    {
        try
        {
            if (isAdd == false)
            {
                //종목록 삭제 후 다시 추가
                var itemsDelete = new ItemsDelete<FishSpecies>();
                bool isSuccess = await Task.Run(() => itemsDelete.DeleteChild($"{nameof(Fish)}Id", parentId));

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
                string strSql = $"INSERT INTO FishSpecies" +
                                 $"(FishTrait, ToleranceGuild, FeedingGuild, HabitatGuild, Exotic, Endemic, Endangered1, Endangered2, NaturalMonument, LineageOrder, " +
                                 $"ClassKo, OrderKo, FamilyKo, SpeciesKo, SpeciesEn, IndividualCount, FishId)  " +
                                 $"VALUES " +
                                 $"(@FishTrait, @ToleranceGuild, @FeedingGuild, @HabitatGuild, @Exotic, @Endemic, @Endangered1, @Endangered2, @NaturalMonument, @LineageOrder, " +
                                 $"@ClassKo, @OrderKo, @FamilyKo, @SpeciesKo, @SpeciesEn, @IndividualCount, @FishId) ";

                using var command = new SQLiteCommand(strSql, conn);

                command.Parameters.AddWithValue("@FishTrait", model.FishTrait ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ToleranceGuild", model.ToleranceGuild ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@FeedingGuild", model.FeedingGuild ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@HabitatGuild", model.HabitatGuild ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Exotic", model.Exotic ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Endemic", model.Endemic ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Endangered1", model.Endangered1 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Endangered2", model.Endangered2 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@NaturalMonument", model.NaturalMonument ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@LineageOrder", model.LineageOrder ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ClassKo", model.ClassKo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@OrderKo", model.OrderKo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@FamilyKo", model.FamilyKo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SpeciesKo", model.SpeciesKo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SpeciesEn", model.SpeciesEn ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IndividualCount", model.IndividualCount ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@FishId", parentId);

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
    private Fish? AddItemToModel()
    {
        try
        {
            var (score, grade) = GetIndexAndGrade();

            var model = new Fish
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
                CollectionTime = CollectionTime,
                CollectionTool = CollectionTool,
                CollectionFlowState = CollectionFlowState,
                AutoRiverChasu = AutoRiverChasu,
                WatershedType = WatershedType,
                DrainageSystemType = DrainageSystemType,
                RiverWidth = RiverWidth,
                YuWidth = YuWidth,
                Depth = Depth,
                Submerged = Submerged,
                Floating = Floating,
                Emergent = Emergent,
                LeftVacantLand = LeftVacantLand,
                LeftH = LeftH,
                LeftS = LeftS,
                LeftT1 = LeftT1,
                RightVacantLand = RightVacantLand,
                RightH = RightH,
                RightS = RightS,
                RightT1 = RightT1,
                LeveeLeft = LeveeLeft,
                LeveeRight = LeveeRight,
                Confounder = Confounder,
                Structure = Structure,
                Fishway = Fishway,
                EtcNote = EtcNote,
                Bedrock = Bedrock,
                Concrete = Concrete,
                Mud = Mud,
                Sand = Sand,
                FineGravel = FineGravel,
                Gravel = Gravel,
                SmallStone = SmallStone,
                BigStone = BigStone,
                HabitatRiverType = HabitatRiverType,
                HabitatFlowState = HabitatFlowState,
                SurveyUnavailableReason = SurveyUnavailableReason,
                Note = Note,
                DECount = DECount,
                EFCount = EFCount,
                LECount = LECount,
                TUCount = TUCount,
                FAIScore = score,
                FAIGrade = grade,
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
            var (score, grade) = GetIndexAndGrade();

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
            EditModel.CollectionTime = CollectionTime;
            EditModel.CollectionTool = CollectionTool;
            EditModel.CollectionFlowState = CollectionFlowState;
            EditModel.AutoRiverChasu = AutoRiverChasu;
            EditModel.WatershedType = WatershedType;
            EditModel.DrainageSystemType = DrainageSystemType;
            EditModel.RiverWidth = RiverWidth;
            EditModel.YuWidth = YuWidth;
            EditModel.Depth = Depth;
            EditModel.Submerged = Submerged;
            EditModel.Floating = Floating;
            EditModel.Emergent = Emergent;
            EditModel.LeftVacantLand = LeftVacantLand;
            EditModel.LeftH = LeftH;
            EditModel.LeftS = LeftS;
            EditModel.LeftT1 = LeftT1;
            EditModel.RightVacantLand = RightVacantLand;
            EditModel.RightH = RightH;
            EditModel.RightS = RightS;
            EditModel.RightT1 = RightT1;
            EditModel.LeveeLeft = LeveeLeft;
            EditModel.LeveeRight = LeveeRight;
            EditModel.Confounder = Confounder;
            EditModel.Structure = Structure;
            EditModel.Fishway = Fishway;
            EditModel.EtcNote = EtcNote;
            EditModel.Bedrock = Bedrock;
            EditModel.Concrete = Concrete;
            EditModel.Mud = Mud;
            EditModel.Sand = Sand;
            EditModel.FineGravel = FineGravel;
            EditModel.Gravel = Gravel;
            EditModel.SmallStone = SmallStone;
            EditModel.BigStone = BigStone;
            EditModel.HabitatRiverType = HabitatRiverType;
            EditModel.HabitatFlowState = HabitatFlowState;
            EditModel.SurveyUnavailableReason = SurveyUnavailableReason;
            EditModel.Note = Note;
            EditModel.DECount = DECount;
            EditModel.EFCount = EFCount;
            EditModel.LECount = LECount;
            EditModel.TUCount = TUCount;
            EditModel.FAIScore = score;
            EditModel.FAIGrade = grade;

            return true;
        }
        catch { return false; }
    }

    //신규, 수정 세팅
    private void SettingProperties(Fish model)
    {
        if (model.FishId > 0)
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
    private void EditModelToProperty(Fish model)
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
        CollectionTime = EditModel.CollectionTime;
        CollectionTool = EditModel.CollectionTool;
        CollectionFlowState = EditModel.CollectionFlowState;
        AutoRiverChasu = EditModel.AutoRiverChasu;
        WatershedType = EditModel.WatershedType;
        DrainageSystemType = EditModel.DrainageSystemType;
        RiverWidth = EditModel.RiverWidth;
        YuWidth = EditModel.YuWidth;
        Depth = EditModel.Depth;
        Submerged = EditModel.Submerged;
        Floating = EditModel.Floating;
        Emergent = EditModel.Emergent;
        LeftVacantLand = EditModel.LeftVacantLand;
        LeftH = EditModel.LeftH;
        LeftS = EditModel.LeftS;
        LeftT1 = EditModel.LeftT1;
        RightVacantLand = EditModel.RightVacantLand;
        RightH = EditModel.RightH;
        RightS = EditModel.RightS;
        RightT1 = EditModel.RightT1;
        LeveeLeft = EditModel.LeveeLeft;
        LeveeRight = EditModel.LeveeRight;
        Confounder = EditModel.Confounder;
        Structure = EditModel.Structure;
        Fishway = EditModel.Fishway;
        EtcNote = EditModel.EtcNote;
        Bedrock = EditModel.Bedrock;
        Concrete = EditModel.Concrete;
        Mud = EditModel.Mud;
        Sand = EditModel.Sand;
        FineGravel = EditModel.FineGravel;
        Gravel = EditModel.Gravel;
        SmallStone = EditModel.SmallStone;
        BigStone = EditModel.BigStone;
        HabitatRiverType = EditModel.HabitatRiverType;
        HabitatFlowState = EditModel.HabitatFlowState;
        SurveyUnavailableReason = EditModel.SurveyUnavailableReason;
        Note = EditModel.Note;
        DECount = EditModel.DECount;
        EFCount = EditModel.EFCount;
        LECount = EditModel.LECount;
        TUCount = EditModel.TUCount;
        FAIScore = EditModel.FAIScore;
        FAIGrade = EditModel.FAIGrade;

        foreach (var sub in EditModel.Subs.ToList())
        {
            var speciesKoEn = $"{sub.SpeciesKo} {sub.SpeciesEn}";
            foreach (var item in AddSpeciesColleciton)
            {
                if (item.SearchText == speciesKoEn)
                {
                    item.IndividualCount = sub.IndividualCount;
                    break;
                }
            }
        }

        AddSpeciesSelectedItem = null;
        AddSpeciesCount = AddSpeciesColleciton.Count(m => m.IndividualCount > 0);
        AddIndividualCountSum = AddSpeciesColleciton.Sum(m => m.IndividualCount) ?? 0;
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
        CollectionTime = null;
        CollectionTool = null;
        CollectionFlowState = null;
        AutoRiverChasu = null;
        WatershedType = null;
        DrainageSystemType = null;
        RiverWidth = null;
        YuWidth = null;
        Depth = null;
        Submerged = null;
        Floating = null;
        Emergent = null;
        LeftVacantLand = null;
        LeftH = null;
        LeftS = null;
        LeftT1 = null;
        RightVacantLand = null;
        RightH = null;
        RightS = null;
        RightT1 = null;
        LeveeLeft = null;
        LeveeRight = null;
        Confounder = null;
        Structure = null;
        Fishway = null;
        EtcNote = null;
        Bedrock = null;
        Concrete = null;
        Mud = null;
        Sand = null;
        FineGravel = null;
        Gravel = null;
        SmallStone = null;
        BigStone = null;
        HabitatRiverType = null;
        HabitatFlowState = null;
        SurveyUnavailableReason = null;
        Note = null;
        DECount = null;
        EFCount = null;
        LECount = null;
        TUCount = null;
        FAIScore = null;
        FAIGrade = null;

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

            AddSpeciesColleciton.Clear();

            //종목록
            AddSpeciesColleciton.AddRange(await context.FishSpeciesLists.ToListAsync());
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetSpeciesData), ex.Message);
        }
    }

    #endregion 

    #region Messages

    private void ModelRecevice(ModelMessage<Fish> message)
    {
        SettingProperties(message.Model);
    }

    #endregion

    #region 점수 및 등급

    private (double?, string?) GetIndexAndGrade()
    {
        try
        {
            //종목록
            var speciesData = AddSpeciesColleciton.Where(m => m.IndividualCount > 0).Select(m => new ImportFishSpecies
            {
                FishTrait = m.FishTrait,
                ToleranceGuild = m.ToleranceGuild,
                FeedingGuild = m.FeedingGuild,
                HabitatGuild = m.HabitatGuild,
                Exotic = m.Exotic,
                Endemic = m.Endemic,
                Endangered1 = m.Endangered1,
                Endangered2 = m.Endangered2,
                NaturalMonument = m.NaturalMonument,
                LineageOrder = m.LineageOrder.ToString(),
                ClassKo = m.ClassKo,
                OrderKo = m.OrderKo,
                FamilyKo = m.FamilyKo,
                SpeciesKo = m.SpeciesKo,
                SpeciesEn = m.SpeciesEn,
                IndividualCount = m.IndividualCount,
            }).ToList();


            var surveyUnavailableReason = SurveyUnavailableReason;
            int chasu = int.TryParse(AutoRiverChasu.ToString(), out int cs) ? cs : 0;

            int abnormalCount = (int.TryParse(DECount.ToString(), out int de) ? de : 0)
                + (int.TryParse(EFCount.ToString(), out int ef) ? ef : 0)
                + (int.TryParse(LECount.ToString(), out int le) ? le : 0)
                + (int.TryParse(TUCount.ToString(), out int tu) ? tu : 0);

            var (score, grade) = GB.GetFAI(speciesData, surveyUnavailableReason, abnormalCount, chasu);

            return (score, grade);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetIndexAndGrade), ex.Message);
            return (null, null);
        }
    }

    #endregion
}
