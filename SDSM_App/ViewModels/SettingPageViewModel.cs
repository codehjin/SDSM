using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.Models;
using SDSM_App.Models.Repositories.Interfases;
using SDSM_Models;
using System.Data;
using System.Media;
using System.Windows;
using Telerik.Windows.Data;

namespace SDSM_App.ViewModels;

/// <summary>
/// 설정
/// </summary>
public partial class SettingPageViewModel : ObservableObject, ITransientService
{
    #region 필드

    #endregion

    #region 생성자

    public SettingPageViewModel()
    {
        InitialLoad();
    }

    #endregion

    #region 기준자료 내보내기

    // 조사자 앱 배포용 기준자료(reference.json) 내보내기
    [RelayCommand]
    private void ExportReference() => Commons.ReferenceDataExporter.Export();

    #endregion

    #region 프로젝트 사업장 지점명

    [ObservableProperty] private string _siteDivisionTitle = "신규";
    [ObservableProperty] private bool _isEdit;
    [ObservableProperty] private SiteDivision _siteDivisioneditModel = new();

    [ObservableProperty] private string _project = "방류하천";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SiteDivisionSaveExecutedCommand))]
    private string? _workplace;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SiteDivisionSaveExecutedCommand))]
    private string? _workplaceRiver;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SiteDivisionSaveExecutedCommand))]
    private string? _workplaceSite;

    [ObservableProperty]
    private RadObservableCollection<SiteDivision> _siteDivisionCollection = [];

    [ObservableProperty]
    private SiteDivision? _siteDivisionSelectedItem;

    private bool CanSiteDivisionSave()
    {
        return !string.IsNullOrWhiteSpace(Workplace)
            && !string.IsNullOrWhiteSpace(WorkplaceRiver)
            && !string.IsNullOrWhiteSpace(WorkplaceSite)
            && WorkplaceSite.Length > 1;
    }

    [RelayCommand(CanExecute = nameof(CanSiteDivisionSave))]
    private async Task SiteDivisionSaveExecuted()
    {
        using var context = new SDSMDbContext();
        var repository = App.Current.Services.GetRequiredService<ISiteDivisionRepository>();

        if (IsEdit == false)
        {
            //신규
            var newItem = new SiteDivision()
            {
                Project = Project,
                Workplace = Workplace,
                WorkplaceRiver = WorkplaceRiver,
                WorkplaceSite = WorkplaceSite
            };

            var existsItem = await context.SiteDivisions
                .FirstOrDefaultAsync(x => x.Workplace == newItem.Workplace
                && x.WorkplaceRiver == newItem.WorkplaceRiver
                && x.WorkplaceSite == newItem.WorkplaceSite);

            if (existsItem != null)
            {
                var message = "이미 존재하는 사업장-과업 대표하천-지점명입니다.";
                MessageBox.Show(message, "자료 중복", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var addModel = await repository.InsertAsync(newItem);
            if (addModel != null)
            {
                var toast = (GB.SaveMessage, true);
                WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

                SiteDivisionRestExecutedCommand.Execute(null);
                await GetSiteDivisionData();
            }
        }
        else
        {
            //수정

            if (SiteDivisioneditModel.Workplace != Workplace || SiteDivisioneditModel.WorkplaceRiver != WorkplaceRiver || SiteDivisioneditModel.WorkplaceSite != WorkplaceSite)
            {
                SystemSounds.Beep.Play();
                var message = $"사업장-과업 대표하천-지점명이 변경되었습니다.{Environment.NewLine}" +
                               $"수정 내용이 저장 후 모든자료의 관련 자료도 함께 변경됩니다.{Environment.NewLine}" +
                               $"계속 진행하시겠습니까?";
                var resut = MessageBox.Show(message, "자료 수정", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
                if (resut == MessageBoxResult.Cancel)
                    return;

                var existsItem = await context.SiteDivisions.Where(m => m.SiteDivisionId != SiteDivisioneditModel.SiteDivisionId)
                    .FirstOrDefaultAsync(x => x.Workplace == Workplace && x.WorkplaceRiver == WorkplaceRiver && x.WorkplaceSite == WorkplaceSite);

                if (existsItem != null)
                {
                    MessageBox.Show("이미 존재하는 사업장-과업 대표하천-지점명입니다.", "자료 중복", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SiteDivisioneditModel.Workplace = Workplace;
                SiteDivisioneditModel.WorkplaceRiver = WorkplaceRiver;
                SiteDivisioneditModel.WorkplaceSite = WorkplaceSite;

                var isEdit = await repository.UpdateAsync(SiteDivisioneditModel);
                if (isEdit)
                {
                    var toast = (GB.EditMessage, true);
                    WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

                    SiteDivisionRestExecutedCommand.Execute(null);
                    await GetSiteDivisionData();
                }
            }
        }
    }

    [RelayCommand]
    private void SiteDivisionEditSelectionExecuted(object obj)
    {
        if (obj != null && obj is SiteDivision seiectionModel)
        {
            SiteDivisionSelectedItem = seiectionModel;
            SiteDivisioneditModel = SiteDivisionSelectedItem;

            IsEdit = true;

            SiteDivisionTitle = $"수정 ({SiteDivisioneditModel.Workplace}-{SiteDivisioneditModel.WorkplaceRiver}-{SiteDivisioneditModel.WorkplaceSite})";
            Workplace = SiteDivisioneditModel.Workplace;
            WorkplaceRiver = SiteDivisioneditModel.WorkplaceRiver;
            WorkplaceSite = SiteDivisioneditModel.WorkplaceSite;
        }
    }

    [RelayCommand]
    private void SiteDivisionRestExecuted()
    {
        SiteDivisionTitle = "신규";
        SiteDivisioneditModel = new();
        IsEdit = false;
        Workplace = null;
        WorkplaceRiver = null;
        WorkplaceSite = null;
    }

    #endregion

    #region 매서드

    private async void InitialLoad()
    {
        if (GB.IsBusy == false)
            GB.IsBusy = true;

        await GetSiteDivisionData();

        if (GB.IsBusy == true)
            GB.IsBusy = false;
    }

    private async Task GetSiteDivisionData()
    {
        try
        {
            if (SiteDivisionCollection.Count != 0)
                SiteDivisionCollection.Clear();

            using var context = new SDSMDbContext();

            var data = await context.SiteDivisions.OrderBy(x => x.Workplace).ThenBy(m => m.WorkplaceRiver).ToListAsync();
            SiteDivisionCollection.AddRange(data.Where(d => d.Project != "생태현황"));

            //전역변수에 저장
            GB.ProjectSiteNames = data;

        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetSiteDivisionData), ex.Message);
        }
    }

    #endregion
}
