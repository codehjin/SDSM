using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using NinjaNye.SearchExtensions;
using SDSM_App.Commons;
using SDSM_App.Enums;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.Models;
using SDSM_Models;
using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace SDSM_App.ViewModels;

/// <summary>
/// 조류 조사
/// </summary>
public partial class BirdPageViewModel : ObservableObject, ITransientService
{
    #region 필드

    private CancellationTokenSource? _cts;

    #endregion

    #region 생성자

    public BirdPageViewModel()
    {
        //작성폼 결과
        WeakReferenceMessenger.Default.Register<EditorMessage<Bird>>(this, (r, m) =>
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

    [ObservableProperty] List<Bird> _modelCollection = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditEditorOpenExecutedCommand))]
    Bird? _modelSelectedItem;

    [ObservableProperty] private Visibility _editorVisibility = Visibility.Collapsed;

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
        WeakReferenceMessenger.Default.Send(new ModelMessage<Bird>(new Bird()));
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
            var selectedModel = context.Birds.Where(m => m.BirdId == ModelSelectedItem.BirdId).FirstOrDefault();

            if (selectedModel != null)
            {
                WeakReferenceMessenger.Default.Send(new ModelMessage<Bird>(selectedModel));
                EditorVisibility = Visibility.Visible;
            }
        }
    }

    #endregion

    #region 삭제

    [ObservableProperty] private Visibility _deleteFormVisibility = Visibility.Collapsed;

    [ObservableProperty] List<Bird>? _deleteData;

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
        if (obj != null && obj is List<Bird> deleteData && deleteData.Count > 0)
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
            GB.IsBusy = true;
            GB.BusyContent = "자료 삭제 중...";

            //파일삭제
            var itemsDelete = new ItemsDelete<Bird>();
            bool isSuccess = await Task.Run(() => itemsDelete.Delete(DeleteData));

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
            IQueryable<Bird> query;

            await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(QueryText))
                {
                    query = context.Birds;
                }
                else
                {
                    QueryableStringSearch<Bird> searchQuery =
                         context.Birds.Search(m => m.YearChsu,
                                                 m => m.SurveyDate.ToString(),
                                                 m => m.Weather,
                                                 m => m.River,
                                                 m => m.Site,
                                                 m => m.SpeciesKo,
                                                 m => m.SpeciesEn,
                                                 m => m.Feature,
                                                 m => m.Lat.ToString(),
                                                 m => m.Lng.ToString(),
                                                 m => m.IndividualCount.ToString(),
                                                 m => m.MigratoryType,
                                                 m => m.Category,
                                                 m => m.CategoryDetail,
                                                 m => m.HabitatType,
                                                 m => m.HabitatType,
                                                 m => m.Surveyor,
                                                 m => m.Note
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

                ModelCollection = [.. query.OrderByDescending(m => m.BirdId).Skip(index * PageSize).Take(PageSize)];

            }, _cts.Token);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetModels), ex.Message);
        }
        finally
        {
            if (_cts != null)
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
            int count = await context.Birds.CountAsync();

            if (count != 0) return;

            string tableName = nameof(Bird);

            await SqliteSequenceReset.SqliteSequenceResetAsync(tableName);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(SqliteSequenceResetRun), ex.Message);
        }
    }

    #endregion

    #region Messages

    //현재모델 작성폼: 수정상태인 경우 작성폼 닫은 후 다시 불러오기
    private void EditorRecevice(EditorMessage<Bird> message)
    {
        if (message.EditStatus == EditStatusType.Edit)
        {
            EditorVisibility = Visibility.Collapsed;
        }

        GetModels();
    }

    #endregion
}
