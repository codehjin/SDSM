using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SDSM_App.Commons;
using SDSM_App.Enums;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.Models.Repositories.Interfases;
using SDSM_Models;
using System.Data.SQLite;
using System.Diagnostics;
using System.Media;
using System.Windows;
using Telerik.Windows.Data;

namespace SDSM_App.ViewModels;

public partial class VegetationENVEditorControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public VegetationENVEditorControlViewModel()
    {
        //수정모델 받기
        WeakReferenceMessenger.Default.Register<ModelMessage<VegetationENV>>(this, (r, m) =>
        {
            ModelRecevice(m);
        });
    }

    #endregion

    #region 속성

    [ObservableProperty] private string _formTitle = "신규";
    [ObservableProperty] private EditStatusType _editStatus = EditStatusType.Add;

    [ObservableProperty] private VegetationENV _editModel = new();

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private int? _surveyYear;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _chsu = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private DateTime? _surveyDate;
    [ObservableProperty] private string? _weather;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))] private string _site = string.Empty;
    [ObservableProperty] private string? _river;
    [ObservableProperty] private string? _surveyor;
    [ObservableProperty] private double? _startLat;
    [ObservableProperty] private double? _startLng;
    [ObservableProperty] private string? _startAddress;
    [ObservableProperty] private double? _endLat;
    [ObservableProperty] private double? _endLng;
    [ObservableProperty] private double? _startAzimuth;
    [ObservableProperty] private string? _rLSubstrateStructure;
    [ObservableProperty] private string? _rLDisturbance;
    [ObservableProperty] private string? _iLLeftLandUse;
    [ObservableProperty] private string? _iLRightLandUse;
    [ObservableProperty] private double? _distance;
    [ObservableProperty] private double? _slope;
    [ObservableProperty] private double? _depthWater;
    [ObservableProperty] private string? _hashingMaterial;
    [ObservableProperty] private string? _cluster;
    [ObservableProperty] private double? _t1Height;
    [ObservableProperty] private double? _t1CoverDegree;
    [ObservableProperty] private double? _t2Height;
    [ObservableProperty] private double? _t2CoverDegree;
    [ObservableProperty] private double? _sHeight;
    [ObservableProperty] private double? _sCoverDegree;
    [ObservableProperty] private double? _hHeight;
    [ObservableProperty] private double? _hCoverDegree;

    #endregion

    #region 생물종 속성, ICommand

    [ObservableProperty] private RadObservableCollection<VegetationENVSpecies> _speciesCollection = [];
    [ObservableProperty] 
    private VegetationENVSpecies? _speciesSelectedItem;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SpeciesAddSaveExecutedCommand))]
    private string _speciesLayer = string.Empty;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SpeciesAddSaveExecutedCommand))]
    private string _speciesSpeciesKo = string.Empty;
    
    [ObservableProperty] private double? _speciesCoverDegree;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SpeciesEditSaveExecutedCommand))]
    private string _editSpeciesLayer = string.Empty;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SpeciesEditSaveExecutedCommand))]
    private string _editSpeciesSpeciesKo = string.Empty;
    
    [ObservableProperty] private double? _editSpeciesCoverDegree;

    [ObservableProperty] private Visibility _speciesEditorVisibility = Visibility.Collapsed;

    [RelayCommand]
    private void SpeciesEditExecuted(object obj)
    {
        if(obj != null && obj is VegetationENVSpecies model)
        {
            SpeciesSelectedItem = model;

            EditSpeciesLayer = SpeciesSelectedItem.Layer;
            EditSpeciesSpeciesKo = SpeciesSelectedItem.SpeciesKo;
            EditSpeciesCoverDegree = SpeciesSelectedItem.CoverDegree;

            SpeciesEditorVisibility = Visibility.Visible;
        }
    }

    [RelayCommand]
    private void SpeciesRemoveExecuted(object obj)
    {
        if (obj != null && obj is VegetationENVSpecies model)
        {
            SpeciesSelectedItem = model;

            var message = $"계층 : {SpeciesSelectedItem.Layer}{Environment.NewLine}" +
                           $"종명 : {SpeciesSelectedItem.SpeciesKo}{Environment.NewLine}{Environment.NewLine}" +
                           $"출현종 목록에서 제거됩니다.{Environment.NewLine}" +
                           $"제거하시겠습니까?{Environment.NewLine}";

            SystemSounds.Beep.Play();
            var result = MessageBox.Show(message, "출현종 제거", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if(result == MessageBoxResult.Yes)
            {
                SpeciesCollection.Remove(SpeciesSelectedItem);
            }
        }
    }

    [RelayCommand]
    private void SpeciesEditorCollapsedExecuted()
    {
        SpeciesEditorVisibility = Visibility.Collapsed;

        EditSpeciesLayer = "";
        EditSpeciesSpeciesKo = "";
        EditSpeciesCoverDegree = null;
    }

    private bool CanSpeciesEditSave()
    {
        return !string.IsNullOrWhiteSpace(EditSpeciesLayer)
            && !string.IsNullOrWhiteSpace(EditSpeciesSpeciesKo);
    }

    [RelayCommand(CanExecute =nameof(CanSpeciesEditSave))]
    private void SpeciesEditSaveExecuted()
    {
        // 사용유무만
    }

    private bool CanSpeciesAddSave()
    {
        return !string.IsNullOrWhiteSpace(SpeciesLayer)
            && !string.IsNullOrWhiteSpace(SpeciesSpeciesKo);
    }

    [RelayCommand(CanExecute = nameof(CanSpeciesAddSave))]
    private void SpeciesAddSaveExecuted()
    {
       // 사용유무만
    }

    #endregion

    #region ICommand 구현

    private bool CanSave()
    {
        return SurveyYear != null
            && !string.IsNullOrWhiteSpace(Chsu)
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
            var repository = App.Current.Services.GetRequiredService<IVegetationENVRepository>();
            var addModel = await repository.InsertAsync(model);
            if (addModel != null)
            {
                var speciess = SpeciesCollection.ToList();
                if (speciess.Count != 0)
                {
                    await Task.Run(() => SubBatchSave(addModel.VegetationENVId, speciess, true));
                }

                var toast = (GB.SaveMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));
                ClearProperty();

                WeakReferenceMessenger.Default.Send(new EditorMessage<VegetationENV>(addModel, EditStatusType.Add));
            }
        }
    }

    //수정저장
    private async void EditSave()
    {
        if (IsEditItemToModel())
        {
            var repository = App.Current.Services.GetRequiredService<IVegetationENVRepository>();
            var isEdit = await repository.UpdateAsync(EditModel);
            if (isEdit)
            {
                var speciess = SpeciesCollection.ToList();

                await Task.Run(() => SubBatchSave(EditModel.VegetationENVId, speciess, false));

                var toast = (GB.EditMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

                WeakReferenceMessenger.Default.Send(new EditorMessage<VegetationENV>(EditModel, EditStatusType.Edit));
            }
        }
    }

    //종목록 일괄저장
    private async void SubBatchSave(int parentId, List<VegetationENVSpecies> speciess, bool isAdd)
    {
        try
        {
            if (isAdd == false)
            {
                //종목록 삭제 후 다시 추가
                var itemsDelete = new ItemsDelete<VegetationENVSpecies>();
                bool isSuccess = await Task.Run(() => itemsDelete.DeleteChild($"{nameof(VegetationENV)}Id", parentId));

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
                string strSql = $"INSERT INTO VegetationENVSpecies" +
                                 $"(Layer, SpeciesKo, CoverDegree, VegetationENVId)  " +
                                 $"VALUES " +
                                 $"(@Layer, @SpeciesKo, @CoverDegree, @VegetationENVId)";

                using var command = new SQLiteCommand(strSql, conn);

                command.Parameters.AddWithValue("@Layer", model.Layer);
                command.Parameters.AddWithValue("@SpeciesKo", model.SpeciesKo);
                command.Parameters.AddWithValue("@CoverDegree", model.CoverDegree ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@VegetationENVId", parentId);

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
    private VegetationENV? AddItemToModel()
    {
        try
        {
            if(SurveyYear is not int year)
                return null;

            var model = new VegetationENV
            {
                SurveyYear = year,
                Chsu = Chsu,
                SurveyDate = SurveyDate,
                Weather = Weather,
                Site = Site,
                River = River,
                Surveyor = Surveyor,
                StartLat = StartLat,
                StartLng = StartLng,
                StartAddress = StartAddress,
                EndLat = EndLat,
                EndLng = EndLng,
                StartAzimuth = StartAzimuth,
                RLSubstrateStructure = RLSubstrateStructure,
                RLDisturbance = RLDisturbance,
                ILLeftLandUse = ILLeftLandUse,
                ILRightLandUse = ILRightLandUse,
                Distance = Distance,
                Slope = Slope,
                DepthWater = DepthWater,
                HashingMaterial = HashingMaterial,
                Cluster = Cluster,
                T1Height = T1Height,
                T1CoverDegree = T1CoverDegree,
                T2Height = T2Height,
                T2CoverDegree = T2CoverDegree,
                SHeight = SHeight,
                SCoverDegree = SCoverDegree,
                HHeight = HHeight,
                HCoverDegree = HCoverDegree,                
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
            if (SurveyYear is not int year)
                return false;

            EditModel.SurveyYear = year;
            EditModel.Chsu = Chsu;
            EditModel.SurveyDate = SurveyDate;
            EditModel.Weather = Weather;
            EditModel.Site = Site;
            EditModel.River = River;
            EditModel.Surveyor = Surveyor;
            EditModel.StartLat = StartLat;
            EditModel.StartLng = StartLng;
            EditModel.StartAddress = StartAddress;
            EditModel.EndLat = EndLat;
            EditModel.EndLng = EndLng;
            EditModel.StartAzimuth = StartAzimuth;
            EditModel.RLSubstrateStructure = RLSubstrateStructure;
            EditModel.RLDisturbance = RLDisturbance;
            EditModel.ILLeftLandUse = ILLeftLandUse;
            EditModel.ILRightLandUse = ILRightLandUse;
            EditModel.Distance = Distance;
            EditModel.Slope = Slope;
            EditModel.DepthWater = DepthWater;
            EditModel.HashingMaterial = HashingMaterial;
            EditModel.Cluster = Cluster;
            EditModel.T1Height = T1Height;
            EditModel.T1CoverDegree = T1CoverDegree;
            EditModel.T2Height = T2Height;
            EditModel.T2CoverDegree = T2CoverDegree;
            EditModel.SHeight = SHeight;
            EditModel.SCoverDegree = SCoverDegree;
            EditModel.HHeight = HHeight;
            EditModel.HCoverDegree = HCoverDegree;

            return true;
        }
        catch { return false; }
    }

    //신규, 수정 세팅
    private void SettingProperties(VegetationENV model)
    {
        if (model.VegetationENVId > 0)
        {
            EditModelToProperty(model);
        }
        else
        {
            ClearProperty();
        }

    }
    //목록에서 선택한 항목값
    private void EditModelToProperty(VegetationENV model)
    {
        EditModel = model;

        FormTitle = "수정";
        EditStatus = EditStatusType.Edit;

        SurveyYear = EditModel.SurveyYear;
        Chsu = EditModel.Chsu;
        SurveyDate = EditModel.SurveyDate;
        Weather = EditModel.Weather;
        Site = EditModel.Site;
        River = EditModel.River;
        Surveyor = EditModel.Surveyor;
        StartLat = EditModel.StartLat;
        StartLng = EditModel.StartLng;
        StartAddress = EditModel.StartAddress;
        EndLat = EditModel.EndLat;
        EndLng = EditModel.EndLng;
        StartAzimuth = EditModel.StartAzimuth;
        RLSubstrateStructure = EditModel.RLSubstrateStructure;
        RLDisturbance = EditModel.RLDisturbance;
        ILLeftLandUse = EditModel.ILLeftLandUse;
        ILRightLandUse = EditModel.ILRightLandUse;
        Distance = EditModel.Distance;
        Slope = EditModel.Slope;
        DepthWater = EditModel.DepthWater;
        HashingMaterial = EditModel.HashingMaterial;
        Cluster = EditModel.Cluster;
        T1Height = EditModel.T1Height;
        T1CoverDegree = EditModel.T1CoverDegree;
        T2Height = EditModel.T2Height;
        T2CoverDegree = EditModel.T2CoverDegree;
        SHeight = EditModel.SHeight;
        SCoverDegree = EditModel.SCoverDegree;
        HHeight = EditModel.HHeight;
        HCoverDegree = EditModel.HCoverDegree;

        ClearSpecies();
        SpeciesCollection.AddRange(EditModel.Subs);
    }

    //항목초기화
    private void ClearProperty()
    {
        FormTitle = "신규";
        EditStatus = EditStatusType.Add;
        EditModel = new();

        SurveyYear = null;
        Chsu = string.Empty;
        SurveyDate = null;
        Weather = null;
        Site = string.Empty;
        River = null;
        Surveyor = null;
        StartLat = null;
        StartLng = null;
        StartAddress = null;
        EndLat = null;
        EndLng = null;
        StartAzimuth = null;
        RLSubstrateStructure = null;
        RLDisturbance = null;
        ILLeftLandUse = null;
        ILRightLandUse = null;
        Distance = null;
        Slope = null;
        DepthWater = null;
        HashingMaterial = null;
        Cluster = null;
        T1Height = null;
        T1CoverDegree = null;
        T2Height = null;
        T2CoverDegree = null;
        SHeight = null;
        SCoverDegree = null;
        HHeight = null;
        HCoverDegree = null;

        ClearSpecies();
    }

    private void ClearSpecies()
    {
        if (SpeciesCollection.Count > 0)
            SpeciesCollection.Clear();

        SpeciesSelectedItem = null;

        SpeciesLayer = string.Empty;
        SpeciesSpeciesKo = string.Empty;
        SpeciesCoverDegree = null;

        EditSpeciesLayer = string.Empty;
        EditSpeciesSpeciesKo = string.Empty;
        EditSpeciesCoverDegree = null;

        SpeciesEditorVisibility = Visibility.Collapsed;
    }

    #endregion 

    #region Messages

    private void ModelRecevice(ModelMessage<VegetationENV> message)
    {
        SettingProperties(message.Model);
    }

    #endregion
}
