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
    private readonly (int, string)[] _habitatRiverTypes = [(1, "산강형"), (2, "평지형"), (3, "강"), (4, "인공구조물")];
    private readonly (int, string)[] _flowStates = [(1, "여울"), (2, "흐름"), (3, "소")];

    #endregion

    #region 생성자

    public BulkBenthosControlViewModel()
    {
        GetColumnNumbers();
    }

    #endregion

    #region 속성

    //항목 열 번호 목록
    public List<ColumnNumber> ColumnNumbers { get; set; } = [];

    public RadSpreadsheet DataSpreadsheet { get; set; } = new();

    [ObservableProperty] private RadObservableCollection<ImportBenthos> _modelCollection = [];
    [ObservableProperty] private RadObservableCollection<Benthos> _convertedCollection = [];

    [ObservableProperty] private RadObservableCollection<ImportBenthosSpecies> _speciesCollection = [];
    [ObservableProperty] private RadObservableCollection<BenthosSpecies> _convertedSpeciesCollection = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DataImportExecutedCommand))]
    private string _sheetName = "입력";

    [ObservableProperty] private int _firstRowNumber = 2;
    [ObservableProperty] private int _firstColumnNumber = 13;

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
                var (models, species) = await Task.Run(() => DataTableToModels(worksheet));
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

    //엑셀->모델
    private (List<ImportBenthos>, List<ImportBenthosSpecies>) DataTableToModels(Worksheet worksheet)
    {
        var importModels = new List<ImportBenthos>();
        var importSpeciesModels = new List<ImportBenthosSpecies>();

        try
        {
            CellRange usedCellRange = worksheet.UsedCellRange;
            int matchNumber = 0;

            for (int columnIndex = FirstColumnNumber - 1; columnIndex <= usedCellRange.ToIndex.ColumnIndex; columnIndex++)
            {
                var firstValue = worksheet.Cells[FirstRowNumber - 1, columnIndex].GetValue().Value.RawValue;
                if (!string.IsNullOrWhiteSpace(firstValue))
                {
                    matchNumber++;

                    //종목록
                    var speciesData = GetSpeciesData(worksheet, columnIndex, matchNumber);

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

                    var surveyUnavailableReason = worksheet.Cells[43, columnIndex].GetValue().Value.RawValue;

                    var indexDI = GB.GetDI(speciesData);
                    var indexH = GB.GetH(speciesData);
                    var indexR1 = GB.GetR1(speciesData);
                    var indexJ = GB.GetJ(speciesData);
                    var (score, grade) = GB.GetBMI(speciesData, surveyUnavailableReason);

                    var speciesCount = speciesData.GroupBy(m => m.SpeciesKo).Count();
                    var individualCountSum = speciesData.Sum(m => m.IndividualCount);

                    var model = new ImportBenthos
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
                        Surbernet30 = worksheet.Cells[13, columnIndex].GetValue().Value.RawValue,
                        Surbernet50 = worksheet.Cells[14, columnIndex].GetValue().Value.RawValue,
                        Dredge = worksheet.Cells[15, columnIndex].GetValue().Value.RawValue,
                        Ekman = worksheet.Cells[16, columnIndex].GetValue().Value.RawValue,
                        Watershed = worksheet.Cells[17, columnIndex].GetValue().Value.RawValue,
                        PollutionSource = worksheet.Cells[18, columnIndex].GetValue().Value.RawValue,
                        CamopyCover = worksheet.Cells[19, columnIndex].GetValue().Value.RawValue,
                        Floodplain = worksheet.Cells[20, columnIndex].GetValue().Value.RawValue,
                        LeveeLeft = worksheet.Cells[21, columnIndex].GetValue().Value.RawValue,
                        LeveeRight = worksheet.Cells[22, columnIndex].GetValue().Value.RawValue,
                        Bedrock = worksheet.Cells[23, columnIndex].GetValue().Value.RawValue,
                        Concrete = worksheet.Cells[24, columnIndex].GetValue().Value.RawValue,
                        Mud = worksheet.Cells[25, columnIndex].GetValue().Value.RawValue,
                        Sand = worksheet.Cells[26, columnIndex].GetValue().Value.RawValue,
                        FineGravel = worksheet.Cells[27, columnIndex].GetValue().Value.RawValue,
                        Gravel = worksheet.Cells[28, columnIndex].GetValue().Value.RawValue,
                        SmallStone = worksheet.Cells[29, columnIndex].GetValue().Value.RawValue,
                        BigStone = worksheet.Cells[30, columnIndex].GetValue().Value.RawValue,
                        HabitatRiverType = worksheet.Cells[32, columnIndex].GetValue().Value.RawValue,
                        RiverWidth = worksheet.Cells[33, columnIndex].GetValue().Value.RawValue,
                        WaterWidth = worksheet.Cells[34, columnIndex].GetValue().Value.RawValue,
                        AverageDepth = worksheet.Cells[35, columnIndex].GetValue().Value.RawValue,
                        AverageVelocity = worksheet.Cells[36, columnIndex].GetValue().Value.RawValue,
                        Temperature = worksheet.Cells[37, columnIndex].GetValue().Value.RawValue,
                        WaterTemperature = worksheet.Cells[38, columnIndex].GetValue().Value.RawValue,
                        FlowState = worksheet.Cells[39, columnIndex].GetValue().Value.RawValue,
                        Transparency = worksheet.Cells[40, columnIndex].GetValue().Value.RawValue,
                        Smell = worksheet.Cells[41, columnIndex].GetValue().Value.RawValue,
                        Note = worksheet.Cells[42, columnIndex].GetValue().Value.RawValue,
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
    private (List<ImportBenthos>, List<Benthos>) DataValidation(List<ImportBenthos> importModels)
    {
        StringBuilder sb = new();
        var converteds = new List<Benthos>();

        try
        {
            foreach (ImportBenthos model in importModels)
            {
                sb.Clear();

                var insertModel = new Benthos();

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
                            if (DateTime.TryParse(value.ToString(), out DateTime dt))
                            {
                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                insertPropertyInfo?.SetValue(insertModel, dt);
                            }
                            else if (int.TryParse(value.ToString(), out int i))
                            {
                                if (i.ToString().Length == 8)
                                {
                                    var yyyymmdd = GB.ToDateFromInt(i);
                                    if (yyyymmdd != null)
                                    {
                                        PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                        insertPropertyInfo?.SetValue(insertModel, yyyymmdd);
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
                                        PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                        insertPropertyInfo?.SetValue(insertModel, yymmdd);
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
                            if (int.TryParse(value.ToString(), out int i))
                            {
                                var weather = _weathers.Where(w => w.Item1 == i).Select(w => w.Item2).FirstOrDefault();
                                if (weather != null)
                                {
                                    PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                    insertPropertyInfo?.SetValue(insertModel, weather);
                                }
                                else
                                {
                                    sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                                }
                            }
                            else
                            {
                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                insertPropertyInfo?.SetValue(insertModel, ((string)value).Trim());
                            }
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
                    else if (prop.Name == "Surbernet30" || prop.Name == "Surbernet50" || prop.Name == "Dredge" || prop.Name == "Ekman" || prop.Name == "Bedrock"
                           || prop.Name == "Concrete" || prop.Name == "Mud" || prop.Name == "Sand" || prop.Name == "FineGravel" || prop.Name == "Gravel"
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
                    else if (prop.Name == "RiverWidth" || prop.Name == "WaterWidth" || prop.Name == "AverageDepth" || prop.Name == "AverageVelocity" || prop.Name == "Temperature"
                        || prop.Name == "WaterTemperature" || prop.Name == "IndexDI" || prop.Name == "IndexH" || prop.Name == "IndexR1" || prop.Name == "IndexJ")
                    {
                        //실수
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
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            if (prop.Name == "CamopyCover")
                            {
                                if (int.TryParse(value.ToString(), out int i))
                                {
                                    var camopyCover = _camopyCovers.Where(w => w.Item1 == i).Select(w => w.Item2).FirstOrDefault();
                                    if (camopyCover != null)
                                    {
                                        PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                        insertPropertyInfo?.SetValue(insertModel, camopyCover);
                                    }
                                    else
                                    {
                                        sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                                    }
                                }
                                else
                                {
                                    PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                    insertPropertyInfo?.SetValue(insertModel, ((string)value).Trim());
                                }
                            }
                            else if (prop.Name == "Floodplain")
                            {
                                if (int.TryParse(value.ToString(), out int i))
                                {
                                    var floodplain = _floodplains.Where(w => w.Item1 == i).Select(w => w.Item2).FirstOrDefault();
                                    if (floodplain != null)
                                    {
                                        PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                        insertPropertyInfo?.SetValue(insertModel, floodplain);
                                    }
                                    else
                                    {
                                        sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                                    }
                                }
                                else
                                {
                                    PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                    insertPropertyInfo?.SetValue(insertModel, ((string)value).Trim());
                                }
                            }
                            else if (prop.Name == "HabitatRiverType")
                            {
                                if (int.TryParse(value.ToString(), out int i))
                                {
                                    var habitatRiverType = _habitatRiverTypes.Where(w => w.Item1 == i).Select(w => w.Item2).FirstOrDefault();
                                    if (habitatRiverType != null)
                                    {
                                        PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                        insertPropertyInfo?.SetValue(insertModel, habitatRiverType);
                                    }
                                    else
                                    {
                                        sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                                    }
                                }
                                else
                                {
                                    PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                    insertPropertyInfo?.SetValue(insertModel, ((string)value).Trim());
                                }

                            }
                            else if (prop.Name == "FlowState")
                            {
                                var flowStates = ((string)value).Replace(" ", "").Split([',', '.'], StringSplitOptions.RemoveEmptyEntries);
                                var flowStateNames = new List<string>();

                                foreach (var fs in flowStates)
                                {
                                    if (int.TryParse(fs, out int num))
                                    {
                                        var name = _flowStates.FirstOrDefault(w => w.Item1 == num).Item2;
                                        if (!string.IsNullOrEmpty(name))
                                            flowStateNames.Add(name);
                                    }
                                    else
                                    {
                                        // 숫자가 아니면 원본 값 사용
                                        flowStateNames.Add(fs);
                                    }
                                }

                                var result = string.Join(",", flowStateNames);

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
                        //실수
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            if (double.TryParse(value.ToString(), out double d))
                            {
                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                insertPropertyInfo?.SetValue(insertModel, Math.Round(d, 1, MidpointRounding.AwayFromZero));
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
                    else if(prop.Name == "IndicatorWeight")
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

            var repository = App.Current.Services.GetRequiredService<IBenthosRepository>();

            foreach (var model in ConvertedCollection)
            {
                progressCount++;
                GB.ProgressValue = (int)((progressCount / progressTotal) * 100);
                GB.BusyContent = $"자료 저장 중...{GB.ProgressValue}%";

                var macthNumber = model.MacthNumber;

                var addModel = await repository.InsertAsync(model);
                if (addModel != null)
                {
                    await Task.Run(() => SubBatchSave(addModel.BenthosId, macthNumber));
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
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "암반", Value = 24 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "콘크리트", Value = 25 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "진흙이하(<0.063mm)", Value = 26 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "모래(0.063-2mm)", Value = 27 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "잔자갈(2-16mm)", Value = 28 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "자갈(16-64mm)", Value = 29 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "작은돌(64-256mm)", Value = 30 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "큰돌(>256mm)", Value = 31 });
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
    private static List<ImportBenthosSpecies> GetSpeciesData(Worksheet worksheet, int columnIndex, int matchNumber)
    {
        var speciess = new List<ImportBenthosSpecies>();

        var usedRange = worksheet.UsedCellRange;
        for (int rowIndex = 46; rowIndex <= usedRange.ToIndex.RowIndex; rowIndex++)
        {
            // 
            var cellValue = worksheet.Cells[rowIndex, columnIndex].GetValue().Value.RawValue;
            if (!string.IsNullOrWhiteSpace(cellValue) && double.TryParse(cellValue, out double d))
            {
                var species = new ImportBenthosSpecies
                {
                    MacthNumber = matchNumber,
                    SaprobicValue = worksheet.Cells[rowIndex, 0].GetValue().Value.RawValue,
                    IndicatorWeight = worksheet.Cells[rowIndex, 1].GetValue().Value.RawValue,
                    Endangered1 = worksheet.Cells[rowIndex, 2].GetValue().Value.RawValue,
                    Endangered2 = worksheet.Cells[rowIndex, 3].GetValue().Value.RawValue,
                    Endemic = worksheet.Cells[rowIndex, 4].GetValue().Value.RawValue,
                    PhylumKo = worksheet.Cells[rowIndex, 5].GetValue().Value.RawValue,
                    ClassKo = worksheet.Cells[rowIndex, 6].GetValue().Value.RawValue,
                    OrderKo = worksheet.Cells[rowIndex, 7].GetValue().Value.RawValue,
                    FamilyKo = worksheet.Cells[rowIndex, 8].GetValue().Value.RawValue,
                    SpeciesEn = worksheet.Cells[rowIndex, 9].GetValue().Value.RawValue,
                    SpeciesKo = worksheet.Cells[rowIndex, 10].GetValue().Value.RawValue,
                    IndividualCount = d,
                };

                speciess.Add(species);
            }
        }
        return speciess;
    }

    #endregion
}
