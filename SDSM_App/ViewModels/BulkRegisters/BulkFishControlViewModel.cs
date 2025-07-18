using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SDSM_App.Enums;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.Models;
using SDSM_App.Models.Repositories.Interfases;
using SDSM_Models;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SQLite;
using System.Diagnostics;
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

    #endregion

    #region 생성자

    public BulkFishControlViewModel()
    {
        GetColumnNumbers();
    }

    #endregion

    #region 속성

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
    [ObservableProperty] private int _speciesFirstRowNumver = 79;
    [ObservableProperty] private bool _isContainHabitat;

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

    #region ICommand 구현

    [RelayCommand]
    private void SheetNameResetExecuted()
    {
        SheetName = "어류_입력";
    }

    [RelayCommand]
    private void ContainHabitatExecuted()
    {
        if (IsContainHabitat == false)
        {
            SpeciesFirstRowNumver = 79;
        }
        else
        {
            SpeciesFirstRowNumver = 103;
        }
        GetColumnNumbers();
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
                string success = "성공";
                string fail = "실패";

                var message = $"엑셀 시트명{Environment.NewLine}" +
                              $"조사자료 : {(isExist ? success : fail)}{Environment.NewLine}";

                MessageBox.Show(message, "엑셀 시트명 오류", MessageBoxButton.OK, MessageBoxImage.Warning);

                GB.IsBusy = false;
                GB.BusyContent = string.Empty;

                return;
            }

            //엑셀 -> 모델
            if (worksheet != null && worksheet.Rows.Count > 0)
            {
                var (models, species) = (new List<ImportFish>(), new List<ImportFishSpecies>());

                if(IsContainHabitat == false)
                {
                    (models, species) = await Task.Run(() => DataTableToModels(worksheet));

                }
                else
                {
                    (models, species) = await Task.Run(() => ContainHabitatDataTableToModels(worksheet));
                }

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

    //엑셀->모델 (서식수변 항목 미포함)
    private (List<ImportFish>, List<ImportFishSpecies>) DataTableToModels(Worksheet worksheet)
    {
        var importModels = new List<ImportFish>();
        var importSpeciesModels = new List<ImportFishSpecies>();

        try
        {
            CellRange usedCellRange = worksheet.UsedCellRange;
            int matchNumber = 0;
            int speciesColumnNumber = SpeciesFirstRowNumver;

            for (int columnIndex = FirstColumnNumber - 1; columnIndex <= usedCellRange.ToIndex.ColumnIndex; columnIndex++)
            {
                var firstValue = worksheet.Cells[FirstRowNumber - 1, columnIndex].GetValue().Value.RawValue;
                if (!string.IsNullOrWhiteSpace(firstValue))
                {
                    matchNumber++;

                    //종목록
                    var speciesData = GetSpeciesData(worksheet, columnIndex, matchNumber, speciesColumnNumber);

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

                    var autoRiverChasu = worksheet.Cells[16, columnIndex].GetValue().Value.RawValue;
                    var surveyUnavailableReason = worksheet.Cells[28, columnIndex].GetValue().Value.RawValue;
                    var deCount = worksheet.Cells[43, columnIndex].GetValue().Value.RawValue;
                    var efCount = worksheet.Cells[44, columnIndex].GetValue().Value.RawValue;
                    var leCount = worksheet.Cells[45, columnIndex].GetValue().Value.RawValue;
                    var tuCount = worksheet.Cells[46, columnIndex].GetValue().Value.RawValue;

                    int chasu = int.TryParse(autoRiverChasu.ToString(), out int cs) ? cs : 0;

                    int abnormalCount = (int.TryParse(deCount.ToString(), out int de) ? de : 0)
                        + (int.TryParse(efCount.ToString(), out int ef) ? ef : 0)
                        + (int.TryParse(leCount.ToString(), out int le) ? le : 0)
                        + (int.TryParse(tuCount.ToString(), out int tu) ? tu : 0);

                    var (score, grade) = GB.GetFAI(speciesData, surveyUnavailableReason, abnormalCount, chasu);

                    var speciesCount = speciesData.GroupBy(m => m.SpeciesKo).Count();
                    var individualCountSum = speciesData.Sum(m => m.IndividualCount);

                    var model = new ImportFish
                    {
                        MacthNumber = matchNumber,
                        YearChsu = firstValue,
                        Site = worksheet.Cells[2, columnIndex].GetValue().Value.RawValue,
                        SurveyDate = ((ICellValue)(worksheet.Cells[3, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[3, columnIndex].GetFormat().Value),
                        MajorRegion = worksheet.Cells[4, columnIndex].GetValue().Value.RawValue,
                        MiddleRegion = worksheet.Cells[5, columnIndex].GetValue().Value.RawValue,
                        River = worksheet.Cells[6, columnIndex].GetValue().Value.RawValue,
                        RiverType = worksheet.Cells[7, columnIndex].GetValue().Value.RawValue,
                        Lat = worksheet.Cells[8, columnIndex].GetValue().Value.RawValue,
                        Lng = worksheet.Cells[9, columnIndex].GetValue().Value.RawValue,
                        Weather = worksheet.Cells[10, columnIndex].GetValue().Value.RawValue,
                        SurveyAgency = worksheet.Cells[11, columnIndex].GetValue().Value.RawValue,
                        Surveyor = worksheet.Cells[12, columnIndex].GetValue().Value.RawValue,
                        CollectionTime = worksheet.Cells[13, columnIndex].GetValue().Value.RawValue,
                        CollectionTool = worksheet.Cells[14, columnIndex].GetValue().Value.RawValue,
                        CollectionFlowState = worksheet.Cells[15, columnIndex].GetValue().Value.RawValue,
                        AutoRiverChasu = autoRiverChasu,
                        Bedrock = worksheet.Cells[17, columnIndex].GetValue().Value.RawValue,
                        Concrete = worksheet.Cells[18, columnIndex].GetValue().Value.RawValue,
                        Mud = worksheet.Cells[19, columnIndex].GetValue().Value.RawValue,
                        Sand = worksheet.Cells[20, columnIndex].GetValue().Value.RawValue,
                        FineGravel = worksheet.Cells[21, columnIndex].GetValue().Value.RawValue,
                        Gravel = worksheet.Cells[22, columnIndex].GetValue().Value.RawValue,
                        SmallStone = worksheet.Cells[23, columnIndex].GetValue().Value.RawValue,
                        BigStone = worksheet.Cells[24, columnIndex].GetValue().Value.RawValue,
                        HabitatRiverType = worksheet.Cells[26, columnIndex].GetValue().Value.RawValue,
                        HabitatFlowState = worksheet.Cells[27, columnIndex].GetValue().Value.RawValue,
                        SurveyUnavailableReason = surveyUnavailableReason,
                        Note = worksheet.Cells[29, columnIndex].GetValue().Value.RawValue,
                        DECount = deCount,
                        EFCount = efCount,
                        LECount = leCount,
                        TUCount = tuCount,
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

    //엑셀->모델 (서식수변 항목 포함)
    private (List<ImportFish>, List<ImportFishSpecies>) ContainHabitatDataTableToModels(Worksheet worksheet)
    {
        var importModels = new List<ImportFish>();
        var importSpeciesModels = new List<ImportFishSpecies>();

        try
        {
            CellRange usedCellRange = worksheet.UsedCellRange;
            int matchNumber = 0;
            int speciesColumnNumber = SpeciesFirstRowNumver;

            for (int columnIndex = FirstColumnNumber - 1; columnIndex <= usedCellRange.ToIndex.ColumnIndex; columnIndex++)
            {
                var firstValue = worksheet.Cells[FirstRowNumber - 1, columnIndex].GetValue().Value.RawValue;
                if (!string.IsNullOrWhiteSpace(firstValue))
                {
                    matchNumber++;

                    //종목록
                    var speciesData = GetSpeciesData(worksheet, columnIndex, matchNumber, speciesColumnNumber);

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

                    var autoRiverChasu = worksheet.Cells[16, columnIndex].GetValue().Value.RawValue;
                    var surveyUnavailableReason = worksheet.Cells[52, columnIndex].GetValue().Value.RawValue;
                    var deCount = worksheet.Cells[67, columnIndex].GetValue().Value.RawValue;
                    var efCount = worksheet.Cells[68, columnIndex].GetValue().Value.RawValue;
                    var leCount = worksheet.Cells[69, columnIndex].GetValue().Value.RawValue;
                    var tuCount = worksheet.Cells[70, columnIndex].GetValue().Value.RawValue;

                    int chasu = int.TryParse(autoRiverChasu.ToString(), out int cs) ? cs : 0;

                    int abnormalCount = (int.TryParse(deCount.ToString(), out int de) ? de : 0)
                        + (int.TryParse(efCount.ToString(), out int ef) ? ef : 0)
                        + (int.TryParse(leCount.ToString(), out int le) ? le : 0)
                        + (int.TryParse(tuCount.ToString(), out int tu) ? tu : 0);

                    var (score, grade) = GB.GetFAI(speciesData, surveyUnavailableReason, abnormalCount, chasu);

                    var speciesCount = speciesData.GroupBy(m => m.SpeciesKo).Count();
                    var individualCountSum = speciesData.Sum(m => m.IndividualCount);

                    var model = new ImportFish
                    {
                        MacthNumber = matchNumber,
                        YearChsu = firstValue,
                        Site = worksheet.Cells[2, columnIndex].GetValue().Value.RawValue,
                        SurveyDate = worksheet.Cells[3, columnIndex].GetValue().Value.RawValue,
                        MajorRegion = worksheet.Cells[4, columnIndex].GetValue().Value.RawValue,
                        MiddleRegion = worksheet.Cells[5, columnIndex].GetValue().Value.RawValue,
                        River = worksheet.Cells[6, columnIndex].GetValue().Value.RawValue,
                        RiverType = worksheet.Cells[7, columnIndex].GetValue().Value.RawValue,
                        Lat = worksheet.Cells[8, columnIndex].GetValue().Value.RawValue,
                        Lng = worksheet.Cells[9, columnIndex].GetValue().Value.RawValue,
                        Weather = worksheet.Cells[10, columnIndex].GetValue().Value.RawValue,
                        SurveyAgency = worksheet.Cells[11, columnIndex].GetValue().Value.RawValue,
                        Surveyor = worksheet.Cells[12, columnIndex].GetValue().Value.RawValue,
                        CollectionTime = worksheet.Cells[13, columnIndex].GetValue().Value.RawValue,
                        CollectionTool = worksheet.Cells[14, columnIndex].GetValue().Value.RawValue,
                        CollectionFlowState = worksheet.Cells[15, columnIndex].GetValue().Value.RawValue,
                        AutoRiverChasu = autoRiverChasu,

                        WatershedType = worksheet.Cells[17, columnIndex].GetValue().Value.RawValue,
                        DrainageSystemType = worksheet.Cells[18, columnIndex].GetValue().Value.RawValue,
                        RiverWidth = worksheet.Cells[19, columnIndex].GetValue().Value.RawValue,
                        YuWidth = worksheet.Cells[20, columnIndex].GetValue().Value.RawValue,
                        Depth = worksheet.Cells[21, columnIndex].GetValue().Value.RawValue,
                        Submerged = worksheet.Cells[22, columnIndex].GetValue().Value.RawValue,
                        Floating = worksheet.Cells[23, columnIndex].GetValue().Value.RawValue,
                        Emergent = worksheet.Cells[24, columnIndex].GetValue().Value.RawValue,
                        LeftVacantLand = worksheet.Cells[25, columnIndex].GetValue().Value.RawValue,
                        LeftH = worksheet.Cells[26, columnIndex].GetValue().Value.RawValue,
                        LeftS = worksheet.Cells[27, columnIndex].GetValue().Value.RawValue,
                        LeftT1 = worksheet.Cells[28, columnIndex].GetValue().Value.RawValue,
                        RightVacantLand = worksheet.Cells[30, columnIndex].GetValue().Value.RawValue,
                        RightH = worksheet.Cells[31, columnIndex].GetValue().Value.RawValue,
                        RightS = worksheet.Cells[32, columnIndex].GetValue().Value.RawValue,
                        RightT1 = worksheet.Cells[33, columnIndex].GetValue().Value.RawValue,
                        LeveeLeft = worksheet.Cells[35, columnIndex].GetValue().Value.RawValue,
                        LeveeRight = worksheet.Cells[36, columnIndex].GetValue().Value.RawValue,
                        Confounder = worksheet.Cells[37, columnIndex].GetValue().Value.RawValue,
                        Structure = worksheet.Cells[38, columnIndex].GetValue().Value.RawValue,
                        Fishway = worksheet.Cells[39, columnIndex].GetValue().Value.RawValue,
                        EtcNote = worksheet.Cells[40, columnIndex].GetValue().Value.RawValue,

                        Bedrock = worksheet.Cells[41, columnIndex].GetValue().Value.RawValue,
                        Concrete = worksheet.Cells[42, columnIndex].GetValue().Value.RawValue,
                        Mud = worksheet.Cells[43, columnIndex].GetValue().Value.RawValue,
                        Sand = worksheet.Cells[44, columnIndex].GetValue().Value.RawValue,
                        FineGravel = worksheet.Cells[45, columnIndex].GetValue().Value.RawValue,
                        Gravel = worksheet.Cells[46, columnIndex].GetValue().Value.RawValue,
                        SmallStone = worksheet.Cells[47, columnIndex].GetValue().Value.RawValue,
                        BigStone = worksheet.Cells[48, columnIndex].GetValue().Value.RawValue,
                        HabitatRiverType = worksheet.Cells[50, columnIndex].GetValue().Value.RawValue,
                        HabitatFlowState = worksheet.Cells[51, columnIndex].GetValue().Value.RawValue,
                        SurveyUnavailableReason = surveyUnavailableReason,
                        Note = worksheet.Cells[53, columnIndex].GetValue().Value.RawValue,
                        DECount = deCount,
                        EFCount = efCount,
                        LECount = leCount,
                        TUCount = tuCount,
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
            GB.LogWrite(GetType().Name, nameof(ContainHabitatDataTableToModels), ex.Message);

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

                //다시 검수 때문에 초기화
                model.ErrorContent = string.Empty;

                //import model
                PropertyInfo[] props = model.GetType().GetProperties();

                foreach (var prop in props)
                {
                    if (prop.Name == "ErrorContent") continue;

                    var value = prop.GetValue(model);

                    if (prop.Name == "MacthNumber" || prop.Name == "FAIScore" || prop.Name == "FAIGrade" || prop.Name == "SpeciesCount" || prop.Name == "IndividualCount")
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
                    else if (prop.Name == "YearChsu" || prop.Name == "Side")
                    {
                        //필수 문자
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            if (((string)value).Replace(" ", "").Length > 0)
                            {
                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                insertPropertyInfo?.SetValue(insertModel, ((string)value).Trim());
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
                    else if (prop.Name == "SurveyDate")
                    {
                        //형식은 Null - DB입력은 필수 날짜
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            // 날짜 범위 체크
                            var minDate = GB.MinDate;
                            var maxDate = DateTime.Today;

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
                            || prop.Name == "DECount" || prop.Name == "EFCount" || prop.Name == "LECount" || prop.Name == "TUCount"
                            || prop.Name == "LeftVacantLand" || prop.Name == "LeftH" || prop.Name == "LeftS" || prop.Name == "LeftT1"
                            || prop.Name == "RightVacantLand" || prop.Name == "RightH" || prop.Name == "RightS" || prop.Name == "RightT1")
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

        if(IsContainHabitat == false)
        {
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
        else
        {            
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "유역형태", Value = 18 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "수계형태", Value = 19 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "하폭(m)", Value = 20 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "유폭(m)", Value = 21 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "수심(m)", Value = 22 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "침수성", Value = 23 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "부수성", Value = 24 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "정수성", Value = 25 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "좌안 나지", Value = 26 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "좌안 초본류", Value = 27 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "좌안 관목류", Value = 28 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "좌안 교목류", Value = 29 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "우안 나지", Value = 31 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "우안 초본류", Value = 32 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "우안 관목류", Value = 33 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "우안 교목류", Value = 34 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "좌안", Value = 36 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "우안", Value = 37 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "교란요인", Value = 38 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "하천내 구조물", Value = 39 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "어도 유무", Value = 40 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "기타 특이사항", Value = 41 });

            ColumnNumbers.Add(new ColumnNumber { ColumnName = "암반", Value = 42 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "콘크리트", Value = 43 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "진흙이하(<0.063mm)", Value = 44 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "모래(0.063-2mm)", Value = 45 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "잔자갈(2-16mm)", Value = 46 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "자갈(16-64mm)", Value = 47 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "작은돌(64-256mm)", Value = 48 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "큰돌(>256mm)", Value = 49 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "하천형태", Value = 51 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "흐름상태", Value = 52 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "채집불가시 특이사항", Value = 53 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "채집불가시 비고", Value = 54 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "기형", Value = 68 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "지느러미손상", Value = 69 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "피부손상", Value = 70 });
            ColumnNumbers.Add(new ColumnNumber { ColumnName = "종양", Value = 71 });
        }
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
        for (int rowIndex = speciesFirshtNumber-1; rowIndex <= usedRange.ToIndex.RowIndex; rowIndex++)
        {
            // 개체수는 정수
            var cellValue = worksheet.Cells[rowIndex, columnIndex].GetValue().Value.RawValue;
            if (!string.IsNullOrWhiteSpace(cellValue) && int.TryParse(cellValue, out int i))
            {
                var species = new ImportFishSpecies
                {
                    MacthNumber = matchNumber,
                    FishTrait = worksheet.Cells[rowIndex, 0].GetValue().Value.RawValue,
                    ToleranceGuild = worksheet.Cells[rowIndex, 1].GetValue().Value.RawValue,
                    FeedingGuild = worksheet.Cells[rowIndex, 2].GetValue().Value.RawValue,
                    HabitatGuild = worksheet.Cells[rowIndex, 3].GetValue().Value.RawValue,
                    Exotic = worksheet.Cells[rowIndex, 4].GetValue().Value.RawValue,
                    Endemic = worksheet.Cells[rowIndex, 5].GetValue().Value.RawValue,
                    Endangered1 = worksheet.Cells[rowIndex, 6].GetValue().Value.RawValue,
                    Endangered2 = worksheet.Cells[rowIndex, 7].GetValue().Value.RawValue,
                    NaturalMonument = worksheet.Cells[rowIndex, 8].GetValue().Value.RawValue,
                    LineageOrder = worksheet.Cells[rowIndex, 9].GetValue().Value.RawValue,
                    ClassKo = worksheet.Cells[rowIndex, 10].GetValue().Value.RawValue,
                    OrderKo = worksheet.Cells[rowIndex, 11].GetValue().Value.RawValue,
                    FamilyKo = worksheet.Cells[rowIndex, 12].GetValue().Value.RawValue,
                    SpeciesKo = worksheet.Cells[rowIndex, 13].GetValue().Value.RawValue,
                    SpeciesEn = worksheet.Cells[rowIndex, 14].GetValue().Value.RawValue,
                    IndividualCount = i,
                };

                speciess.Add(species);
            }
        }
        return speciess;
    }

    #endregion
}
