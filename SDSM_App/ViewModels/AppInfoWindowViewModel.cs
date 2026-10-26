using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SDSM_App.Commons;
using SDSM_App.Helpers;
using SDSM_App.InjectableServices;
using SDSM_App.Models;

namespace SDSM_App.ViewModels;

/// <summary>
/// 프로그램 정보 창 — 이름 · 판 · 빌드 날짜 · 적용 지침 · 종목록 기준 · 개발사(2026-10-22 §21-5).
/// 글은 모두 <see cref="AppInfo"/> 한 곳에서 온다. 종 수만 DB 에서 센다(읽기만).
/// </summary>
public partial class AppInfoWindowViewModel : ObservableObject, ITransientService
{
    public AppInfoWindowViewModel()
    {
        LoadSpeciesCounts();
    }

    public string ProgramName => AppInfo.ProgramName;
    public string VersionText => $"{AppInfo.Version} (커밋 {AppInfo.Commit})";
    public string BuildDate => AppInfo.BuildDate;
    public string GuidelineTitle => AppInfo.GuidelineTitle;
    public string GuidelineNotice => AppInfo.GuidelineNotice;
    public string SpeciesListBasis => AppInfo.SpeciesListBasis;
    public string Developer => AppInfo.Developer;
    public string Contact => AppInfo.Contact.Replace(" · ", Environment.NewLine);

    [ObservableProperty] private string _speciesCountText = "-";

    //지침 원본(PDF) — 사용 설명서와 같은 창으로 연다
    [RelayCommand]
    private void GuidelineOpenExecuted() => ManualWindowHelper.Open(AppInfo.GuidelinePdfUri, AppInfo.GuidelinePdfCaption);

    //종목록 내보내기(species.json · 어류 · 저서) — 조사자 앱 반영용(2026-10-22 §22-3 ②)
    [RelayCommand]
    private void SpeciesCatalogExportExecuted() => SpeciesCatalogExporter.Export();

    private async void LoadSpeciesCounts()
    {
        try
        {
            using var context = new SDSMDbContext();
            var fish = await context.FishSpeciesLists.CountAsync();
            var benthos = await context.BenthosSpeciesLists.CountAsync();
            var indicator = await context.BenthosSpeciesLists.CountAsync(m => m.SaprobicValue != null);
            SpeciesCountText = $"어류 {fish:N0}종 · 저서동물 {benthos:N0}종(지표종 {indicator:N0})";
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(LoadSpeciesCounts), ex.Message);
            SpeciesCountText = "종목록을 읽지 못했습니다";
        }
    }
}
