using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SDSM_App.Enums;
using SDSM_App.Helpers;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.Models;
using SDSM_App.Models.Repositories.Interfases;
using SDSM_App.Views.Windows;
using SDSM_Models;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SQLite;
using System.IO;
using System.Media;
using System.Reflection;
using System.Text;
using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;
using Telerik.Windows.Documents.Spreadsheet.FormatProviders.OpenXml.Xlsx;
using Telerik.Windows.Documents.Spreadsheet.Model;

namespace SDSM_App.ViewModels;

/// <summary>
/// 일괄등록 서식수변
/// </summary>
public partial class BulkBenthosControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    private readonly (int, string)[] _weathers = [(1, "맑음"), (2, "흐림"), (3, "비(눈)")];
    private readonly (int, string)[] _camopyCovers = [(1, "없음"), (2, "부분적"), (3, "짙음")];
    private readonly (int, string)[] _floodplains = [(1, "자연형"), (2, "농경지"), (3, "도로"), (4, "주차장"), (5, "산책로"), (6, "기타")];
    private readonly (int, string)[] _habitatRiverTypes = [(1, "산간형"), (2, "평지형"), (3, "강"), (4, "인공구조물")];
    private readonly (int, string)[] _flowStates = [(1, "여울"), (2, "흐름"), (3, "소")];

    private string _loadedFileName = string.Empty;

    // null : 생태현황, 방류하천 없음
    // tryue : 생태현황 / false : 방류하천
    private bool? _isEcology = null;

    #endregion

    #region 생성자

    public BulkBenthosControlViewModel()
    {
        InitialLoad();
    }

    #endregion

    #region 속성

    //저장된 파일목록
    public List<SavedFileInfo> SavedFileInfos { get; set; } = [];

    //항목 열 번호 목록
    public List<ColumnNumber> ColumnNumbers { get; set; } = [];

    [ObservableProperty] private RadObservableCollection<ImportBenthos> _modelCollection = [];
    [ObservableProperty] private RadObservableCollection<Benthos> _convertedCollection = [];

    [ObservableProperty] private RadObservableCollection<ImportBenthosSpecies> _speciesCollection = [];
    [ObservableProperty] private RadObservableCollection<BenthosSpecies> _convertedSpeciesCollection = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DataImportExecutedCommand))]
    private string _sheetName = "입력";

    [ObservableProperty] private int _firstRowNumber = 2;
    [ObservableProperty] private int _firstColumnNumber = 13;
    public string FirstColumnAlphabet { get; } = "M";
    [ObservableProperty] private int _speciesFirstRowNumber = 47;

    //에러 개수
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))]
    private int _errorCount;

    //변환된 자료 개수
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))]
    private int _convertedCount;

    //종목록 에러 개수
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))]
    private int _errorSpeciesCount;

    //종목록 변환된 자료 개수
    [ObservableProperty]
    private int _convertedSpeciesCount;

    //하상 암반·콘크리트(0보다 큼)를 특이사항으로 옮긴 기록 수 — 지침 저서 하상 6칸 밖(2026-10-21 §20). 막지는 않는다
    [ObservableProperty] private int _substrateMovedCount;

    #endregion

    #region 종목록 속성

    //종 행 점검 결과 — 종목록 값과 다른 종 · 목록 밖 종(2026-10-22 §22-2 · §22-3)
    private SpeciesValueCheck _speciesCheck = new();

    //목록 밖 종을 종목록에 더할지(저장 때 확인 창에서 고름)
    private bool _addOutOfListToList;

    //파일 값이 종목록 값과 달라 종목록 값을 쓴 종 수
    [ObservableProperty] private int _listValueDiffCount;

    //종목록에 없는 종 수
    [ObservableProperty] private int _outOfListCount;

    /// <summary>종목록 값과 다른 종 — '국명 학명: 파일 … → 종목록 …'(결과 줄 풍선 · 작업로그용).</summary>
    public IReadOnlyList<string> ListValueDiffDetails => [.. _speciesCheck.Differences.Select(kv => $"{kv.Key}: {kv.Value}")];

    /// <summary>목록 밖 종(확인 창 · 작업로그용).</summary>
    public IReadOnlyList<OutOfListSpecies> OutOfListSpecies => [.. _speciesCheck.OutOfList.Values];

    public string ListValueDiffText => string.Join(Environment.NewLine, ListValueDiffDetails);

    #endregion

    #region ICommand 구현

    [RelayCommand]
    private void SheetNameResetExecuted()
    {
        SheetName = "입력";
    }

    private bool CanDataImport()
    {
        return !string.IsNullOrWhiteSpace(SheetName);
    }

    //자료 가져오기
    [RelayCommand(CanExecute = nameof(CanDataImport))]
    private async Task DataImportExecuted()
    {
        OpenFileDialog openFileDialog = new()
        {
            Filter = "Excel파일(*.xlsx)|*.xlsx"
        };

        if (openFileDialog.ShowDialog() == false) return;

        string fileName = openFileDialog.FileName;

        if (!File.Exists(fileName))
        {
            return;
        }

        //생태현황, 방류하천 포함여부 체크
        (bool? isEcology, string? message) checkResult = SiteDivisionHelper.CheckFileNameByKeywords(Path.GetFileName(fileName));
        if (checkResult.isEcology == null)
        {
            MessageBox.Show(checkResult.message, "파일명 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        else
        {
            _isEcology = checkResult.isEcology;
        }

        _loadedFileName = fileName;
        var savedFileInfo = SavedFileInfos.Where(m => m.FileName.Replace(" ", "").ToLower() == Path.GetFileName(_loadedFileName).Replace(" ", "").ToLower()).LastOrDefault();

        //파일명만 같은 것은 정보로만 알린다 — 같은 기록인지는 저장할 때 내용으로 검사한다(2026-10-21 문제 2·3)
        if (savedFileInfo != null)
        {
            var info = (DuplicateImportHelper.SavedFileNameInfo(savedFileInfo.FileName, savedFileInfo.SavedTime), true);
            WeakReferenceMessenger.Default.Send(new NotifyMessage(info));
        }

        GB.IsBusy = true;
        GB.BusyContent = "자료 가져오는 중...";

        Workbook? workbook = null;
        Worksheet? worksheet = null;

        try
        {
            using Stream input = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            workbook = await Task.Run(() => new XlsxFormatProvider().Import(input, null));
            bool isExist = false;

            //초기화
            Reset();

            var names = new string[1];
            names[0] = SheetName.ToLower();

            foreach (Worksheet sheet in workbook.Sheets)
            {
                if (sheet.Name.Replace(" ", "").ToLower() == SheetName.Replace(" ", "").ToLower())
                {
                    worksheet = sheet;
                    isExist = true;
                }
            }

            if (isExist == false)
            {
                var message = $"엑셀 시트명 : {SheetName}{Environment.NewLine}을 찾을 수 없습니다.{Environment.NewLine}";

                MessageBox.Show(message, "엑셀 시트명 오류", MessageBoxButton.OK, MessageBoxImage.Warning);

                GB.IsBusy = false;
                GB.BusyContent = string.Empty;

                return;
            }

            //엑셀 -> 모델
            if (worksheet != null && worksheet.Rows.Count > 0)
            {
                //지표값 출처 = 관리자 종목록 — 종 행을 읽자마자 종목록 값으로 바꾸고(목록 밖 종은 비움) 그 값으로 지수를 계산한다(2026-10-22 §22-2)
                var check = new SpeciesValueCheck();
                var listIndex = await Task.Run(() =>
                {
                    using var listContext = new SDSMDbContext();
                    return SpeciesListValues.BenthosIndex(listContext.BenthosSpeciesLists.AsNoTracking().ToList());
                });

                var (models, species) = await Task.Run(() => DataTableToModels(worksheet, listIndex, check));
                if (models.Count != 0)
                {
                    if (GB.IsBusy == false)
                        GB.IsBusy = true;

                    GB.BusyContent = "자료 검수 중...";

                    var data = await Task.Run(() => DataValidation(models));
                    ModelCollection.AddRange(data.Item1);
                    ConvertedCollection.AddRange(data.Item2);

                    ConvertedCount = ConvertedCollection.Count;
                    ErrorCount = ConvertedCollection.Count(m => m.ErrorContent.Length > 0);

                    SubstrateMovedCount = data.Item3;
                    if (SubstrateMovedCount > 0)
                    {
                        var info = ($"하상 암반·콘크리트 값 {SubstrateMovedCount:N0}건을 특이사항으로 옮겼습니다(지침 저서 하상 6칸 밖)", true);
                        WeakReferenceMessenger.Default.Send(new NotifyMessage(info));
                    }

                    var speciesData = await Task.Run(() => SpeciesDataValidation(species));
                    SpeciesCollection.AddRange(speciesData.Item1);
                    ConvertedSpeciesCollection.AddRange(speciesData.Item2);

                    ConvertedSpeciesCount = SpeciesCollection.Count;
                    ErrorSpeciesCount = ConvertedSpeciesCollection.Count(m => m.ErrorContent.Length > 0);

                    //종목록 값과 다른 종 · 목록 밖 종 — 결과 줄 · 알림(2026-10-22 §22-2 · §22-3)
                    _speciesCheck = check;
                    ListValueDiffCount = check.Differences.Count;
                    OutOfListCount = check.OutOfList.Count;
                    OnPropertyChanged(nameof(ListValueDiffText));
                    var speciesNotice = SpeciesNotice();
                    if (speciesNotice.Length > 0)
                        WeakReferenceMessenger.Default.Send(new NotifyMessage((speciesNotice, true)));
                }
            }

            GB.IsBusy = false;
            GB.BusyContent = string.Empty;
        }
        catch (IOException)
        {
            MessageBox.Show("파일이 실행중인지 확인하세요", "가져오기 오류", MessageBoxButton.OK, MessageBoxImage.Error);

            GB.IsBusy = false;
            GB.BusyContent = string.Empty;
        }
        catch (Exception)
        {
            MessageBox.Show("파일 변환 오류", "가져오기 오류", MessageBoxButton.OK, MessageBoxImage.Error);

            GB.IsBusy = false;
            GB.BusyContent = string.Empty;
        }
    }

    private bool CanSave()
    {
        return ErrorCount == 0 && ConvertedCount > 0 && ErrorSpeciesCount == 0;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveExecuted()
    {
        if (ErrorCount != 0 || ConvertedCount == 0 || ErrorSpeciesCount != 0)
            return;

        string message = $"자료 : {ConvertedCount:N0}건을{Environment.NewLine}저장하시겠습니까?";

        SystemSounds.Beep.Play();
        if (MessageBox.Show(message, "자료 일괄 저장", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            return;

        //종목록에 없는 종 — [목록에 추가(지표값 없음)] / [목록에 넣지 않고 저장] / [취소] (2026-10-22 §22-3)
        _addOutOfListToList = false;
        if (_speciesCheck.OutOfList.Count > 0)
        {
            var action = OutOfListSpeciesWindow.Ask("저서동물", OutOfListSpecies);
            if (action == OutOfListAction.Cancel)
                return;
            _addOutOfListToList = action == OutOfListAction.AddToList;
        }

        //DB에 같은 기록이 있는지 — 있으면 [건너뛰기(기본)/바꾸기/추가]를 묻는다(2026-10-21 문제 2·3)
        var decisions = FindDuplicates();
        if (decisions == null)
        {
            MessageBox.Show("같은 기록을 확인하는 중 오류가 발생하였습니다.", "저장 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        //저장
        var saveResult = await Task.Run(() => AddSave(decisions));

        if (saveResult.IsSuccess)
        {
            if (saveResult.Saved + saveResult.Replaced > 0)
            {
                //저장된 파일정보
                var repogitory = App.Current.Services.GetRequiredService<ISavedFileInfoRepository>();
                var savedFileInfo = new SavedFileInfo
                {
                    Division = "저서동물",
                    FileName = Path.GetFileName(_loadedFileName),
                    SavedTime = DateTime.Now
                };

                var savedModel = await repogitory.InsertAsync(savedFileInfo);
                if (savedModel != null)
                {
                    SavedFileInfos.Add(savedModel);
                }
            }

            //저장·바꿈·건너뜀 건수에 맞는 알림(2026-10-21 N5)
            var toast = DuplicateImportHelper.SaveToast(saveResult);
            var speciesNotice = SpeciesNotice();
            if (speciesNotice.Length > 0 && saveResult.Saved + saveResult.Replaced > 0)
                toast = ($"{toast.Item1} · {speciesNotice}", toast.Item2);
            if (SubstrateMovedCount > 0 && saveResult.Saved + saveResult.Replaced > 0)
                toast = ($"{toast.Item1} · 하상 암반·콘크리트 → 특이사항 {SubstrateMovedCount:N0}건", toast.Item2);
            WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

            if (decisions.Count > 0)
                DuplicateImportHelper.ShowSummary("저서동물", saveResult.Saved, saveResult.Replaced, saveResult.Skipped, saveResult.DuplicateAdded);

            Reset();
        }
        else
        {
            MessageBox.Show("저장 중 오류가 발생하였습니다.", "저장 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region 메서드

    private void InitialLoad()
    {
        GetColumnNumbers();
        GetSavedFileInfos();
    }

    //엑셀->모델
    private (List<ImportBenthos>, List<ImportBenthosSpecies>) DataTableToModels(Worksheet worksheet, IReadOnlyDictionary<string, BenthosSpeciesList> listIndex, SpeciesValueCheck check)
    {
        var importModels = new List<ImportBenthos>();
        var importSpeciesModels = new List<ImportBenthosSpecies>();

        try
        {
            CellRange usedCellRange = worksheet.UsedCellRange;
            int matchNumber = 0;

            for (int columnIndex = FirstColumnNumber - 1; columnIndex <= usedCellRange.ToIndex.ColumnIndex; columnIndex++)
            {
                var firstValue = ((ICellValue)(worksheet.Cells[FirstRowNumber - 1, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[FirstRowNumber - 1, columnIndex].GetFormat().Value);
                if (!string.IsNullOrWhiteSpace(firstValue))
                {
                    matchNumber++;

                    //종목록
                    var speciesData = GetSpeciesData(worksheet, columnIndex, matchNumber, SpeciesFirstRowNumber);

                    //지표값 · 길드 = 종목록 값(목록 밖 종은 비움) — 아래 지수 계산 전에(2026-10-22 §22-2)
                    SpeciesListValues.ApplyBenthos(speciesData, listIndex, check);

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

                    var surveyUnavailableReason = ((ICellValue)(worksheet.Cells[43, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[43, columnIndex].GetFormat().Value);

                    //조사불가·0종이면 DI·H'·R1·J' 는 NULL(BMI 는 그대로 '-' · 0 E) — 2026-10-21 문제 15
                    var (indexDI, indexH, indexR1, indexJ) = GB.GetBenthosIndices(speciesData, surveyUnavailableReason);
                    var (score, grade) = GB.GetBMI(speciesData, surveyUnavailableReason);

                    var speciesCount = speciesData.GroupBy(m => m.SpeciesKo).Count();
                    var individualCountSum = speciesData.Sum(m => m.IndividualCount);

                    var model = new ImportBenthos
                    {
                        MacthNumber = matchNumber,
                        YearChsu = firstValue,
                        Site = ((ICellValue)(worksheet.Cells[2, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[2, columnIndex].GetFormat().Value),
                        SurveyDate = ((ICellValue)(worksheet.Cells[3, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[3, columnIndex].GetFormat().Value),
                        MajorRegion = ((ICellValue)(worksheet.Cells[4, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[4, columnIndex].GetFormat().Value),
                        MiddleRegion = ((ICellValue)(worksheet.Cells[5, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[5, columnIndex].GetFormat().Value),
                        River = ((ICellValue)(worksheet.Cells[6, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[6, columnIndex].GetFormat().Value),
                        RiverType = ((ICellValue)(worksheet.Cells[7, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[7, columnIndex].GetFormat().Value),
                        Lat = ((ICellValue)(worksheet.Cells[8, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[8, columnIndex].GetFormat().Value),
                        Lng = ((ICellValue)(worksheet.Cells[9, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[9, columnIndex].GetFormat().Value),
                        Weather = ((ICellValue)(worksheet.Cells[10, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[10, columnIndex].GetFormat().Value),
                        SurveyAgency = ((ICellValue)(worksheet.Cells[11, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[11, columnIndex].GetFormat().Value),
                        Surveyor = ((ICellValue)(worksheet.Cells[12, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[12, columnIndex].GetFormat().Value),
                        Surbernet30 = ((ICellValue)(worksheet.Cells[13, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[13, columnIndex].GetFormat().Value),
                        Surbernet50 = ((ICellValue)(worksheet.Cells[14, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[14, columnIndex].GetFormat().Value),
                        Dredge = ((ICellValue)(worksheet.Cells[15, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[15, columnIndex].GetFormat().Value),
                        Ekman = ((ICellValue)(worksheet.Cells[16, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[16, columnIndex].GetFormat().Value),
                        Watershed = ((ICellValue)(worksheet.Cells[17, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[17, columnIndex].GetFormat().Value),
                        PollutionSource = ((ICellValue)(worksheet.Cells[18, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[18, columnIndex].GetFormat().Value),
                        CamopyCover = ((ICellValue)(worksheet.Cells[19, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[19, columnIndex].GetFormat().Value),
                        Floodplain = ((ICellValue)(worksheet.Cells[20, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[20, columnIndex].GetFormat().Value),
                        LeveeLeft = ((ICellValue)(worksheet.Cells[21, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[21, columnIndex].GetFormat().Value),
                        LeveeRight = ((ICellValue)(worksheet.Cells[22, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[22, columnIndex].GetFormat().Value),
                        Bedrock = ((ICellValue)(worksheet.Cells[23, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[23, columnIndex].GetFormat().Value),
                        Concrete = ((ICellValue)(worksheet.Cells[24, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[24, columnIndex].GetFormat().Value),
                        Mud = ((ICellValue)(worksheet.Cells[25, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[25, columnIndex].GetFormat().Value),
                        Sand = ((ICellValue)(worksheet.Cells[26, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[26, columnIndex].GetFormat().Value),
                        FineGravel = ((ICellValue)(worksheet.Cells[27, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[27, columnIndex].GetFormat().Value),
                        Gravel = ((ICellValue)(worksheet.Cells[28, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[28, columnIndex].GetFormat().Value),
                        SmallStone = ((ICellValue)(worksheet.Cells[29, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[29, columnIndex].GetFormat().Value),
                        BigStone = ((ICellValue)(worksheet.Cells[30, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[30, columnIndex].GetFormat().Value),
                        HabitatRiverType = ((ICellValue)(worksheet.Cells[32, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[32, columnIndex].GetFormat().Value),
                        //환경 측정값(34~39행)은 셀 표시 형식(양식 '0.0')이 아니라 일반 형식으로 읽는다 — 0.45 가 '0.5' 로 읽히지 않게(원값 저장) · 2026-10-21 문제 4(저서)
                        RiverWidth = ((ICellValue)(worksheet.Cells[33, columnIndex].GetValue().Value)).GetResultValueAsString(CellValueFormat.GeneralFormat),
                        WaterWidth = ((ICellValue)(worksheet.Cells[34, columnIndex].GetValue().Value)).GetResultValueAsString(CellValueFormat.GeneralFormat),
                        AverageDepth = ((ICellValue)(worksheet.Cells[35, columnIndex].GetValue().Value)).GetResultValueAsString(CellValueFormat.GeneralFormat),
                        AverageVelocity = ((ICellValue)(worksheet.Cells[36, columnIndex].GetValue().Value)).GetResultValueAsString(CellValueFormat.GeneralFormat),
                        Temperature = ((ICellValue)(worksheet.Cells[37, columnIndex].GetValue().Value)).GetResultValueAsString(CellValueFormat.GeneralFormat),
                        WaterTemperature = ((ICellValue)(worksheet.Cells[38, columnIndex].GetValue().Value)).GetResultValueAsString(CellValueFormat.GeneralFormat),
                        FlowState = ((ICellValue)(worksheet.Cells[39, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[39, columnIndex].GetFormat().Value),
                        Transparency = ((ICellValue)(worksheet.Cells[40, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[40, columnIndex].GetFormat().Value),
                        Smell = ((ICellValue)(worksheet.Cells[41, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[41, columnIndex].GetFormat().Value),
                        Note = ((ICellValue)(worksheet.Cells[42, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[42, columnIndex].GetFormat().Value),
                        SurveyUnavailableReason = surveyUnavailableReason,
                        IndexDI = indexDI,
                        IndexH = indexH,
                        IndexR1 = indexR1,
                        IndexJ = indexJ,
                        BMIScore = score,
                        BMIGrade = grade,
                        SpeciesCount = speciesCount,
                        IndividualCount = individualCountSum ?? 0,
                    };

                    importModels.Add(model);

                    importSpeciesModels.AddRange(speciesData);
                }
            }
            return (importModels, importSpeciesModels);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(DataTableToModels), ex.Message);

            return (importModels, importSpeciesModels);
        }
    }

    //기본정보 검수하기
    private (List<ImportBenthos>, List<Benthos>, int) DataValidation(List<ImportBenthos> importModels)
    {
        StringBuilder sb = new();
        var converteds = new List<Benthos>();
        int substrateMoved = 0;

        try
        {
            foreach (ImportBenthos model in importModels)
            {
                sb.Clear();

                var insertModel = new Benthos();

                // 날짜 범위 체크
                var minDate = GB.MinDate;
                var maxDate = DateTime.Today;

                //다시 검수 때문에 초기화
                model.ErrorContent = string.Empty;

                //import model
                PropertyInfo[] props = model.GetType().GetProperties();

                foreach (var prop in props)
                {
                    if (prop.Name == "ErrorContent") continue;

                    var value = prop.GetValue(model);

                    if (prop.Name == "MacthNumber"
                        || prop.Name == "IndexDI" || prop.Name == "IndexH" || prop.Name == "IndexR1" || prop.Name == "IndexJ"
                        || prop.Name == "BMIScore" || prop.Name == "BMIGrade"
                        || prop.Name == "SpeciesCount" || prop.Name == "IndividualCount")
                    {
                        PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                        insertPropertyInfo?.SetValue(insertModel, value);
                    }
                    else if (prop.Name == "YearChsu")
                    {
                        //필수 문자
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            var v = ((string)value).Replace(" ", "");
                            if (v.Length > 0)
                            {
                                if (v.Length >= 4 && int.TryParse(v[..4], out int year))
                                {
                                    PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                    insertPropertyInfo?.SetValue(insertModel, v);

                                    if (year >= minDate.Year && year <= maxDate.Year)
                                    {
                                        //연도
                                        insertModel.SurveyYear = year;
                                    }
                                    else
                                    {
                                        sb.Append(GB.GetImportErrorText(prop, ErrorType.YearError));
                                    }
                                }
                                else
                                {
                                    sb.Append(GB.GetImportErrorText(prop, ErrorType.YearError));
                                }
                            }
                            else
                            {
                                sb.Append(GB.GetImportErrorText(prop, ErrorType.Unique));
                            }
                        }
                        else
                        {
                            sb.Append(GB.GetImportErrorText(prop, ErrorType.Unique));
                        }
                    }
                    else if (prop.Name == "Site")
                    {
                        PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);

                        //필수, 생태현황, 방류하천 체크
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            var v = ((string)value).Replace(" ", "").ToUpper();
                            if (v.Length > 0)
                            {
                                var siteDivisionId = SiteDivisionHelper.FindSiteDivisionId(_isEcology, v, model.YearChsu);
                                if (siteDivisionId != null)
                                {
                                    insertPropertyInfo?.SetValue(insertModel, v);
                                    insertModel.SiteDivisionId = siteDivisionId;
                                }
                                else
                                {
                                    sb.Append(GB.GetImportErrorText(prop, ErrorType.SiteError));
                                }
                            }
                            else
                            {
                                if (_isEcology == true)
                                {
                                    var siteDivisionId = SiteDivisionHelper.FindSiteDivisionIdBySiteContains(_isEcology, null);
                                    if (siteDivisionId != null)
                                    {
                                        insertPropertyInfo?.SetValue(insertModel, null);
                                        insertModel.SiteDivisionId = siteDivisionId;
                                    }
                                    else
                                    {
                                        sb.Append(GB.GetImportErrorText(prop, ErrorType.SiteError));
                                    }
                                }
                                else
                                {
                                    sb.Append(GB.GetImportErrorText(prop, ErrorType.Unique));
                                }
                            }
                        }
                        else
                        {
                            if (_isEcology == true)
                            {
                                var siteDivisionId = SiteDivisionHelper.FindSiteDivisionIdBySiteContains(_isEcology, null);
                                if (siteDivisionId != null)
                                {
                                    insertPropertyInfo?.SetValue(insertModel, null);
                                    insertModel.SiteDivisionId = siteDivisionId;
                                }
                                else
                                {
                                    sb.Append(GB.GetImportErrorText(prop, ErrorType.SiteError));
                                }
                            }
                            else
                            {
                                sb.Append(GB.GetImportErrorText(prop, ErrorType.Unique));
                            }
                        }
                    }
                    else if (prop.Name == "SurveyDate")
                    {
                        //형식은 Null - DB입력은 필수 날짜
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            if (DateTime.TryParse(value.ToString(), out DateTime dt))
                            {
                                if (dt < minDate || dt > maxDate)
                                {
                                    sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                                }
                                else
                                {
                                    PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                    insertPropertyInfo?.SetValue(insertModel, dt);
                                }
                            }
                            else if (int.TryParse(value.ToString(), out int i))
                            {
                                if (i.ToString().Length == 8)
                                {
                                    var yyyymmdd = GB.ToDateFromInt(i);
                                    if (yyyymmdd != null)
                                    {
                                        if (yyyymmdd < minDate || yyyymmdd > maxDate)
                                        {
                                            sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                                        }
                                        else
                                        {
                                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                            insertPropertyInfo?.SetValue(insertModel, yyyymmdd);
                                        }
                                    }
                                    else
                                    {
                                        sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                                    }
                                }
                                else if (i.ToString().Length == 6)
                                {
                                    var yymmdd = GB.ToDateFrom6Int(i);
                                    if (yymmdd != null)
                                    {
                                        if (yymmdd < minDate || yymmdd > maxDate)
                                        {
                                            sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                                        }
                                        else
                                        {
                                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                            insertPropertyInfo?.SetValue(insertModel, yymmdd);
                                        }
                                    }
                                    else
                                    {
                                        sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                                    }
                                }
                                else
                                {
                                    sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                                }
                            }
                            else
                            {
                                sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                            }
                        }
                        else
                        {
                            sb.Append(GB.GetImportErrorText(prop, ErrorType.Unique));
                        }

                    }
                    else if (prop.Name == "Weather")
                    {
                        //날씨
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            var items = ((string)value).Replace(" ", "").Split([',', '.'], StringSplitOptions.RemoveEmptyEntries);
                            var itemNames = new List<string>();

                            foreach (var item in items)
                            {
                                if (int.TryParse(item, out int num))
                                {
                                    var name = _weathers.FirstOrDefault(w => w.Item1 == num).Item2;
                                    if (!string.IsNullOrEmpty(name))
                                        itemNames.Add(name);
                                }
                                else
                                {
                                    // 숫자가 아니면 원본 값 사용
                                    itemNames.Add(item);
                                }
                            }

                            var result = string.Join(",", itemNames);

                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                            insertPropertyInfo?.SetValue(insertModel, result);
                        }
                        else
                        {
                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                            insertPropertyInfo?.SetValue(insertModel, null);
                        }
                    }
                    else if (prop.Name == "Lat")
                    {
                        //실수
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            if (double.TryParse(value.ToString(), out double d))
                            {
                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                insertPropertyInfo?.SetValue(insertModel, Math.Round(d, 6, MidpointRounding.AwayFromZero));
                            }
                            else
                            {
                                //문자
                                var (latDo, latBun, latCho) = GB.ParseDmsString(value.ToString());
                                if (latDo != null && latBun != null && latCho != null)
                                {
                                    var lat = GB.DmsToDegress(latDo.Value, latBun.Value, latCho.Value);
                                    PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                    insertPropertyInfo?.SetValue(insertModel, Math.Round(lat, 6, MidpointRounding.AwayFromZero));
                                }
                                else
                                {
                                    sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                                }
                            }
                        }
                        else
                        {
                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                            insertPropertyInfo?.SetValue(insertModel, null);
                        }
                    }
                    else if (prop.Name == "Lng")
                    {
                        //실수
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            if (double.TryParse(value.ToString(), out double d))
                            {
                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                insertPropertyInfo?.SetValue(insertModel, Math.Round(d, 6, MidpointRounding.AwayFromZero));
                            }
                            else
                            {
                                //문자
                                var (lngDo, lngBun, lngCho) = GB.ParseDmsString(value.ToString());
                                if (lngDo != null && lngBun != null && lngCho != null)
                                {
                                    var lng = GB.DmsToDegress(lngDo.Value, lngBun.Value, lngCho.Value);
                                    PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                    insertPropertyInfo?.SetValue(insertModel, Math.Round(lng, 6, MidpointRounding.AwayFromZero));
                                }
                                else
                                {
                                    sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                                }
                            }
                        }
                        else
                        {
                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                            insertPropertyInfo?.SetValue(insertModel, null);
                        }
                    }
                    else if (prop.Name == "Bedrock" || prop.Name == "Concrete")
                    {
                        //지침 저서 하상 6칸 밖 — 검수하지 않고 칸은 NULL. 원문 값은 아래에서 특이사항으로 옮긴다(2026-10-21 §20)
                        PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                        insertPropertyInfo?.SetValue(insertModel, null);
                    }
                    else if (prop.Name == "Surbernet30" || prop.Name == "Surbernet50" || prop.Name == "Dredge" || prop.Name == "Ekman"
                           || prop.Name == "Mud" || prop.Name == "Sand" || prop.Name == "FineGravel" || prop.Name == "Gravel"
                           || prop.Name == "SmallStone" || prop.Name == "BigStone" || prop.Name == "Transparency" || prop.Name == "Smell")
                    {
                        //정수
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            if (int.TryParse(value.ToString(), out int i))
                            {
                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                insertPropertyInfo?.SetValue(insertModel, i);
                            }
                            else if (double.TryParse(value.ToString(), out double d))
                            {
                                //소수점이 있는 경우 잘라내기
                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                insertPropertyInfo?.SetValue(insertModel, (int)Math.Truncate(d));
                            }
                            else
                            {
                                sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                            }
                        }
                        else
                        {
                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                            insertPropertyInfo?.SetValue(insertModel, null);
                        }
                    }
                    else if (prop.Name == "RiverWidth" || prop.Name == "WaterWidth" || prop.Name == "AverageDepth"
                        || prop.Name == "AverageVelocity" || prop.Name == "Temperature" || prop.Name == "WaterTemperature")
                    {
                        //실수 — 환경 측정값은 원값 저장(반올림하지 않음). 1자리는 목록·편집 화면 서식으로만 보인다 — 2026-10-21 문제 4(저서)
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            if (double.TryParse(value.ToString(), out double d))
                            {
                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                insertPropertyInfo?.SetValue(insertModel, d);
                            }
                            else
                            {
                                sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                            }
                        }
                        else
                        {
                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                            insertPropertyInfo?.SetValue(insertModel, null);
                        }
                    }
                    else if (prop.Name == "CamopyCover" || prop.Name == "Floodplain" || prop.Name == "HabitatRiverType" || prop.Name == "FlowState")
                    {
                        //숫자를 텍스트로
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            if (prop.Name == "CamopyCover")
                            {
                                var items = ((string)value).Replace(" ", "").Split([',', '.'], StringSplitOptions.RemoveEmptyEntries);
                                var itemNames = new List<string>();

                                foreach (var item in items)
                                {
                                    if (int.TryParse(item, out int num))
                                    {
                                        var name = _camopyCovers.FirstOrDefault(w => w.Item1 == num).Item2;
                                        if (!string.IsNullOrEmpty(name))
                                            itemNames.Add(name);
                                    }
                                    else
                                    {
                                        // 숫자가 아니면 원본 값 사용
                                        itemNames.Add(item);
                                    }
                                }

                                var result = string.Join(",", itemNames);

                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                insertPropertyInfo?.SetValue(insertModel, result);
                            }
                            else if (prop.Name == "Floodplain")
                            {
                                var items = ((string)value).Replace(" ", "").Split([',', '.'], StringSplitOptions.RemoveEmptyEntries);
                                var itemNames = new List<string>();

                                foreach (var item in items)
                                {
                                    if (int.TryParse(item, out int num))
                                    {
                                        var name = _floodplains.FirstOrDefault(w => w.Item1 == num).Item2;
                                        if (!string.IsNullOrEmpty(name))
                                            itemNames.Add(name);
                                    }
                                    else
                                    {
                                        // 숫자가 아니면 원본 값 사용
                                        itemNames.Add(item);
                                    }
                                }

                                var result = string.Join(",", itemNames);

                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                insertPropertyInfo?.SetValue(insertModel, result);
                            }
                            else if (prop.Name == "HabitatRiverType")
                            {
                                var items = ((string)value).Replace(" ", "").Split([',', '.'], StringSplitOptions.RemoveEmptyEntries);
                                var itemNames = new List<string>();

                                foreach (var item in items)
                                {
                                    if (int.TryParse(item, out int num))
                                    {
                                        var name = _habitatRiverTypes.FirstOrDefault(w => w.Item1 == num).Item2;
                                        if (!string.IsNullOrEmpty(name))
                                            itemNames.Add(name);
                                    }
                                    else
                                    {
                                        // 숫자가 아니면 원본 값 사용
                                        itemNames.Add(item);
                                    }
                                }

                                var result = string.Join(",", itemNames);

                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                insertPropertyInfo?.SetValue(insertModel, result);
                            }
                            else if (prop.Name == "FlowState")
                            {
                                var items = ((string)value).Replace(" ", "").Split([',', '.'], StringSplitOptions.RemoveEmptyEntries);
                                var itemNames = new List<string>();

                                foreach (var item in items)
                                {
                                    if (int.TryParse(item, out int num))
                                    {
                                        var name = _flowStates.FirstOrDefault(w => w.Item1 == num).Item2;
                                        if (!string.IsNullOrEmpty(name))
                                            itemNames.Add(name);
                                    }
                                    else
                                    {
                                        // 숫자가 아니면 원본 값 사용
                                        itemNames.Add(item);
                                    }
                                }

                                var result = string.Join(",", itemNames);

                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                insertPropertyInfo?.SetValue(insertModel, result);
                            }
                        }
                        else
                        {
                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                            insertPropertyInfo?.SetValue(insertModel, null);
                        }
                    }
                    else
                    {
                        //문자
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);

                            if (prop.Name == "River" ||  prop.Name == "RiverType" || prop.Name == "SurveyUnavailableReason")
                            {
                                insertPropertyInfo?.SetValue(insertModel, ((string)value).Replace(" ", ""));
                            }
                            else
                            {
                                insertPropertyInfo?.SetValue(insertModel, ((string)value).Trim());
                            }
                        }
                        else
                        {
                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                            insertPropertyInfo?.SetValue(insertModel, null);
                        }
                    }
                }

                //저서 하상은 지침 6칸 — 암반·콘크리트 파일 값은 특이사항 끝으로 옮기고 두 칸은 NULL · 2026-10-21 §20
                //숫자 0보다 큼 → 'n%' · 숫자 아닌 글자 → 「원문」 · 0 · 빈칸 · 빈 표시('-' · 'N/A' 등)는 버림. 변환 오류로 막지 않는다
                var movedText = BenthosSubstrateNote.MovedText(model.Bedrock, model.Concrete);
                insertModel.Note = BenthosSubstrateNote.AppendTo(insertModel.Note, movedText);
                if (movedText != null) substrateMoved++;

                if (sb.Length > 0)
                {
                    model.ErrorContent = sb.ToString();
                    insertModel.ErrorContent = sb.ToString();
                }

                converteds.Add(insertModel);
            }
            return (importModels, converteds, substrateMoved);
        }
        catch (SqliteException ex)
        {
            MessageBox.Show("데이터베이스 또는 테이블을 찾을수 없습니다.", "데이터베이스 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            GB.LogWrite(GetType().Name, nameof(DataValidation), ex.Message);
            return (importModels, converteds, substrateMoved);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(DataValidation), ex.Message);
            return (importModels, converteds, substrateMoved);
        }
    }

    //종목록 검수하기
    private (List<ImportBenthosSpecies>, List<BenthosSpecies>) SpeciesDataValidation(List<ImportBenthosSpecies> importModels)
    {
        StringBuilder sb = new();
        var converteds = new List<BenthosSpecies>();

        try
        {
            foreach (ImportBenthosSpecies model in importModels)
            {
                sb.Clear();

                var insertModel = new BenthosSpecies();

                //다시 검수 때문에 초기화
                model.ErrorContent = string.Empty;

                //import model
                PropertyInfo[] props = model.GetType().GetProperties();

                foreach (var prop in props)
                {
                    if (prop.Name == "ErrorContent") continue;

                    var value = prop.GetValue(model);

                    if (prop.Name == "MacthNumber" || prop.Name == "RankScorer")
                    {
                        PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                        insertPropertyInfo?.SetValue(insertModel, value);
                    }
                    else if (prop.Name == "IndividualCount" || prop.Name == "SaprobicValue")
                    {
                        //실수 — 반올림하지 않고 읽은 값 그대로 저장. 지수(DI·H′·R1·J′·BMI)는 GetSpeciesData 에서 읽은 이 값으로 계산하므로
                        //종 행도 같은 값으로 둔다(읽는 방식은 그대로). 1자리는 목록 화면 서식으로만 보인다 — 2026-10-21 문제 4(저서)
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            if (double.TryParse(value.ToString(), out double d))
                            {
                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                insertPropertyInfo?.SetValue(insertModel, d);
                            }
                            else
                            {
                                sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                            }
                        }
                        else
                        {
                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                            insertPropertyInfo?.SetValue(insertModel, null);
                        }
                    }
                    else if (prop.Name == "IndicatorWeight")
                    {
                        //정수
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            if (int.TryParse(value.ToString(), out int i))
                            {
                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                insertPropertyInfo?.SetValue(insertModel, i);
                            }
                            else
                            {
                                sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                            }
                        }
                        else
                        {
                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                            insertPropertyInfo?.SetValue(insertModel, null);
                        }
                    }
                    else
                    {
                        //문자
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                            if (prop.Name == "Endangered1" || prop.Name == "Endangered2" || prop.Name == "Endemic")
                            {
                                insertPropertyInfo?.SetValue(insertModel, ((string)value).Replace(" ", ""));
                            }
                            else
                            {
                                insertPropertyInfo?.SetValue(insertModel, ((string)value).Trim());
                            }
                        }
                        else
                        {
                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                            insertPropertyInfo?.SetValue(insertModel, null);
                        }
                    }
                }

                if (sb.Length > 0)
                {
                    model.ErrorContent = sb.ToString();
                    insertModel.ErrorContent = sb.ToString();
                }

                converteds.Add(insertModel);
            }
            return (importModels, converteds);
        }
        catch (SqliteException ex)
        {
            MessageBox.Show("데이터베이스 또는 테이블을 찾을수 없습니다.", "데이터베이스 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            GB.LogWrite(GetType().Name, nameof(SpeciesDataValidation), ex.Message);
            return (importModels, converteds);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(SpeciesDataValidation), ex.Message);
            return (importModels, converteds);
        }
    }

    //DB에 같은 기록(대분류·지점·연도차수·조사일)이 있는지 찾고, 있으면 처리 방법을 묻는다 — 2026-10-21 문제 2·3
    //반환 null = 확인 실패(저장하지 않는다)
    private Dictionary<Benthos, DuplicateDecision>? FindDuplicates()
    {
        try
        {
            var decisions = new Dictionary<Benthos, DuplicateDecision>(ReferenceEqualityComparer.Instance);
            var project = _isEcology == true ? "생태현황" : "방류하천";
            var projectIds = GB.ProjectSiteNames.Where(sd => sd.Project == project).Select(sd => sd.SiteDivisionId).ToHashSet();

            using var context = new SDSMDbContext();
            var existing = context.Benthoss.AsNoTracking().IgnoreAutoIncludes()
                .Select(m => new { Id = m.BenthosId, m.YearChsu, m.Site, m.SurveyDate, m.SiteDivisionId })
                .ToList()
                .Where(m => m.SiteDivisionId is int id && projectIds.Contains(id))
                .ToList();

            var matches = ConvertedCollection
                .Select(model => (model, ids: existing
                    .Where(e => DuplicateImportHelper.Norm(e.YearChsu) == DuplicateImportHelper.Norm(model.YearChsu)
                             && DuplicateImportHelper.Norm(e.Site) == DuplicateImportHelper.Norm(model.Site)
                             && DuplicateImportHelper.Day(e.SurveyDate) == DuplicateImportHelper.Day(model.SurveyDate))
                    .Select(e => e.Id).ToList()))
                .Where(x => x.ids.Count > 0)
                .ToList();

            DuplicateAction? applyAll = null;
            for (int i = 0; i < matches.Count; i++)
            {
                var (model, ids) = matches[i];
                var speciesRows = ConvertedSpeciesCollection.Count(s => s.MacthNumber == model.MacthNumber);
                var oldSpeciesRows = context.BenthosSpeciess.Count(s => ids.Contains(s.BenthosId));
                var newText = $"[{project}] {model.Site} · {model.YearChsu} · {DuplicateImportHelper.DateText(model.SurveyDate)} · 종 {speciesRows}행";
                var existingText = $"{ids.Count}건 ({DuplicateImportHelper.IdText(ids)}) · 종 {oldSpeciesRows}행" + (ids.Count > 1 ? " — [바꾸기]는 모두 지웁니다" : "");
                var action = DuplicateImportHelper.Ask($"저서동물 — 같은 기록이 DB에 있습니다 ({i + 1}/{matches.Count})", newText, existingText, ref applyAll);
                decisions[model] = new DuplicateDecision(action, ids);
            }

            return decisions;
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(FindDuplicates), ex.Message);
            return null;
        }
    }

    private async Task<BulkSaveResult> AddSave(Dictionary<Benthos, DuplicateDecision> decisions)
    {
        int saved = 0, replaced = 0, skipped = 0, duplicateAdded = 0;

        try
        {
            GB.IsBusy = true;
            GB.ProgressValue = 0;
            GB.BusyContent = "자료 저장 중...";
            GB.IsIndeterminate = false;

            double progressTotal = ConvertedCount;
            double progressCount = 0;

            var repository = App.Current.Services.GetRequiredService<IBenthosRepository>();

            foreach (var model in ConvertedCollection)
            {
                progressCount++;
                GB.ProgressValue = (int)((progressCount / progressTotal) * 100);
                GB.BusyContent = $"자료 저장 중...{GB.ProgressValue}%";

                var macthNumber = model.MacthNumber;

                decisions.TryGetValue(model, out var decision);
                if (decision?.Action == DuplicateAction.Skip)
                {
                    skipped++;
                    continue;
                }
                if (decision?.Action == DuplicateAction.Replace)
                {
                    await Task.Run(() => ReplaceSave(model, decision.ExistingIds));
                    replaced++;
                    continue;
                }

                var addModel = await repository.InsertAsync(model);
                if (addModel != null)
                {
                    await Task.Run(() => SubBatchSave(addModel.BenthosId, macthNumber));
                    saved++;
                    if (decision?.Action == DuplicateAction.Add) duplicateAdded++;
                }
            }

            //목록 밖 종 — [목록에 추가]를 골랐을 때만 지표값 · 길드 없이(출현 목록) 더한다. 파일의 지표값은 넣지 않는다(2026-10-22 §22-3)
            if (_addOutOfListToList && saved + replaced > 0 && _speciesCheck.OutOfList.Count > 0)
            {
                using var context = new SDSMDbContext();
                var existing = SpeciesListValues.BenthosIndex(context.BenthosSpeciesLists.AsNoTracking().ToList());
                var newSpecies = _speciesCheck.OutOfList
                    .Where(kv => !existing.ContainsKey(kv.Key))
                    .Select(kv => SpeciesListValues.NewBenthosListRow(kv.Value))
                    .ToList();
                if (newSpecies.Count != 0)
                {
                    context.BenthosSpeciesLists.AddRange(newSpecies);
                    await context.SaveChangesAsync();
                }
            }

            return new BulkSaveResult(true, saved, replaced, skipped, duplicateAdded);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(AddSave), ex.Message);
            return BulkSaveResult.Fail;
        }
        finally
        {
            GB.IsBusy = false;
            GB.IsIndeterminate = true;
            GB.BusyContent = string.Empty;
            GB.ProgressValue = 0;
        }
    }

    //바꾸기 — 옛 기록(부모와 종 행)을 지우고 새 기록·종 행을 저장한다. 한 트랜잭션(실패하면 모두 되돌림) — 2026-10-21 문제 2·3
    private void ReplaceSave(Benthos model, IReadOnlyList<int> oldIds)
    {
        var ids = oldIds.ToArray();

        using var context = new SDSMDbContext();
        using var transaction = context.Database.BeginTransaction();

        try
        {
            context.BenthosSpeciess.Where(m => ids.Contains(m.BenthosId)).ExecuteDelete();
            context.Benthoss.IgnoreAutoIncludes().Where(m => ids.Contains(m.BenthosId)).ExecuteDelete();

            context.Benthoss.Add(model);
            context.SaveChanges();

            var species = ConvertedSpeciesCollection.Where(w => w.MacthNumber == model.MacthNumber).ToList();
            foreach (var item in species)
                item.BenthosId = model.BenthosId;

            context.BenthosSpeciess.AddRange(species);
            context.SaveChanges();

            transaction.Commit();
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(ReplaceSave), ex.Message);
            throw;
        }
    }

    private void SubBatchSave(int parentId, int macthNumber)
    {
        try
        {
            var addModels = ConvertedSpeciesCollection.Where(w => w.MacthNumber == macthNumber).ToList();

            if (addModels.Count == 0) return;

            string strConn = GB.SqliteDataSource;

            using var conn = new SQLiteConnection(strConn);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            foreach (var model in addModels)
            {
                string strSql = $"INSERT INTO BenthosSpecies" +
                                 $"(SaprobicValue, IndicatorWeight, Endangered1, Endangered2, Endemic, " +
                                 $"PhylumKo, ClassKo, OrderKo, FamilyKo, SpeciesKo, SpeciesEn, IndividualCount, BenthosId)  " +
                                 $"VALUES " +
                                 $"(@SaprobicValue, @IndicatorWeight, @Endangered1, @Endangered2, @Endemic, " +
                                 $"@PhylumKo, @ClassKo, @OrderKo, @FamilyKo, @SpeciesKo, @SpeciesEn, @IndividualCount, @BenthosId)";

                using var command = new SQLiteCommand(strSql, conn);

                command.Parameters.AddWithValue("@SaprobicValue", model.SaprobicValue ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IndicatorWeight", model.IndicatorWeight ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Endangered1", model.Endangered1 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Endangered2", model.Endangered2 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Endemic", model.Endemic ?? (object)DBNull.Value);
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

    //초기화
    private void Reset()
    {
        ModelCollection.Clear();
        ConvertedCollection.Clear();
        SpeciesCollection.Clear();
        ConvertedSpeciesCollection.Clear();

        ConvertedCount = 0;
        ErrorCount = 0;
        ConvertedSpeciesCount = 0;
        ErrorSpeciesCount = 0;
        SubstrateMovedCount = 0;

        _speciesCheck = new();
        _addOutOfListToList = false;
        ListValueDiffCount = 0;
        OutOfListCount = 0;
        OnPropertyChanged(nameof(ListValueDiffText));
    }

    //종 행 점검 알림 — 'n종 값 다름 → 종목록 값 사용' · '목록 밖 종 n종'(2026-10-22 §22-2 · §22-3)
    private string SpeciesNotice()
    {
        var parts = new List<string>();
        if (_speciesCheck.Differences.Count > 0) parts.Add($"{_speciesCheck.Differences.Count:N0}종 값 다름 → 종목록 값 사용");
        if (_speciesCheck.OutOfList.Count > 0) parts.Add($"목록 밖 종 {_speciesCheck.OutOfList.Count:N0}종(지표값 · 길드 비움)");
        return string.Join(" · ", parts);
    }

    private void GetColumnNumbers()
    {
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "연도차수", Value = 2 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "지점명", Value = 3 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "조사일", Value = 4 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "대권역명", Value = 5 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "중권역명", Value = 6 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "하천명", Value = 7 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "하천유형", Value = 8 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "위도", Value = 9 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "경도", Value = 10 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "날씨", Value = 11 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "조사기관", Value = 12 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "조사자", Value = 13 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "Surber net(30*30)", Value = 14 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "Surber net(50*50)", Value = 15 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "드렛지", Value = 16 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "에크만", Value = 17 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "유역이용", Value = 18 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "확인 가능 오염원", Value = 19 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "식생 수피도", Value = 20 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "범람원의 이용", Value = 21 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "제방(좌안)", Value = 22 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "제방(우안)", Value = 23 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "암반(빈칸 · 값은 특이사항으로)", Value = 24 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "콘크리트(빈칸 · 값은 특이사항으로)", Value = 25 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "진흙 이하(<0.063mm)", Value = 26 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "모래(0.063-2mm)", Value = 27 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "왕모래(양식 잔자갈 · 2-16mm)", Value = 28 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "자갈(16-64mm)", Value = 29 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "작은돌(64-256mm)", Value = 30 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "큰돌 이상(>256mm)", Value = 31 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "하천유형", Value = 33 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "하폭, 제방과 제방사이 거리(m)", Value = 34 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "수폭, 물이 흐르는 하도의 거리(m)", Value = 35 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "평균수심, 샘플링 지점 평균(cm)", Value = 36 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "평균유속, 샘플링 지점 평균(cm/s)", Value = 37 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "기온", Value = 38 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "수온", Value = 39 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "흐름상태", Value = 40 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "투명도", Value = 41 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "냄새", Value = 42 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "특이사항", Value = 43 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "채집불가 내용", Value = 44 });
    }

    #endregion

    #region 자료위치 찾기 및 등급계산

    /// <summary>
    /// 워크시트에서 특정 문자열이 포함된 첫 번째 셀의 위치(행, 열)를 반환합니다.
    /// 찾지 못하면 null을 반환합니다.
    /// </summary>
    private static List<ImportBenthosSpecies> GetSpeciesData(Worksheet worksheet, int columnIndex, int matchNumber, int speciesFirstRowNumber)
    {
        var speciess = new List<ImportBenthosSpecies>();

        var usedRange = worksheet.UsedCellRange;
        for (int rowIndex = speciesFirstRowNumber - 1; rowIndex <= usedRange.ToIndex.RowIndex; rowIndex++)
        {
            // 개체수는 실수 — 셀 표시 형식(양식 '#,##0.0'·'0.0'·'#,##0')이 아니라 일반 형식으로 읽는다.
            // 3.75 가 '3.8', 2.5 가 '3'(#,##0) 으로 읽히지 않게 — 지수(BMI·DI·H′·R1·J′)도 이 원값으로 계산된다 · 2026-10-21 §19-5
            var cellValue = ((ICellValue)(worksheet.Cells[rowIndex, columnIndex].GetValue().Value)).GetResultValueAsString(CellValueFormat.GeneralFormat);
            if (!string.IsNullOrWhiteSpace(cellValue) && double.TryParse(cellValue, out double d))
            {
                var species = new ImportBenthosSpecies
                {
                    MacthNumber = matchNumber,
                    SaprobicValue = ((ICellValue)(worksheet.Cells[rowIndex, 0].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 0].GetFormat().Value),
                    IndicatorWeight = ((ICellValue)(worksheet.Cells[rowIndex, 1].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 1].GetFormat().Value),
                    Endangered1 = ((ICellValue)(worksheet.Cells[rowIndex, 2].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 2].GetFormat().Value),
                    Endangered2 = ((ICellValue)(worksheet.Cells[rowIndex, 3].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 3].GetFormat().Value),
                    Endemic = ((ICellValue)(worksheet.Cells[rowIndex, 4].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 4].GetFormat().Value),
                    PhylumKo = ((ICellValue)(worksheet.Cells[rowIndex, 5].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 5].GetFormat().Value),
                    ClassKo = ((ICellValue)(worksheet.Cells[rowIndex, 6].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 6].GetFormat().Value),
                    OrderKo = ((ICellValue)(worksheet.Cells[rowIndex, 7].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 7].GetFormat().Value),
                    FamilyKo = ((ICellValue)(worksheet.Cells[rowIndex, 8].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 8].GetFormat().Value),
                    //옛 학명은 지침 표기로 읽는다(2026-10-22 §22-1 · SpeciesNameMap)
                    SpeciesEn = SpeciesNameMap.Benthos(((ICellValue)(worksheet.Cells[rowIndex, 9].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 9].GetFormat().Value)),
                    SpeciesKo = ((ICellValue)(worksheet.Cells[rowIndex, 10].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 10].GetFormat().Value),
                    IndividualCount = d,
                };

                speciess.Add(species);
            }
        }
        return speciess;
    }

    private void GetSavedFileInfos()
    {
        using var context = new SDSMDbContext();

        SavedFileInfos = [.. context.SavedFileInfos.Where(m => m.Division == "저서동물").OrderBy(m => m.SavedFileInfoId)];
    }

    #endregion
}
