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
public partial class BulkWaterQualityControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    private readonly (int, string)[] _weathers = [(1, "맑음"), (2, "흐림"), (3, "비(눈)")];

    private string _loadedFileName = string.Empty;

    #endregion

    #region 생성자

    public BulkWaterQualityControlViewModel()
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

    [ObservableProperty] private RadObservableCollection<ImportWaterQuality> _modelCollection = [];
    [ObservableProperty] private RadObservableCollection<WaterQuality> _convertedCollection = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DataImportExecutedCommand))]
    private string _sheetName = "수질";

    [ObservableProperty] private int _firstRowNumber = 2;
    [ObservableProperty] private int _firstColumnNumber = 6;

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
        SheetName = "수질";
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

        //저장
        bool isSeccess = await Task.Run(() => AddSave());

        if (isSeccess)
        {
            //저장된 파일정보
            var repogitory = App.Current.Services.GetRequiredService<ISavedFileInfoRepository>();
            var savedFileInfo = new SavedFileInfo
            {
                Division = "수질",
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

    //엑셀->모델
    private List<ImportWaterQuality> DataTableToModels(Worksheet worksheet)
    {
        var importModels = new List<ImportWaterQuality>();
        try
        {
            CellRange usedCellRange = worksheet.UsedCellRange;
            for (int columnIndex = FirstColumnNumber - 1; columnIndex <= usedCellRange.ToIndex.ColumnIndex; columnIndex++)
            {
                var firstValue = ((ICellValue)(worksheet.Cells[FirstRowNumber - 1, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[FirstRowNumber - 1, columnIndex].GetFormat().Value);
                if (!string.IsNullOrWhiteSpace(firstValue))
                {
                    var model = new ImportWaterQuality
                    {
                        YearChsu = firstValue,
                        Site = ((ICellValue)(worksheet.Cells[2, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[2, columnIndex].GetFormat().Value).ToString(),
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
                        PH = ((ICellValue)(worksheet.Cells[13, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[13, columnIndex].GetFormat().Value),
                        BOD = ((ICellValue)(worksheet.Cells[14, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[14, columnIndex].GetFormat().Value),
                        COD = ((ICellValue)(worksheet.Cells[15, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[15, columnIndex].GetFormat().Value),
                        TOC = ((ICellValue)(worksheet.Cells[16, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[16, columnIndex].GetFormat().Value),
                        SS = ((ICellValue)(worksheet.Cells[17, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[17, columnIndex].GetFormat().Value),
                        DO = ((ICellValue)(worksheet.Cells[18, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[18, columnIndex].GetFormat().Value),
                        TP = ((ICellValue)(worksheet.Cells[19, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[19, columnIndex].GetFormat().Value),
                        Ecotoxicity = ((ICellValue)(worksheet.Cells[20, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[20, columnIndex].GetFormat().Value),
                        EColi = ((ICellValue)(worksheet.Cells[21, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[21, columnIndex].GetFormat().Value),
                        TN = FindCellPositionByText(worksheet, "TN", 22, columnIndex),
                        EC = FindCellPositionByText(worksheet, "EC", 22, columnIndex),
                        Cl = FindCellPositionByText(worksheet, "Cl", 22, columnIndex),
                        SO42 = FindCellPositionByText(worksheet, "SO42", 22, columnIndex),
                        Cu = FindCellPositionByText(worksheet, "Cu", 22, columnIndex),
                        Zn = FindCellPositionByText(worksheet, "Zn", 22, columnIndex),
                        Cr = FindCellPositionByText(worksheet, "Cr", 22, columnIndex),
                        Turbidity = FindCellPositionByText(worksheet, "Turbidity", 22, columnIndex),
                        Chla = FindCellPositionByText(worksheet, "Chla", 22, columnIndex),
                        WaterTemperature = FindCellPositionByText(worksheet, "WaterTemperature", 22, columnIndex),
                        WaterDepth = FindCellPositionByText(worksheet, "WaterDepth", 22, columnIndex),
                        FlowVelocity = FindCellPositionByText(worksheet, "FlowVelocity", 22, columnIndex),
                        FlowSec = FindCellPositionByText(worksheet, "FlowSec", 22, columnIndex),
                        FlowDay = FindCellPositionByText(worksheet, "FlowDay", 22, columnIndex),
                        PHGrade = GetPhGrade(worksheet, columnIndex),
                        BODGrade = GetBodGrade(worksheet, columnIndex),
                        CODGrade = GetCodGrade(worksheet, columnIndex),
                        TOCGrade = GetTocGrade(worksheet, columnIndex),
                        SSGrade = GetSsGrade(worksheet, columnIndex),
                        DOGrade = GetDoGrade(worksheet, columnIndex),
                        TPGrade = GetTpGrade(worksheet, columnIndex),
                        EColiGrade = GetEColiGrade(worksheet, columnIndex),
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
    private (List<ImportWaterQuality>, List<WaterQuality>) DataValidation(List<ImportWaterQuality> importModels)
    {
        StringBuilder sb = new();
        var converteds = new List<WaterQuality>();

        try
        {
            foreach (ImportWaterQuality model in importModels)
            {
                sb.Clear();

                var insertModel = new WaterQuality();

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

                    if (prop.Name == "PHGrade" || prop.Name == "BODGrade" || prop.Name == "CODGrade" || prop.Name == "TOCGrade" || prop.Name == "SSGrade"
                        || prop.Name == "DOGrade" || prop.Name == "TPGrade" || prop.Name == "EColiGrade")
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
                    else if (prop.Name == "Side")
                    {
                        //필수 문자
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            var v = ((string)value).Replace(" ", "");
                            if (v.Length > 0)
                            {
                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                insertPropertyInfo?.SetValue(insertModel, v);
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
                    else if (prop.Name == "PH" || prop.Name == "BOD" || prop.Name == "COD" || prop.Name == "TOC" || prop.Name == "SS"
                        || prop.Name == "DO" || prop.Name == "TP" || prop.Name == "Ecotoxicity" || prop.Name == "EColi" 
                        || prop.Name == "TN" || prop.Name == "EC" || prop.Name == "Cl" || prop.Name == "SO42" || prop.Name == "Cu" || prop.Name == "Zn"
                        || prop.Name == "Cr" || prop.Name == "Turbidity" || prop.Name == "Chla" || prop.Name == "WaterTemperature" || prop.Name == "WaterDepth"
                        || prop.Name == "FlowVelocity" || prop.Name == "FlowSec" || prop.Name == "FlowDay" )
                    {
                        //실수
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            if (double.TryParse(value.ToString(), out double d))
                            {
                                PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                                
                                if(prop.Name == "TP" || prop.Name == "Cu" || prop.Name == "Zn" || prop.Name == "Cr"  || prop.Name == "FlowSec")
                                {
                                    //소수점 3자리
                                    insertPropertyInfo?.SetValue(insertModel, Math.Round(d, 3, MidpointRounding.AwayFromZero));
                                }
                                else if(prop.Name == "BOD" || prop.Name == "COD" || prop.Name == "TOC" || prop.Name == "DO" || prop.Name == "TN" || prop.Name == "FlowVelocity")
                                {
                                    //소수점 2자리
                                    insertPropertyInfo?.SetValue(insertModel, Math.Round(d, 2, MidpointRounding.AwayFromZero));
                                }
                                else
                                {
                                    insertPropertyInfo?.SetValue(insertModel, Math.Round(d, 1, MidpointRounding.AwayFromZero));
                                }
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
                            if(prop.Name == "River" || prop.Name == "RiverType")
                            {
                                insertPropertyInfo?.SetValue(insertModel, ((string)value).Replace(" ",""));
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

    private bool AddSave()
    {
        try
        {
            GB.IsBusy = true;
            GB.ProgressValue = 0;
            GB.BusyContent = "자료 저장 중...";
            GB.IsIndeterminate = false;

            double progressTotal = ConvertedCount;
            double progressCount = 0;

            string strConn = GB.SqliteDataSource;

            using (var conn = new SQLiteConnection(strConn))
            {
                conn.Open();

                using var transaction = conn.BeginTransaction();

                foreach (var model in ConvertedCollection)
                {
                    progressCount++;
                    GB.ProgressValue = (progressCount / progressCount) * 100;

                    string strSql = $"INSERT INTO WaterQuality" +
                                     $"(SurveyYear, YearChsu, Site, SurveyDate, MajorRegion, MiddleRegion, " +
                                     $"River, RiverType, Lat, Lng, Weather, " +
                                     $"SurveyAgency, Surveyor, PH, BOD, COD, " +
                                     $"TOC, SS, DO, TP, Ecotoxicity, EColi, " +
                                     $"TN, EC, Cl, SO42, Cu, " +
                                     $"Zn, Cr, Turbidity, Chla, " +
                                     $"WaterTemperature, WaterDepth, FlowVelocity, FlowSec, FlowDay," +
                                     $"PHGrade, BODGrade, CODGrade, TOCGrade, SSGrade, DOGrade, TPGrade, EColiGrade) " +
                                     $"VALUES " +
                                     $"(@SurveyYear, @YearChsu, @Site, @SurveyDate, @MajorRegion, @MiddleRegion, " +
                                     $"@River, @RiverType, @Lat, @Lng, @Weather, " +
                                     $"@SurveyAgency, @Surveyor, @PH, @BOD, @COD, " +
                                     $"@TOC, @SS, @DO, @TP, @Ecotoxicity, @EColi, " +
                                     $"@TN, @EC, @Cl, @SO42, @Cu, " +
                                     $"@Zn, @Cr, @Turbidity, @Chla, " +
                                     $"@WaterTemperature, @WaterDepth, @FlowVelocity, @FlowSec, @FlowDay," +
                                     $"@PHGrade, @BODGrade, @CODGrade, @TOCGrade, @SSGrade, @DOGrade, @TPGrade, @EColiGrade)";

                    using var command = new SQLiteCommand(strSql, conn);

                    command.Parameters.AddWithValue("@SurveyYear", model.SurveyYear);
                    command.Parameters.AddWithValue("@YearChsu", model.YearChsu);
                    command.Parameters.AddWithValue("@Site", model.Site);
                    command.Parameters.AddWithValue("@SurveyDate", model.SurveyDate);
                    command.Parameters.AddWithValue("@MajorRegion", model.MajorRegion ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@MiddleRegion", model.MiddleRegion ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@River", model.River ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@RiverType", model.RiverType ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Lat", model.Lat ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Lng", model.Lng ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Weather", model.Weather ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@SurveyAgency", model.SurveyAgency ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Surveyor", model.Surveyor ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PH", model.PH ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@BOD", model.BOD ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@COD", model.COD ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@TOC", model.TOC ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@SS", model.SS ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@DO", model.DO ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@TP", model.TP ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Ecotoxicity", model.Ecotoxicity ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EColi", model.EColi ?? (object)DBNull.Value);
                    
                    command.Parameters.AddWithValue("@TN", model.TN ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EC", model.EC ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Cl", model.Cl ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@SO42", model.SO42 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Cu", model.Cu ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Zn", model.Zn ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Cr", model.Cr ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Turbidity", model.Turbidity ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Chla", model.Chla ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@WaterTemperature", model.WaterTemperature ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@WaterDepth", model.WaterDepth ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@FlowVelocity", model.FlowVelocity ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@FlowSec", model.FlowSec ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@FlowDay", model.FlowDay ?? (object)DBNull.Value);
                    
                    command.Parameters.AddWithValue("@PHGrade", model.PHGrade ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@BODGrade", model.BODGrade ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CODGrade", model.CODGrade ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@TOCGrade", model.TOCGrade ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@SSGrade", model.SSGrade ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@DOGrade", model.DOGrade ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@TPGrade", model.TPGrade ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EColiGrade", model.EColiGrade ?? (object)DBNull.Value);

                    command.ExecuteNonQuery();
                }

                transaction.Commit();
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
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "PH", Value = 14 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "BOD", Value = 15 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "COD", Value = 16 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "TOC", Value = 17 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "SS", Value = 18 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "DO", Value = 19 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "T-P", Value = 20 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "생태독성", Value = 21 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "대장균군", Value = 22 });
    }

    private void GetSavedFileInfos()
    {
        using var context = new SDSMDbContext();

        SavedFileInfos = [.. context.SavedFileInfos.Where(m => m.Division == "수질").OrderBy(m => m.SavedFileInfoId)];
    }

    #endregion

    #region 자료위치 찾기 및 등급계산

    /// <summary>
    /// 워크시트에서 특정 문자열이 포함된 첫 번째 셀의 위치(행, 열)를 반환합니다.
    /// 찾지 못하면 null을 반환합니다.
    /// </summary>
    private static string? FindCellPositionByText(Worksheet worksheet, string name, int startRowIndex, int columnIndex)
    {
        if (worksheet == null || string.IsNullOrWhiteSpace(name))
            return null;

        // 항목별 이름 리스트 정의
        var nameMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["TN"] = ["TN", "T-N"],
            ["EC"] = ["EC", "전기전도도", "ElectricalConductivity", "Electrical"],
            ["Cl"] = ["Cl", "Cl-", "염소이온"],
            ["SO42"] = ["SO42", "SO42-", "황이온"],
            ["Cu"] = ["구리","Cu"],
            ["Zn"] = ["아연","Zn"],
            ["Cr"] = ["크롬","Cr"],
            ["Turbidity"] = ["탁도"],
            ["Chla"] = ["클로로필a", "클로로필", "Chla", "Chl-a"],
            ["WaterTemperature"] = ["수온", "온도"],
            ["WaterDepth"] = ["수심"],
            ["FlowVelocity"] = ["유속"],
            ["FlowSec"] = ["초당유량", "m3/sec", "㎥/sec"],
            ["FlowDay"] = ["일당유량", "m3/day", "㎥/day"],
        };

        if (!nameMap.TryGetValue(name, out var listName))
            return null;

        // 모든 비교 문자열을 소문자로 변환
        var lowerNames = listName.Select(x => x.ToLower()).ToList();

        var usedRange = worksheet.UsedCellRange;
        for (int rowIndex = startRowIndex; rowIndex <= usedRange.ToIndex.RowIndex; rowIndex++)
        {
            //2와3 열위치를 합쳐서 포함
            var cell2Value = ((ICellValue)(worksheet.Cells[rowIndex, 2].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 2].GetFormat().Value).Replace(" ", "").ToLower();
            var cell3Value = ((ICellValue)(worksheet.Cells[rowIndex, 3].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 3].GetFormat().Value).Replace(" ", "").ToLower();
            var cellValue = cell2Value + cell3Value;

            //2 열위치 값이 비어있지 않고
            if (!string.IsNullOrWhiteSpace(cell2Value) && lowerNames.Any(name => cellValue.Contains(name)))
            {
                return ((ICellValue)(worksheet.Cells[rowIndex, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, columnIndex].GetFormat().Value);
            }
        }
        return null;
    }

    private static int? GetPhGrade(Worksheet worksheet, int columnIndex)
    {
        var value = ((ICellValue)(worksheet.Cells[13, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[13, columnIndex].GetFormat().Value);
        if (double.TryParse(value, out double d))
            return GB.GetPhGrade(d);

        return 4;
    }

    private static string? GetBodGrade(Worksheet worksheet, int columnIndex)
    {
        var value = ((ICellValue)(worksheet.Cells[14, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[14, columnIndex].GetFormat().Value);
        if (double.TryParse(value, out double d))
            return GB.GetBodGrade(d);

        return "6";
    }

    private static string? GetCodGrade(Worksheet worksheet, int columnIndex)
    {
        var value = ((ICellValue)(worksheet.Cells[15, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[15, columnIndex].GetFormat().Value);
        if (double.TryParse(value, out double d))
            return GB.GetCodGrade(d);

        return "6";
    }

    private static string? GetTocGrade(Worksheet worksheet, int columnIndex)
    {
        var value = ((ICellValue)(worksheet.Cells[16, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[16, columnIndex].GetFormat().Value);
        if (double.TryParse(value, out double d))
            return GB.GetTocGrade(d);

        return "6";
    }

    private static int? GetSsGrade(Worksheet worksheet, int columnIndex)
    {
        var value = ((ICellValue)(worksheet.Cells[17, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[17, columnIndex].GetFormat().Value);
        if (double.TryParse(value, out double d))
            return GB.GetSsGrade(d);

        return 5;
    }

    private static string? GetDoGrade(Worksheet worksheet, int columnIndex)
    {
        var value = ((ICellValue)(worksheet.Cells[18, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[18, columnIndex].GetFormat().Value);
        if (double.TryParse(value, out double d))
            return GB.GetDoGrade(d);

        return "6";
    }

    private static string? GetTpGrade(Worksheet worksheet, int columnIndex)
    {
        var value = ((ICellValue)(worksheet.Cells[19, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[19, columnIndex].GetFormat().Value);
        if (double.TryParse(value, out double d))
            return GB.GetTpGrade(d);

        return "6";
    }

    private static string? GetEColiGrade(Worksheet worksheet, int columnIndex)
    {
        var value = ((ICellValue)(worksheet.Cells[21, columnIndex].GetValue().Value)).GetResultValueAsString(worksheet.Cells[21, columnIndex].GetFormat().Value);
        if (double.TryParse(value, out double d))
            return GB.GetEColiGrade(d);
       
        return "4";
    }

    #endregion
}
