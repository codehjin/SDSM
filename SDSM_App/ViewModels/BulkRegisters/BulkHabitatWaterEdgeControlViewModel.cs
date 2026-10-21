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
public partial class BulkHabitatWaterEdgeControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    private readonly (int, string)[] _weathers = [(1, "맑음"), (2, "흐림"), (3, "비(눈)")];

    private string _loadedFileName = string.Empty;

    // null : 생태현황, 방류하천 없음
    // tryue : 생태현황 / false : 방류하천
    private bool? _isEcology = null;

    #endregion

    #region 생성자

    public BulkHabitatWaterEdgeControlViewModel()
    {
        InitialLoad();
    }

    #endregion

    #region 속성

    //저장된 파일목록
    public List<SavedFileInfo> SavedFileInfos { get; set; } = [];

    //항목 열 번호 목록
    public List<ColumnNumber> ColumnNumbers { get; set; } = [];

    public RadSpreadsheet DataSpreadsheet { get; set; } = new();

    [ObservableProperty] private RadObservableCollection<ImportHabitatWaterEdge> _modelCollection = [];
    [ObservableProperty] private RadObservableCollection<HabitatWaterEdge> _convertedCollection = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DataImportExecutedCommand))]
    private string _sheetName = "서식 및 수변환경";

    [ObservableProperty] private int _firstRowNumber = 2;
    [ObservableProperty] private int _firstColumnNumber = 6;
    public string FirstColumnAlphabet { get; } = "F";

    //에러 개수
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))]
    private int _errorCount;

    //변환된 자료 개수
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveExecutedCommand))]
    private int _convertedCount;

    #endregion

    #region ICommand 구현

    [RelayCommand]
    private void SheetNameResetExecuted()
    {
        SheetName = "서식 및 수변환경";
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
                var models = await Task.Run(() => DataTableToModels(worksheet));
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
        return ErrorCount == 0 && ConvertedCount > 0;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveExecuted()
    {
        if (ErrorCount != 0 || ConvertedCount == 0)
            return;

        string message = $"자료 : {ConvertedCount:N0}건을{Environment.NewLine}저장하시겠습니까?";

        SystemSounds.Beep.Play();
        if (MessageBox.Show(message, "자료 일괄 저장", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            return;

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
                    Division = "서식수변",
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
            WeakReferenceMessenger.Default.Send(new NotifyMessage(toast));

            if (decisions.Count > 0)
                DuplicateImportHelper.ShowSummary("서식수변", saveResult.Saved, saveResult.Replaced, saveResult.Skipped, saveResult.DuplicateAdded);

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
    private List<ImportHabitatWaterEdge> DataTableToModels(Worksheet worksheet)
    {
        var importModels = new List<ImportHabitatWaterEdge>();
        try
        {
            CellRange usedCellRange = worksheet.UsedCellRange;
            for (int columnIndex = FirstColumnNumber - 1; columnIndex <= usedCellRange.ToIndex.ColumnIndex; columnIndex++)
            {
                var firstValue = ((ICellValue)(worksheet.Cells[FirstRowNumber - 1, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[FirstRowNumber - 1, columnIndex].GetFormat().Value);
                if (!string.IsNullOrWhiteSpace(firstValue))
                {
                    var (score, grade) = GetGrade(worksheet, columnIndex);

                    var model = new ImportHabitatWaterEdge
                    {
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
                        SurveyUnavailableReason = ((ICellValue)(worksheet.Cells[13, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[13, columnIndex].GetFormat().Value),
                        Note = ((ICellValue)(worksheet.Cells[14, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[14, columnIndex].GetFormat().Value),
                        EvaluationItem1 = ((ICellValue)(worksheet.Cells[16, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[16, columnIndex].GetFormat().Value),
                        EvaluationItem2 = ((ICellValue)(worksheet.Cells[17, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[17, columnIndex].GetFormat().Value),
                        EvaluationItem3 = ((ICellValue)(worksheet.Cells[18, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[18, columnIndex].GetFormat().Value),
                        EvaluationItem4 = ((ICellValue)(worksheet.Cells[19, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[19, columnIndex].GetFormat().Value),
                        EvaluationItem5 = ((ICellValue)(worksheet.Cells[20, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[20, columnIndex].GetFormat().Value),
                        EvaluationItem6 = ((ICellValue)(worksheet.Cells[21, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[21, columnIndex].GetFormat().Value),
                        EvaluationItem7 = ((ICellValue)(worksheet.Cells[22, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[22, columnIndex].GetFormat().Value),
                        EvaluationItem8 = ((ICellValue)(worksheet.Cells[23, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[23, columnIndex].GetFormat().Value),
                        EvaluationItem9 = ((ICellValue)(worksheet.Cells[24, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[24, columnIndex].GetFormat().Value),
                        EvaluationItem10 = ((ICellValue)(worksheet.Cells[25, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[25, columnIndex].GetFormat().Value),
                        EvaluationScore = score,
                        EvaluationGrade = grade,
                    };

                    importModels.Add(model);
                }
            }
            return importModels;
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(DataTableToModels), ex.Message);

            return importModels;
        }
    }

    //검수하기
    private (List<ImportHabitatWaterEdge>, List<HabitatWaterEdge>) DataValidation(List<ImportHabitatWaterEdge> importModels)
    {
        StringBuilder sb = new();
        var converteds = new List<HabitatWaterEdge>();

        try
        {
            foreach (ImportHabitatWaterEdge model in importModels)
            {
                sb.Clear();

                var insertModel = new HabitatWaterEdge();

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

                    if (prop.Name == "EvaluationScore" || prop.Name == "EvaluationGrade")
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
                    else if (prop.Name == "EvaluationItem1" || prop.Name == "EvaluationItem2" || prop.Name == "EvaluationItem3" || prop.Name == "EvaluationItem4" || prop.Name == "EvaluationItem5"
                        || prop.Name == "EvaluationItem6" || prop.Name == "EvaluationItem7" || prop.Name == "EvaluationItem8" || prop.Name == "EvaluationItem9" || prop.Name == "EvaluationItem10")
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

    //DB에 같은 기록(대분류·지점·연도차수·조사일)이 있는지 찾고, 있으면 처리 방법을 묻는다 — 2026-10-21 문제 2·3
    //반환 null = 확인 실패(저장하지 않는다)
    private Dictionary<HabitatWaterEdge, DuplicateDecision>? FindDuplicates()
    {
        try
        {
            var decisions = new Dictionary<HabitatWaterEdge, DuplicateDecision>(ReferenceEqualityComparer.Instance);
            var project = _isEcology == true ? "생태현황" : "방류하천";
            var projectIds = GB.ProjectSiteNames.Where(sd => sd.Project == project).Select(sd => sd.SiteDivisionId).ToHashSet();

            using var context = new SDSMDbContext();
            var existing = context.HabitatWaterEdges.AsNoTracking().IgnoreAutoIncludes()
                .Select(m => new { Id = m.HabitatWaterEdgeId, m.YearChsu, m.Site, m.SurveyDate, m.SiteDivisionId })
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
                var newText = $"[{project}] {model.Site} · {model.YearChsu} · {DuplicateImportHelper.DateText(model.SurveyDate)}";
                var existingText = $"{ids.Count}건 ({DuplicateImportHelper.IdText(ids)})" + (ids.Count > 1 ? " — [바꾸기]는 모두 지웁니다" : "");
                var action = DuplicateImportHelper.Ask($"서식수변 — 같은 기록이 DB에 있습니다 ({i + 1}/{matches.Count})", newText, existingText, ref applyAll);
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

    private BulkSaveResult AddSave(Dictionary<HabitatWaterEdge, DuplicateDecision> decisions)
    {
        const int batchSize = 500;

        try
        {
            GB.IsBusy = true;
            GB.ProgressValue = 0;
            GB.BusyContent = "자료 저장 중...";
            GB.IsIndeterminate = false;

            //같은 기록 처리 — 건너뛰기는 빼고, 바꾸기는 옛 기록을 같은 트랜잭션 안에서 먼저 지운다(2026-10-21 문제 2·3)
            DuplicateDecision? DecisionOf(HabitatWaterEdge m) => decisions.TryGetValue(m, out var d) ? d : null;

            var all = ConvertedCollection.ToList();
            var entities = all.Where(m => DecisionOf(m)?.Action != DuplicateAction.Skip).ToList();
            int skipped = all.Count - entities.Count;
            int replaced = entities.Count(m => DecisionOf(m)?.Action == DuplicateAction.Replace);
            int duplicateAdded = entities.Count(m => DecisionOf(m)?.Action == DuplicateAction.Add);
            int saved = entities.Count - replaced;
            var deleteIds = decisions.Values.Where(d => d.Action == DuplicateAction.Replace).SelectMany(d => d.ExistingIds).Distinct().ToArray();

            if (entities.Count == 0)
                return new BulkSaveResult(true, 0, 0, skipped, 0);

            using var context = new SDSMDbContext();
            using var transaction = context.Database.BeginTransaction();

            try
            {
                if (deleteIds.Length > 0)
                    context.HabitatWaterEdges.IgnoreAutoIncludes().Where(m => deleteIds.Contains(m.HabitatWaterEdgeId)).ExecuteDelete();

                // 성능 최적화
                context.ChangeTracker.AutoDetectChangesEnabled = false;

                int total = entities.Count;
                int processed = 0;

                for (int i = 0; i < total; i += batchSize)
                {
                    var batch = entities.Skip(i).Take(batchSize).ToList();

                    context.HabitatWaterEdges.AddRange(batch);
                    context.SaveChanges();

                    // 메모리 해제 및 다음 배치 준비
                    context.ChangeTracker.Clear();

                    processed += batch.Count;
                    GB.ProgressValue = total > 0 ? (processed / (double)total) * 100d : 100d;
                }

                transaction.Commit();
                return new BulkSaveResult(true, saved, replaced, skipped, duplicateAdded);
            }
            catch (Exception exInner)
            {
                try { transaction.Rollback(); } catch { }
                GB.LogWrite(GetType().Name, nameof(AddSave), exInner.Message);
                return BulkSaveResult.Fail;
            }
            finally
            {
                // 복구
                try { context.ChangeTracker.AutoDetectChangesEnabled = true; } catch { }
            }
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
 
    //초기화
    private void Reset()
    {
        ModelCollection.Clear();
        ConvertedCollection.Clear();

        ConvertedCount = 0;
        ErrorCount = 0;
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
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "조사불가 내용", Value = 14 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "특이사항", Value = 15 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "자연적 종횡사주", Value = 17 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "하도정비/하도 자연성", Value = 18 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "유속 다양성", Value = 19 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "하천변 폭", Value = 20 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "저수로 하안공", Value = 21 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "제방하안 재료", Value = 22 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "저질 상태", Value = 23 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "횡구조물", Value = 24 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "제외지 토지 이용", Value = 25 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "제내지 토지 이용", Value = 26 });
    }

    private void GetSavedFileInfos()
    {
        using var context = new SDSMDbContext();

        SavedFileInfos = [.. context.SavedFileInfos.Where(m => m.Division == "서식수변").OrderBy(m => m.SavedFileInfoId)];

    }
    #endregion

    #region 등급계산

    //평가등급
    // ⚠ 계산은 `GB.GetHabitatGrade`(= SDSM_Core\Ecology\HabitatEvaluator) **한 곳**에서 한다(2026-08-22).
    //    여기서는 엑셀 칸을 읽어 넘기기만 한다. 근거: docs\_excel_formula_audit.md §5-5
    private static (double?, string?) GetGrade(Worksheet worksheet, int columnIndex)
    {
        // 평가항목 10개(엑셀 행 16~25). 숫자로 읽히지 않는 칸은 **미입력(null)** 이다 — 0 으로 채우지 않는다.
        // 10개를 전부 0점으로 적어 낸 조사표는 미입력이 아니라 0점·E등급이다.
        var items = new double?[10];
        for (int row = 16; row <= 25; row++)
        {
            var cellValue = ((ICellValue)(worksheet.Cells[row, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[row, columnIndex].GetFormat().Value);
            items[row - 16] = double.TryParse(cellValue, out double v) ? v : null;
        }

        // 조사불가 사유(공백 제거 후 "접근불가" 포함 여부)는 계산기가 판정한다.
        var unavailable = ((ICellValue)(worksheet.Cells[13, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[13, columnIndex].GetFormat().Value);

        return GB.GetHabitatGrade(items, unavailable);
    }

    #endregion
}
