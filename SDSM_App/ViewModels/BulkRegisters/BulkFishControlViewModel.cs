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
/// 일괄등록 어류
/// </summary>
public partial class BulkFishControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    private readonly (int, string)[] _weathers = [(1, "맑음"), (2, "흐림"), (3, "비(눈)")];
    private readonly (int, string)[] _collectionTools = [(1, "투망/족대")];
    private readonly (int, string)[] _flowStates = [(1, "여울"), (2, "흐름"), (3, "소")];
    private readonly (int, string)[] _habitatRiverTypes = [(1, "자연형"), (2, "직강화"), (3, "복합형"), (4, "댐/보/교각"), (5, "공사")];
    private readonly (int, string)[] _habitatFlowStates = [(1, "아주빠름"), (2, "빠름"), (3, "보통"), (4, "느림"), (5, "정체")];

    private string _loadedFileName = string.Empty;

    // null : 생태현황, 방류하천 없음
    // tryue : 생태현황 / false : 방류하천
    private bool? _isEcology = null;

    #endregion

    #region 생성자

    public BulkFishControlViewModel()
    {
        InitialLoad();
    }

    #endregion

    #region 속성

    //저장된 파일목록
    public List<SavedFileInfo> SavedFileInfos { get; set; } = [];

    //항목 열 번호 목록
    public RadObservableCollection<ColumnNumber> ColumnNumbers { get; set; } = [];

    public RadSpreadsheet DataSpreadsheet { get; set; } = new();

    [ObservableProperty] private RadObservableCollection<ImportFish> _modelCollection = [];
    [ObservableProperty] private RadObservableCollection<Fish> _convertedCollection = [];

    [ObservableProperty] private RadObservableCollection<ImportFishSpecies> _speciesCollection = [];
    [ObservableProperty] private RadObservableCollection<FishSpecies> _convertedSpeciesCollection = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DataImportExecutedCommand))]
    private string _sheetName = "어류_입력";

    [ObservableProperty] private int _firstRowNumber = 2;
    [ObservableProperty] private int _firstColumnNumber = 17;
    public string FirstColumnAlphabet { get; } = "Q";
    [ObservableProperty] private int _speciesFirstRowNumber = 79;

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

    #endregion

    #region 종목록 속성

    //기존 종목록
    public List<FishSpeciesList> SpeciesListData { get; set; } = [];

    // 엑셀에서 가져온 종목록
    public List<FishSpeciesList> ImportSpeciesListData { get; set; } = [];

    #endregion

    #region ICommand 구현

    [RelayCommand]
    private void SheetNameResetExecuted()
    {
        SheetName = "어류_입력";
    }

    private bool CanDataImport()
    {
        return !string.IsNullOrWhiteSpace(SheetName);
    }

    //엑셀 자료 가져오기
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

        if (savedFileInfo != null)
        {
            var message = $"파일명 : {savedFileInfo.FileName}{Environment.NewLine}" +
                           $"저장일 : {savedFileInfo.SavedTime:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                           $"저장된 기록이 있는 파일명입니다.{Environment.NewLine}{Environment.NewLine}" +
                           $"계속 진행하시겠습니까?";
            var result = MessageBox.Show(message, "저장된 기록이 있는 파일명", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);

            if (result == MessageBoxResult.Cancel)
            {
                return;
            }
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
                var (models, species) = (new List<ImportFish>(), new List<ImportFishSpecies>());

                (models, species) = await Task.Run(() => DataTableToModels(worksheet));

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

                    var speciesData = await Task.Run(() => SpeciesDataValidation(species));
                    SpeciesCollection.AddRange(speciesData.Item1);
                    ConvertedSpeciesCollection.AddRange(speciesData.Item2);

                    ConvertedSpeciesCount = SpeciesCollection.Count;
                    ErrorSpeciesCount = ConvertedSpeciesCollection.Count(m => m.ErrorContent.Length > 0);

                    //기존 종목록, 엑셀시트 종목록 가져오기
                    if (ErrorCount == 0 && ErrorSpeciesCount == 0)
                    {
                        using var context = new SDSMDbContext();

                        SpeciesListData = await context.FishSpeciesLists.ToListAsync();
                        ImportSpeciesListData = await Task.Run(() => GetImportSpeciesData(worksheet, SpeciesFirstRowNumber));
                    }
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

        //저장
        bool isSeccess = await Task.Run(() => AddSave());

        if (isSeccess)
        {
            //저장된 파일정보
            var repogitory = App.Current.Services.GetRequiredService<ISavedFileInfoRepository>();
            var savedFileInfo = new SavedFileInfo
            {
                Division = "어류",
                FileName = Path.GetFileName(_loadedFileName),
                SavedTime = DateTime.Now
            };

            var savedModel = await repogitory.InsertAsync(savedFileInfo);
            if (savedModel != null)
            {
                SavedFileInfos.Add(savedModel);
            }

            var toast = (GB.SaveMessage, true);
            WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

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

    //엑셀->모델 (서식수변 항목 미포함)
    private (List<ImportFish>, List<ImportFishSpecies>) DataTableToModels(Worksheet worksheet)
    {      
        var importModels = new List<ImportFish>();
        var importSpeciesModels = new List<ImportFishSpecies>();

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

                    var autoRiverChasu = ((ICellValue)(worksheet.Cells[16, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[16, columnIndex].GetFormat().Value);
                    var surveyUnavailableReason = ((ICellValue)(worksheet.Cells[28, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[28, columnIndex].GetFormat().Value);
                    var deCount = ((ICellValue)(worksheet.Cells[43, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[43, columnIndex].GetFormat().Value);
                    var efCount = ((ICellValue)(worksheet.Cells[44, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[44, columnIndex].GetFormat().Value);
                    var leCount = ((ICellValue)(worksheet.Cells[45, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[45, columnIndex].GetFormat().Value);
                    var tuCount = ((ICellValue)(worksheet.Cells[46, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[46, columnIndex].GetFormat().Value);

                    int chasu = int.TryParse(autoRiverChasu.ToString(), out int cs) ? cs : 0;

                    int abnormalCount = (int.TryParse(deCount.ToString(), out int de) ? de : 0)
                        + (int.TryParse(efCount.ToString(), out int ef) ? ef : 0)
                        + (int.TryParse(leCount.ToString(), out int le) ? le : 0)
                        + (int.TryParse(tuCount.ToString(), out int tu) ? tu : 0);

                    var (cluster, score, grade) = GB.GetFAI(speciesData, surveyUnavailableReason, abnormalCount, chasu);

                    var speciesCount = speciesData.GroupBy(m => m.SpeciesKo).Count();
                    var individualCountSum = speciesData.Sum(m => m.IndividualCount);

                    var model = new ImportFish
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
                        CollectionTime = ((ICellValue)(worksheet.Cells[13, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[13, columnIndex].GetFormat().Value),
                        CollectionTool = ((ICellValue)(worksheet.Cells[14, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[14, columnIndex].GetFormat().Value),
                        CollectionFlowState = ((ICellValue)(worksheet.Cells[15, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[15, columnIndex].GetFormat().Value),
                        AutoRiverChasu = autoRiverChasu,
                        Bedrock = ((ICellValue)(worksheet.Cells[17, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[17, columnIndex].GetFormat().Value),
                        Concrete = ((ICellValue)(worksheet.Cells[18, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[18, columnIndex].GetFormat().Value),
                        Mud = ((ICellValue)(worksheet.Cells[19, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[19, columnIndex].GetFormat().Value),
                        Sand = ((ICellValue)(worksheet.Cells[20, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[20, columnIndex].GetFormat().Value),
                        FineGravel = ((ICellValue)(worksheet.Cells[21, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[21, columnIndex].GetFormat().Value),
                        Gravel = ((ICellValue)(worksheet.Cells[22, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[22, columnIndex].GetFormat().Value),
                        SmallStone = ((ICellValue)(worksheet.Cells[23, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[23, columnIndex].GetFormat().Value),
                        BigStone = ((ICellValue)(worksheet.Cells[24, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[24, columnIndex].GetFormat().Value),
                        HabitatRiverType = ((ICellValue)(worksheet.Cells[26, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[26, columnIndex].GetFormat().Value),
                        HabitatFlowState = ((ICellValue)(worksheet.Cells[27, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[27, columnIndex].GetFormat().Value),
                        SurveyUnavailableReason = surveyUnavailableReason,
                        Note = ((ICellValue)(worksheet.Cells[29, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[29, columnIndex].GetFormat().Value),
                        DECount = deCount,
                        EFCount = efCount,
                        LECount = leCount,
                        TUCount = tuCount,
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
    private (List<ImportFish>, List<Fish>) DataValidation(List<ImportFish> importModels)
    {
        StringBuilder sb = new();
        var converteds = new List<Fish>();

        try
        {
            foreach (ImportFish model in importModels)
            {
                sb.Clear();

                var insertModel = new Fish();

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
                        || prop.Name == "FAIScore" || prop.Name == "FAIScore" || prop.Name == "TotalIndividualCount" || prop.Name == "TotalSpeciesCount" || prop.Name == "M1Value"
                        || prop.Name == "M2Value" || prop.Name == "M3Value" || prop.Name == "M4Value" || prop.Name == "M5Value" || prop.Name == "M6Value" || prop.Name == "M7Value"
                        || prop.Name == "FAIScore" || prop.Name == "FAIGrade" || prop.Name == "SpeciesCount" || prop.Name == "IndividualCount")
                    {
                        PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                        insertPropertyInfo?.SetValue(insertModel, value);
                    }
                    else if (prop.Name == "AutoRiverChasu")
                    {
                        //필수 정수
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
                            sb.Append(GB.GetImportErrorText(prop, ErrorType.Unique));
                        }
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

                                    if (year >= minDate.Year || year <= maxDate.Year)
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
                                var siteDivisionId = SiteDivisionHelper.FindSiteDivisionIdBySiteContains(_isEcology, v);
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
                    else if (prop.Name == "CollectionTime"
                            || prop.Name == "Bedrock" || prop.Name == "Concrete" || prop.Name == "Mud" || prop.Name == "Sand" || prop.Name == "FineGravel"
                            || prop.Name == "Gravel" || prop.Name == "SmallStone" || prop.Name == "BigStone"
                            || prop.Name == "DECount" || prop.Name == "EFCount" || prop.Name == "LECount" || prop.Name == "TUCount")
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
                    else if (prop.Name == "CollectionTool" || prop.Name == "CollectionFlowState" || prop.Name == "HabitatRiverType" || prop.Name == "HabitatFlowState")
                    {
                        //목록자료
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            if (prop.Name == "CollectionTool")
                            {
                                var items = ((string)value).Replace(" ", "").Split([',', '.'], StringSplitOptions.RemoveEmptyEntries);
                                var itemNames = new List<string>();

                                foreach (var item in items)
                                {
                                    if (int.TryParse(item, out int num))
                                    {
                                        var name = _collectionTools.FirstOrDefault(w => w.Item1 == num).Item2;
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
                            else if (prop.Name == "CollectionFlowState")
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
                            else if (prop.Name == "HabitatFlowState")
                            {
                                var items = ((string)value).Replace(" ", "").Split([',', '.'], StringSplitOptions.RemoveEmptyEntries);
                                var itemNames = new List<string>();

                                foreach (var item in items)
                                {
                                    if (int.TryParse(item, out int num))
                                    {
                                        var name = _habitatFlowStates.FirstOrDefault(w => w.Item1 == num).Item2;
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

                            if (prop.Name == "River" || prop.Name == "RiverType" || prop.Name == "SurveyUnavailableReason")
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
            GB.LogWrite(GetType().Name, nameof(DataValidation), ex.Message);
            return (importModels, converteds);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(DataValidation), ex.Message);
            return (importModels, converteds);
        }
    }

    //종목록 검수하기
    private (List<ImportFishSpecies>, List<FishSpecies>) SpeciesDataValidation(List<ImportFishSpecies> importModels)
    {
        StringBuilder sb = new();
        var converteds = new List<FishSpecies>();

        try
        {
            foreach (ImportFishSpecies model in importModels)
            {
                sb.Clear();

                var insertModel = new FishSpecies();

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
                    else if (prop.Name == "LineageOrder" || prop.Name == "IndividualCount")
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
                    else if (prop.Name == "FishTrait" || prop.Name == "ToleranceGuild" || prop.Name == "FeedingGuild"
                             || prop.Name == "HabitatGuild" || prop.Name == "Exotic" || prop.Name == "Endemic"
                             || prop.Name == "Endangered1" || prop.Name == "Endangered2" || prop.Name == "NaturalMonument")
                    {
                        //공백제거 및 대문자로
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                            insertPropertyInfo?.SetValue(insertModel, ((string)value).Replace(" ", "").ToUpper());
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
                            insertPropertyInfo?.SetValue(insertModel, ((string)value).Trim());
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

    //저장
    private async Task<bool> AddSave()
    {
        try
        {
            GB.IsBusy = true;
            GB.ProgressValue = 0;
            GB.BusyContent = "자료 저장 중...";
            GB.IsIndeterminate = false;

            double progressTotal = ConvertedCount;
            double progressCount = 0;

            var repository = App.Current.Services.GetRequiredService<IFishRepository>();

            foreach (var model in ConvertedCollection)
            {
                progressCount++;
                GB.ProgressValue = (int)((progressCount / progressTotal) * 100);
                GB.BusyContent = $"자료 저장 중...{GB.ProgressValue}%";

                var macthNumber = model.MacthNumber;

                var addModel = await repository.InsertAsync(model);
                if (addModel != null)
                {
                    await Task.Run(() => SubBatchSave(addModel.FishId, macthNumber));
                }
            }

            //기존 종목록에 없는 종목록 추가
            var newSpecies = GetNewSpeciesData();
            if (newSpecies.Count != 0)
            {
                using var context = new SDSMDbContext();
                context.FishSpeciesLists.AddRange(newSpecies);
                await context.SaveChangesAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(AddSave), ex.Message);
            return false;
        }
        finally
        {
            GB.IsBusy = false;
            GB.IsIndeterminate = true;
            GB.BusyContent = string.Empty;
            GB.ProgressValue = 0;
        }
    }

    //하위 저장
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
                string strSql = $"INSERT INTO FishSpecies" +
                                 $"(FishTrait, ToleranceGuild, FeedingGuild, HabitatGuild, Exotic, " +
                                 $"Endemic, Endangered1, Endangered2, NaturalMonument, LineageOrder, " +
                                 $"ClassKo, OrderKo, FamilyKo, SpeciesKo, SpeciesEn, IndividualCount, FishId) " +
                                 $"VALUES " +
                                 $"(@FishTrait, @ToleranceGuild, @FeedingGuild, @HabitatGuild, @Exotic, " +
                                 $"@Endemic, @Endangered1, @Endangered2, @NaturalMonument, @LineageOrder, " +
                                 $"@ClassKo, @OrderKo, @FamilyKo, @SpeciesKo, @SpeciesEn, @IndividualCount, @FishId)";

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

        SpeciesListData.Clear();
        ImportSpeciesListData.Clear();
    }

    private void GetColumnNumbers()
    {
        ColumnNumbers.Clear();

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
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "채집소요시간(분)", Value = 14 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "채집도구", Value = 15 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "흐름상태", Value = 16 });

        ColumnNumbers.Add(new ColumnNumber { ColumnName = "암반", Value = 18 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "콘크리트", Value = 19 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "진흙이하(<0.063mm)", Value = 20 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "모래(0.063-2mm)", Value = 21 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "잔자갈(2-16mm)", Value = 22 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "자갈(16-64mm)", Value = 23 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "작은돌(64-256mm)", Value = 24 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "큰돌(>256mm)", Value = 25 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "하천형태", Value = 27 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "흐름상태", Value = 28 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "채집불가시 특이사항", Value = 29 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "채집불가시 비고", Value = 30 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "기형", Value = 44 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "지느러미손상", Value = 45 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "피부손상", Value = 46 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "종양", Value = 47 });
    }

    #endregion

    #region 자료위치 찾기 및 등급계산

    /// <summary>
    /// 워크시트에서 특정 문자열이 포함된 첫 번째 셀의 위치(행, 열)를 반환합니다.
    /// 찾지 못하면 null을 반환합니다.
    /// </summary>
    private static List<ImportFishSpecies> GetSpeciesData(Worksheet worksheet, int columnIndex, int matchNumber, int speciesFirshtNumber)
    {
        var speciess = new List<ImportFishSpecies>();

        var usedRange = worksheet.UsedCellRange;
        for (int rowIndex = speciesFirshtNumber - 1; rowIndex <= usedRange.ToIndex.RowIndex; rowIndex++)
        {
            // 개체수는 정수
            var cellValue = ((ICellValue)(worksheet.Cells[rowIndex, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, columnIndex].GetFormat().Value);
            if (!string.IsNullOrWhiteSpace(cellValue) && int.TryParse(cellValue, out int i))
            {
                var species = new ImportFishSpecies
                {
                    MacthNumber = matchNumber,
                    FishTrait = ((ICellValue)(worksheet.Cells[rowIndex, 0].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 0].GetFormat().Value),
                    ToleranceGuild = ((ICellValue)(worksheet.Cells[rowIndex, 1].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 1].GetFormat().Value),
                    FeedingGuild = ((ICellValue)(worksheet.Cells[rowIndex, 2].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 2].GetFormat().Value),
                    HabitatGuild = ((ICellValue)(worksheet.Cells[rowIndex, 3].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 3].GetFormat().Value),
                    Exotic = ((ICellValue)(worksheet.Cells[rowIndex, 4].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 4].GetFormat().Value),
                    Endemic = ((ICellValue)(worksheet.Cells[rowIndex, 5].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 5].GetFormat().Value),
                    Endangered1 = ((ICellValue)(worksheet.Cells[rowIndex, 6].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 6].GetFormat().Value),
                    Endangered2 = ((ICellValue)(worksheet.Cells[rowIndex, 7].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 7].GetFormat().Value),
                    NaturalMonument = ((ICellValue)(worksheet.Cells[rowIndex, 8].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 8].GetFormat().Value),
                    LineageOrder = ((ICellValue)(worksheet.Cells[rowIndex, 9].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 9].GetFormat().Value),
                    ClassKo = ((ICellValue)(worksheet.Cells[rowIndex, 10].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 10].GetFormat().Value),
                    OrderKo = ((ICellValue)(worksheet.Cells[rowIndex, 11].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 11].GetFormat().Value),
                    FamilyKo = ((ICellValue)(worksheet.Cells[rowIndex, 12].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 12].GetFormat().Value),
                    SpeciesKo = ((ICellValue)(worksheet.Cells[rowIndex, 13].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 13].GetFormat().Value),
                    SpeciesEn = ((ICellValue)(worksheet.Cells[rowIndex, 14].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 14].GetFormat().Value),
                    IndividualCount = i,
                };

                speciess.Add(species);
            }
        }
        return speciess;
    }

    private void GetSavedFileInfos()
    {
        using var context = new SDSMDbContext();

        SavedFileInfos = [.. context.SavedFileInfos.Where(m => m.Division == "어류").OrderBy(m => m.SavedFileInfoId)];
    }

    #endregion

    #region 엑셀시트에서 가져온 종목록

    private List<FishSpeciesList> GetImportSpeciesData(Worksheet worksheet, int speciesFirstRowNumber)
    {
        var data = new List<FishSpeciesList>();

        try
        {
            var usedRange = worksheet.UsedCellRange;
            for (int rowIndex = speciesFirstRowNumber - 1; rowIndex <= usedRange.ToIndex.RowIndex; rowIndex++)
            {
                int? lineageOrder = int.TryParse(((ICellValue)(worksheet.Cells[rowIndex, 9].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 9].GetFormat().Value), out int lineageOrderResult) ? lineageOrderResult : null;
                string? speciesKo = ((ICellValue)(worksheet.Cells[rowIndex, 13].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 13].GetFormat().Value);
                string? speciesEn = ((ICellValue)(worksheet.Cells[rowIndex, 14].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 14].GetFormat().Value);

                if (!string.IsNullOrWhiteSpace(speciesEn) && !string.IsNullOrWhiteSpace(speciesKo))
                {
                    var species = new FishSpeciesList
                    {
                        FishTrait = ((ICellValue)(worksheet.Cells[rowIndex, 0].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 0].GetFormat().Value),
                        ToleranceGuild = ((ICellValue)(worksheet.Cells[rowIndex, 1].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 1].GetFormat().Value),
                        FeedingGuild = ((ICellValue)(worksheet.Cells[rowIndex, 2].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 2].GetFormat().Value),
                        HabitatGuild = ((ICellValue)(worksheet.Cells[rowIndex, 3].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 3].GetFormat().Value),
                        Exotic = ((ICellValue)(worksheet.Cells[rowIndex, 4].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 4].GetFormat().Value),
                        Endemic = ((ICellValue)(worksheet.Cells[rowIndex, 5].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 5].GetFormat().Value),
                        Endangered1 = ((ICellValue)(worksheet.Cells[rowIndex, 6].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 6].GetFormat().Value),
                        Endangered2 = ((ICellValue)(worksheet.Cells[rowIndex, 7].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 7].GetFormat().Value),
                        NaturalMonument = ((ICellValue)(worksheet.Cells[rowIndex, 8].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 8].GetFormat().Value),
                        LineageOrder = lineageOrder,
                        ClassKo = ((ICellValue)(worksheet.Cells[rowIndex, 10].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 10].GetFormat().Value),
                        OrderKo = ((ICellValue)(worksheet.Cells[rowIndex, 11].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 11].GetFormat().Value),
                        FamilyKo = ((ICellValue)(worksheet.Cells[rowIndex, 12].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 12].GetFormat().Value),
                        SpeciesKo = speciesKo,
                        SpeciesEn = speciesEn,
                    };

                    data.Add(species);
                }
            }

            return data;
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(GetImportSpeciesData), ex.Message);
            return data;
        }
    }

    private List<FishSpeciesList> GetNewSpeciesData()
    {
        // SpeciesKo, SpeciesEn 기준으로 직접 비교
        var newSpecies = ImportSpeciesListData
            .Where(import =>
                !SpeciesListData.Any(existing =>
                    import.SpeciesKo?.Replace(" ", "").ToLower() == existing.SpeciesKo?.Replace(" ", "").ToLower() &&
                    import.SpeciesEn?.Replace(" ", "").ToLower() == existing.SpeciesEn?.Replace(" ", "").ToLower()
                )
            )
            .ToList();

        return newSpecies;
    }

    #endregion
}
