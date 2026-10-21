using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SDSM_App.Commons;
using SDSM_App.Enums;
using SDSM_App.Helpers;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.Models;
using SDSM_App.Models.Repositories.Interfases;
using SDSM_Models;
using System.Data.SQLite;
using System.Windows;
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
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _project = "방류하천";

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _yearChsu = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string? _site;
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
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private int? _autoRiverChasu;
    
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
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string? _surveyUnavailableReason;
    [ObservableProperty] private string? _note;
    [ObservableProperty] private int? _dECount;
    [ObservableProperty] private int? _eFCount;
    [ObservableProperty] private int? _lECount;
    [ObservableProperty] private int? _tUCount;
    [ObservableProperty] private int? _totalIndividualCount;
    [ObservableProperty] private int? _totalSpeciesCount;
    [ObservableProperty] private int? _m1Value;
    [ObservableProperty] private int? _m2Value;
    [ObservableProperty] private int? _m3Value;
    [ObservableProperty] private double? _m4Value;
    [ObservableProperty] private double? _m5Value;
    [ObservableProperty] private double? _m6Value;
    [ObservableProperty] private int? _m7Value;
    [ObservableProperty] private double? _fAIScore;
    [ObservableProperty] private string? _fAIGrade;
    [ObservableProperty] private int? _siteDivisionId;

    #endregion

    #region 생물종 속성, ICommand

    [ObservableProperty] private RadObservableCollection<FishSpeciesList> _addSpeciesColleciton = [];
    [ObservableProperty] private FishSpeciesList? _addSpeciesSelectedItem;

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private int _addSpeciesCount;
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
            && SurveyDate != null
            && (AutoRiverChasu != null || !string.IsNullOrWhiteSpace(SurveyUnavailableReason) || AddSpeciesCount == 0)   // 조사불가·0종이면 하천차수 없이(2026-10-21 문제 7)
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

            //글자 칸의 빈 값은 일괄등록처럼 NULL(종목록에는 빈 글자로 들어 있다) · 2026-10-20 (3) 결정 6
            static object TextOrNull(string? v) => string.IsNullOrWhiteSpace(v) ? DBNull.Value : v;

            foreach (var model in speciess)
            {
                string strSql = $"INSERT INTO FishSpecies" +
                                 $"(FishTrait, ToleranceGuild, FeedingGuild, HabitatGuild, Exotic, Endemic, Endangered1, Endangered2, NaturalMonument, LineageOrder, " +
                                 $"ClassKo, OrderKo, FamilyKo, SpeciesKo, SpeciesEn, IndividualCount, FishId)  " +
                                 $"VALUES " +
                                 $"(@FishTrait, @ToleranceGuild, @FeedingGuild, @HabitatGuild, @Exotic, @Endemic, @Endangered1, @Endangered2, @NaturalMonument, @LineageOrder, " +
                                 $"@ClassKo, @OrderKo, @FamilyKo, @SpeciesKo, @SpeciesEn, @IndividualCount, @FishId) ";

                using var command = new SQLiteCommand(strSql, conn);

                command.Parameters.AddWithValue("@FishTrait", TextOrNull(model.FishTrait));
                command.Parameters.AddWithValue("@ToleranceGuild", TextOrNull(model.ToleranceGuild));
                command.Parameters.AddWithValue("@FeedingGuild", TextOrNull(model.FeedingGuild));
                command.Parameters.AddWithValue("@HabitatGuild", TextOrNull(model.HabitatGuild));
                command.Parameters.AddWithValue("@Exotic", TextOrNull(model.Exotic));
                command.Parameters.AddWithValue("@Endemic", TextOrNull(model.Endemic));
                command.Parameters.AddWithValue("@Endangered1", TextOrNull(model.Endangered1));
                command.Parameters.AddWithValue("@Endangered2", TextOrNull(model.Endangered2));
                command.Parameters.AddWithValue("@NaturalMonument", TextOrNull(model.NaturalMonument));
                command.Parameters.AddWithValue("@LineageOrder", model.LineageOrder ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ClassKo", TextOrNull(model.ClassKo));
                command.Parameters.AddWithValue("@OrderKo", TextOrNull(model.OrderKo));
                command.Parameters.AddWithValue("@FamilyKo", TextOrNull(model.FamilyKo));
                command.Parameters.AddWithValue("@SpeciesKo", TextOrNull(model.SpeciesKo));
                command.Parameters.AddWithValue("@SpeciesEn", TextOrNull(model.SpeciesEn));
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
            var (cluster, score, grade) = GetIndexAndGrade();

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
                //총 개체수·총 종수는 일괄등록처럼 FAI 계산 결과로(조사불가면 NULL) — 2026-10-21 §11-5
                TotalIndividualCount = cluster?.TotalIndividualCount,
                TotalSpeciesCount = cluster?.TotalSpeciesCount,
                M1Value = cluster?.M1Value,
                M2Value = cluster?.M2Value,
                M3Value = cluster?.M3Value,
                M4Value = cluster?.M4Value,
                M5Value = cluster?.M5Value,
                M6Value = cluster?.M6Value,
                M7Value = cluster?.M7Value,
                FAIScore = score,
                FAIGrade = grade,
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
            var (cluster, score, grade) = GetIndexAndGrade();

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
            //총 개체수·총 종수는 일괄등록처럼 FAI 계산 결과로(조사불가면 NULL) — 열 때 값을 그대로 쓰면 개체수를 바꿔도 옛 값이 남았다 · 2026-10-21 §11-5
            EditModel.TotalIndividualCount = cluster?.TotalIndividualCount;
            EditModel.TotalSpeciesCount = cluster?.TotalSpeciesCount;
            EditModel.M1Value = cluster?.M1Value;
            EditModel.M2Value = cluster?.M2Value;
            EditModel.M3Value = cluster?.M3Value;
            EditModel.M4Value = cluster?.M4Value;
            EditModel.M5Value = cluster?.M5Value;
            EditModel.M6Value = cluster?.M6Value;
            EditModel.M7Value = cluster?.M7Value;
            EditModel.FAIScore = score;
            EditModel.FAIGrade = grade;
            EditModel.SiteDivisionId = SiteDivisionId;

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
        CollectionTime = EditModel.CollectionTime;
        CollectionTool = EditModel.CollectionTool;
        CollectionFlowState = EditModel.CollectionFlowState;
        AutoRiverChasu = EditModel.AutoRiverChasu;
        
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
        TotalIndividualCount = EditModel.TotalIndividualCount;
        TotalSpeciesCount = EditModel.TotalSpeciesCount;
        M1Value = EditModel.M1Value;
        M2Value = EditModel.M2Value;
        M3Value = EditModel.M3Value;
        M4Value = EditModel.M4Value;
        M5Value = EditModel.M5Value;
        M6Value = EditModel.M6Value;
        M7Value = EditModel.M7Value;
        FAIScore = EditModel.FAIScore;
        FAIGrade = EditModel.FAIGrade;
        SiteDivisionId = EditModel.SiteDivisionId;

        foreach (var sub in EditModel.Subs.ToList())
        {
            var speciesKoEn = $"{sub.SpeciesKo} {sub.SpeciesEn}";
            bool isListed = false;
            foreach (var item in AddSpeciesColleciton)
            {
                if (item.SearchText == speciesKoEn)
                {
                    item.IndividualCount = sub.IndividualCount;
                    isListed = true;
                    break;
                }
            }

            //종목록에 없는 종 행(일괄등록으로 들어온 목록 밖 종)은 그 행 값 그대로 목록 끝에 붙인다
            //→ 화면에 보이고 출현종 수·FAI·[저장]에 그대로 들어간다(지워지지 않게) · 2026-10-20 (3) 결정 6
            if (!isListed)
            {
                AddSpeciesColleciton.Add(new FishSpeciesList
                {
                    FishTrait = sub.FishTrait,
                    ToleranceGuild = sub.ToleranceGuild,
                    FeedingGuild = sub.FeedingGuild,
                    HabitatGuild = sub.HabitatGuild,
                    Exotic = sub.Exotic,
                    Endemic = sub.Endemic,
                    Endangered1 = sub.Endangered1,
                    Endangered2 = sub.Endangered2,
                    NaturalMonument = sub.NaturalMonument,
                    LineageOrder = sub.LineageOrder,
                    ClassKo = sub.ClassKo,
                    OrderKo = sub.OrderKo,
                    FamilyKo = sub.FamilyKo,
                    SpeciesKo = sub.SpeciesKo,
                    SpeciesEn = sub.SpeciesEn,
                    IndividualCount = sub.IndividualCount,
                });
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
        CollectionTime = null;
        CollectionTool = null;
        CollectionFlowState = null;
        AutoRiverChasu = null;
        
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
        TotalIndividualCount = null;
        TotalSpeciesCount = null;
        M1Value = null;
        M2Value = null;
        M3Value = null;
        M4Value = null;
        M5Value = null;
        M6Value = null;
        M7Value = null;
        FAIScore = null;
        FAIGrade = null;
        SiteDivisionId = null;

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

    private (ClusterModel?, double?, string?) GetIndexAndGrade()
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

            var (cluster, score, grade) = GB.GetFAI(speciesData, surveyUnavailableReason, abnormalCount, chasu);

            return (cluster, score, grade);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetIndexAndGrade), ex.Message);
            return (null, null, null);
        }
    }

    #endregion
}
