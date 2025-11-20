using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NinjaNye.SearchExtensions;
using SDSM_App.Commons;
using SDSM_App.Enums;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.Models;
using SDSM_App.Views.UserControls;
using SDSM_Models;
using System.Diagnostics;
using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace SDSM_App.ViewModels;

/// <summary>
/// 어류
/// </summary>
public partial class FishPageViewModel : ObservableObject, ITransientService
{
    #region 필드

    private CancellationTokenSource? _cts;

    #endregion

    #region 생성자

    public FishPageViewModel()
    {
        //작성폼 결과
        WeakReferenceMessenger.Default.Register<EditorMessage<Fish>>(this, (r, m) =>
        {
            EditorRecevice(m);
        });

        InitialLoad();
    }

    #endregion

    #region 속성

    public string QueryText { get; set; } = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;

    public SearchModeType SearchModeSelectedValue { get; set; } = SearchModeType.포함;

    //검색모드
    public EnumDataSource SearchModeSource
    {
        get { return new EnumDataSource { EnumType = typeof(SearchModeType) }; }
    }

    [ObservableProperty] List<FishView> _modelCollection = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditEditorOpenExecutedCommand))]
    FishView? _modelSelectedItem;

    [ObservableProperty] private Visibility _editorVisibility = Visibility.Collapsed;

    [ObservableProperty] private Visibility _speciesLayoutVisibility = Visibility.Collapsed;
    [ObservableProperty] private FishSpeciesControl? _speciesContent;

    #endregion

    #region 페이징

    [ObservableProperty] private int _modelTotal;
    [ObservableProperty] private int _pageIndex;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PageSizeMinExecutedCommand))]
    [NotifyCanExecuteChangedFor(nameof(PageSizeAvgExecutedCommand))]
    [NotifyCanExecuteChangedFor(nameof(PageSizeMaxExecutedCommand))]
    private int _pageSize = GB.CurrentPageSize;

    [RelayCommand]
    private void PageIndexChangedExecuted(object? obj)
    {
        if (_cts == null && obj != null && obj is PageIndexChangedEventArgs e)
        {
            if (GB.IsBusy == false)
                GB.IsBusy = true;

            try
            {
                GetModels(e.NewPageIndex);

                if (GB.IsBusy == true)
                    GB.IsBusy = false;
            }
            catch
            {
                if (GB.IsBusy == true)
                    GB.IsBusy = false;
            }
        }
    }

    [RelayCommand]
    private void PageSizeChangedExecuted(object? obj)
    {
        if (_cts == null && obj != null && obj is RadRangeBaseValueChangedEventArgs e)
        {
            if (int.TryParse(e.NewValue.ToString(), out int pageSize))
            {
                GB.CurrentPageSize = pageSize;

                if (GB.IsBusy == false)
                    GB.IsBusy = true;

                try
                {
                    GetModels();

                    if (GB.IsBusy == true)
                        GB.IsBusy = false;
                }
                catch
                {
                    if (GB.IsBusy == true)
                        GB.IsBusy = false;
                }
            }
        }
    }

    private bool CanPageSizeMin()
    {
        return PageSize > GB.MinPageSize;
    }

    [RelayCommand(CanExecute = nameof(CanPageSizeMin))]
    private void PageSizeMinExecuted()
    {
        GB.CurrentPageSize = GB.MinPageSize;
        PageSize = GB.MinPageSize;
    }

    private bool CanPageSizeAvg()
    {
        return PageSize != GB.AvgPageSize;
    }

    [RelayCommand(CanExecute = nameof(CanPageSizeAvg))]
    private void PageSizeAvgExecuted()
    {
        GB.CurrentPageSize = GB.AvgPageSize;
        PageSize = GB.AvgPageSize;
    }

    private bool CanPageSizeMax()
    {
        return PageSize < GB.MaxPageSize;
    }

    [RelayCommand(CanExecute = nameof(CanPageSizeMax))]
    private void PageSizeMaxExecuted()
    {
        GB.CurrentPageSize = GB.MaxPageSize;
        PageSize = GB.MaxPageSize;
    }

    #endregion

    #region ICommand 구현

    //출현종
    [RelayCommand]
    private void SpeceisOpenExecuted()
    {
        SpeciesContent = App.Current.Services.GetRequiredService<FishSpeciesControl>();
        SpeciesLayoutVisibility = Visibility.Visible;
    }

    //출현종
    [RelayCommand]
    private void SpeceisCloseExecuted()
    {
        SpeciesLayoutVisibility = Visibility.Collapsed;
        SpeciesContent = null;
    }

    //검색
    [RelayCommand]
    private void SearchExecuted(object? obj)
    {
        QueryText = SearchText;
        GetModels();
    }

    //신규
    [RelayCommand]
    private void AddEditorOpenExecuted()
    {
        WeakReferenceMessenger.Default.Send(new ModelMessage<Fish>(new Fish()));
        EditorVisibility = Visibility.Visible;
    }

    private bool CanEditEditorOpen() => ModelSelectedItem != null;

    //수정
    [RelayCommand(CanExecute = nameof(CanEditEditorOpen))]
    private void EditEditorOpenExecuted()
    {
        if (ModelSelectedItem != null)
        {
            using var context = new SDSMDbContext();
            var selectedModel = context.Fishs.Where(m => m.FishId == ModelSelectedItem.FishId).Include(m => m.Subs).FirstOrDefault();

            if (selectedModel != null)
            {
                WeakReferenceMessenger.Default.Send(new ModelMessage<Fish>(selectedModel));
                EditorVisibility = Visibility.Visible;
            }
        }
    }

    #endregion

    #region 삭제

    [ObservableProperty] private Visibility _deleteFormVisibility = Visibility.Collapsed;

    [ObservableProperty] List<FishView>? _deleteData;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteExecutedCommand))]
    private int? _deleteCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteExecutedCommand))]
    private int? _checkDeleteCount;

    //삭제폼 열기
    [RelayCommand]
    private void DeleteFormVisibleExecuted(object obj)
    {
        if (obj != null && obj is List<FishView> deleteData && deleteData.Count > 0)
        {
            DeleteData = deleteData;
            DeleteCount = deleteData.Count;
            DeleteFormVisibility = Visibility.Visible;
        }
    }

    //삭제창 닫기
    [RelayCommand]
    private void DeleteFormCloseExecuted()
    {
        if (DeleteFormVisibility == Visibility.Visible)
        {
            DeleteFormVisibility = Visibility.Collapsed;
        }
        DeleteCount = null;
        CheckDeleteCount = null;
        DeleteData = null;
    }

    private bool CanDelete()
    {
        return DeleteCount == CheckDeleteCount;
    }

    //삭제
    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteExecuted()
    {
        if (DeleteData != null && DeleteData.Count > 0)
        {
            foreach (var item in DeleteData)
            {
                Debug.WriteLine(item.YearChsu);
            }

            GB.IsBusy = true;
            GB.BusyContent = "자료 삭제 중...";

            using var context = new SDSMDbContext();

            var delteItems = new List<Fish>();

            //뷰 => 원본자료
            await Task.Run(() =>
            {
                var ids = context.Fishs.Select(m => new Fish { FishId = m.FishId }).ToList();
                delteItems = [.. ids.Where(m => DeleteData.Any(i => i.FishId == m.FishId))];
            });

            //삭제 파일
            var itemsDelete = new ItemsDelete<Fish>();
            bool isSuccess = await Task.Run(() => itemsDelete.Delete(delteItems));

            if (isSuccess)
            {
                var toast = (GB.DeleteMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

                DeleteFormCloseExecutedCommand.Execute(null);
                DeleteData = null;

                //자동생성 번호 초기화
                await SqliteSequenceResetRun();

                GetModels();
            }

            GB.IsBusy = false;
            GB.BusyContent = "";
        }
    }

    #endregion

    #region 매서드

    private void InitialLoad()
    {
        GetModels();
    }

    private async void GetModels(int index = 0)
    {
        if (GB.IsBusy == false)
            GB.IsBusy = true;

        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            PageIndex = index;

            using var context = new SDSMDbContext();
            IQueryable<FishView> query;

            await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(QueryText))
                {
                    query = context.FishViews;
                }
                else
                {
                    QueryableStringSearch<FishView> searchQuery =
                        context.FishViews.Search(m => m.YearChsu,
                                                  m => m.Site,
                                                  m => m.SurveyDate.ToString(),
                                                  m => m.MajorRegion,
                                                  m => m.MiddleRegion,
                                                  m => m.River,
                                                  m => m.RiverType,
                                                  m => m.Weather,
                                                  m => m.SurveyAgency,
                                                  m => m.Surveyor,
                                                  m => m.CollectionTime.ToString(),
                                                  m => m.CollectionTool,
                                                  m => m.CollectionFlowState,
                                                  m => m.AutoRiverChasu.ToString(),
                                                  m => m.WatershedType,
                                                  m => m.DrainageSystemType,
                                                  m => m.RiverWidth,
                                                  m => m.YuWidth,
                                                  m => m.Depth,
                                                  m => m.Submerged,
                                                  m => m.Floating,
                                                  m => m.Emergent,
                                                  m => m.LeftVacantLand.ToString(),
                                                  m => m.LeftH.ToString(),
                                                  m => m.LeftS.ToString(),
                                                  m => m.LeftT1.ToString(),
                                                  m => m.RightVacantLand.ToString(),
                                                  m => m.RightH.ToString(),
                                                  m => m.RightS.ToString(),
                                                  m => m.RightT1.ToString(),
                                                  m => m.LeveeLeft,
                                                  m => m.LeveeRight,
                                                  m => m.Confounder,
                                                  m => m.Structure,
                                                  m => m.Fishway,
                                                  m => m.EtcNote,
                                                  m => m.Bedrock.ToString(),
                                                  m => m.Concrete.ToString(),
                                                  m => m.Mud.ToString(),
                                                  m => m.Sand.ToString(),
                                                  m => m.FineGravel.ToString(),
                                                  m => m.Gravel.ToString(),
                                                  m => m.SmallStone.ToString(),
                                                  m => m.BigStone.ToString(),
                                                  m => m.HabitatRiverType,
                                                  m => m.HabitatFlowState,
                                                  m => m.SurveyUnavailableReason,
                                                  m => m.Note,
                                                  m => m.DECount.ToString(),
                                                  m => m.EFCount.ToString(),
                                                  m => m.LECount.ToString(),
                                                  m => m.TUCount.ToString(),
                                                  m => m.TotalIndividualCount.ToString(),
                                                  m => m.TotalSpeciesCount.ToString(),
                                                  m => m.M1Value.ToString(),
                                                  m => m.M2Value.ToString(),
                                                  m => m.M3Value.ToString(),
                                                  m => m.M4Value.ToString(),
                                                  m => m.M5Value.ToString(),
                                                  m => m.M6Value.ToString(),
                                                  m => m.M7Value.ToString(),
                                                  m => m.FAIGrade,
                                                  m => m.SpeciesCount.ToString(),
                                                  m => m.IndividualCount.ToString(),
                                                  m => m.Project,
                                                  m => m.Workplace,
                                                  m => m.WorkplaceRiver
                                                  );

                    if (SearchModeSelectedValue == SearchModeType.일치)
                    {
                        query = searchQuery.EqualTo(QueryText);
                    }
                    else if (SearchModeSelectedValue == SearchModeType.시작문자)
                    {
                        query = searchQuery.StartsWith(QueryText);
                    }
                    else if (SearchModeSelectedValue == SearchModeType.끝문자)
                    {
                        query = searchQuery.EndsWith(QueryText);
                    }
                    else
                    {
                        query = searchQuery.Containing(QueryText);
                    }
                }

                ModelTotal = query.Count();

                ModelCollection = [.. query.OrderByDescending(m => m.FishId).Skip(index * PageSize).Take(PageSize)];

            }, _cts.Token);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetModels), ex.Message);
        }
        finally
        {
            if(_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
           
            if (GB.IsBusy == true)
                GB.IsBusy = false;
        }
    }

    #endregion

    #region 삭제후 sqlite_sequence 초기화

    private async Task SqliteSequenceResetRun()
    {
        try
        {
            using var context = new SDSMDbContext();

            int count = await context.Fishs.CountAsync();
            if (count == 0)
            {
                string tableName = nameof(Fish);
                await SqliteSequenceReset.SqliteSequenceResetAsync(tableName);
            }

            int speciesCount = await context.FishSpeciess.CountAsync();
            if (speciesCount == 0)
            {
                string tableName = nameof(FishSpecies);
                await SqliteSequenceReset.SqliteSequenceResetAsync(tableName);
            }

        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(SqliteSequenceResetRun), ex.Message);
        }
    }

    #endregion

    #region Messages

    //현재모델 작성폼: 수정상태인 경우 작성폼 닫은 후 다시 불러오기
    private void EditorRecevice(EditorMessage<Fish> message)
    {
        if (message.EditStatus == EditStatusType.Edit)
        {
            EditorVisibility = Visibility.Collapsed;
        }

        GetModels();
    }

    #endregion
}
