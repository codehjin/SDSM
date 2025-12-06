using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SDSM_App.Enums;
using SDSM_App.Helpers;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.Models;
using SDSM_App.Models.Repositories.Interfases;
using SDSM_Models;
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
/// 일괄등록 포유류
/// </summary>
public partial class BulkMammalControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    private readonly (int, string)[] _weathers = [(1, "맑음"), (2, "흐림"), (3, "비(눈)")];

    private string _loadedFileName = string.Empty;

    // null : 생태현황, 방류하천 없음
    // tryue : 생태현황 / false : 방류하천
    private bool? _isEcology = null;

    #endregion

    #region 생성자

    public BulkMammalControlViewModel()
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

    [ObservableProperty] private RadObservableCollection<ImportMammal> _modelCollection = [];
    [ObservableProperty] private RadObservableCollection<Mammal> _convertedCollection = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DataImportExecutedCommand))]
    private string _sheetName = "포유류";

    [ObservableProperty] private int _firstRowNumber = 5;
    [ObservableProperty] private int _firstColumnNumber = 2;
    public string FirstColumnAlphabet { get; } = "B";

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
        SheetName = "포유류";
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
                Division = "포유류",
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

            //초기화
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
    private List<ImportMammal> DataTableToModels(Worksheet worksheet)
    {
        var importModels = new List<ImportMammal>();
        try
        {
            CellRange usedCellRange = worksheet.UsedCellRange;
            for (int rowIndex = FirstRowNumber - 1; rowIndex <= usedCellRange.ToIndex.RowIndex; rowIndex++)
            {
                var firstValue = ((ICellValue)(worksheet.Cells[rowIndex, FirstColumnNumber - 1].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, FirstColumnNumber - 1].GetFormat().Value);
                if (!string.IsNullOrWhiteSpace(firstValue))
                {
                    var model = new ImportMammal
                    {
                        YearChsu = firstValue,
                        SurveyDate = ((ICellValue)(worksheet.Cells[rowIndex, 2].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 2].GetFormat().Value),
                        Weather = ((ICellValue)(worksheet.Cells[rowIndex, 3].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 3].GetFormat().Value),
                        River = ((ICellValue)(worksheet.Cells[rowIndex, 4].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 4].GetFormat().Value),
                        Site = ((ICellValue)(worksheet.Cells[rowIndex, 5].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 5].GetFormat().Value),
                        SpeciesKo = ((ICellValue)(worksheet.Cells[rowIndex, 6].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 6].GetFormat().Value),
                        SpeciesEn = ((ICellValue)(worksheet.Cells[rowIndex, 7].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 7].GetFormat().Value),
                        Feature = ((ICellValue)(worksheet.Cells[rowIndex, 8].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 8].GetFormat().Value),
                        Lat_Do = ((ICellValue)(worksheet.Cells[rowIndex, 9].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 9].GetFormat().Value),
                        Lat_Bun = ((ICellValue)(worksheet.Cells[rowIndex, 10].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 10].GetFormat().Value),
                        Lat_Cho = ((ICellValue)(worksheet.Cells[rowIndex, 11].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 11].GetFormat().Value),
                        Lng_Do = ((ICellValue)(worksheet.Cells[rowIndex, 12].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 12].GetFormat().Value),
                        Lng_Bun = ((ICellValue)(worksheet.Cells[rowIndex, 13].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 13].GetFormat().Value),
                        Lng_Cho = ((ICellValue)(worksheet.Cells[rowIndex, 14].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 14].GetFormat().Value),
                        Lat = ((ICellValue)(worksheet.Cells[rowIndex, 15].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 15].GetFormat().Value),
                        Lng = ((ICellValue)(worksheet.Cells[rowIndex, 16].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 16].GetFormat().Value),
                        ObservationSite = ((ICellValue)(worksheet.Cells[rowIndex, 17].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 17].GetFormat().Value),
                        Trace1 = ((ICellValue)(worksheet.Cells[rowIndex, 19].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 19].GetFormat().Value),
                        Trace2 = ((ICellValue)(worksheet.Cells[rowIndex, 20].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 20].GetFormat().Value),
                        Trace3 = ((ICellValue)(worksheet.Cells[rowIndex, 21].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 21].GetFormat().Value),
                        Trace4 = ((ICellValue)(worksheet.Cells[rowIndex, 22].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 22].GetFormat().Value),
                        Trace5 = ((ICellValue)(worksheet.Cells[rowIndex, 23].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 23].GetFormat().Value),
                        Trace6 = ((ICellValue)(worksheet.Cells[rowIndex, 24].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 24].GetFormat().Value),
                        Trace7 = ((ICellValue)(worksheet.Cells[rowIndex, 25].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 25].GetFormat().Value),
                        Trace8 = ((ICellValue)(worksheet.Cells[rowIndex, 26].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 26].GetFormat().Value),
                        Trace9 = ((ICellValue)(worksheet.Cells[rowIndex, 27].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 27].GetFormat().Value),
                        Trace10 = ((ICellValue)(worksheet.Cells[rowIndex, 28].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 28].GetFormat().Value),
                        Trace11 = ((ICellValue)(worksheet.Cells[rowIndex, 29].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 29].GetFormat().Value),
                        Trace12 = ((ICellValue)(worksheet.Cells[rowIndex, 30].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 30].GetFormat().Value),
                        Surveyor = ((ICellValue)(worksheet.Cells[rowIndex, 31].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 31].GetFormat().Value),
                        Note = ((ICellValue)(worksheet.Cells[rowIndex, 32].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 32].GetFormat().Value),
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
    private (List<ImportMammal>, List<Mammal>) DataValidation(List<ImportMammal> importModels)
    {
        StringBuilder sb = new();
        var converteds = new List<Mammal>();

        try
        {
            foreach (ImportMammal model in importModels)
            {
                sb.Clear();

                var insertModel = new Mammal();

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

                    if (prop.Name == "YearChsu")
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
                    else if (prop.Name == "SpeciesKo")
                    {
                        PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);

                        //필수 문자
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            var v = ((string)value).Replace(" ", "");
                            if (v.Length > 0)
                            {
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
                    else if (prop.Name == "Lat_Do")
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
                    else if (prop.Name == "Lat_Bun")
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
                    else if (prop.Name == "Lat_Cho")
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
                    else if (prop.Name == "Lng_Do")
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
                    else if (prop.Name == "Lng_Bun")
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
                    else if (prop.Name == "Lng_Cho")
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
                                sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
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
                                sb.Append(GB.GetImportErrorText(prop, ErrorType.Conversion));
                            }
                        }
                        else
                        {
                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                            insertPropertyInfo?.SetValue(insertModel, null);
                        }
                    }
                    else if (prop.Name == "Trace1" || prop.Name == "Trace2" || prop.Name == "Trace3" || prop.Name == "Trace4" || prop.Name == "Trace5"
                          || prop.Name == "Trace6" || prop.Name == "Trace7" || prop.Name == "Trace8" || prop.Name == "Trace9" || prop.Name == "Trace10"
                          || prop.Name == "Trace11" || prop.Name == "Trace12")
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

                            if (prop.Name == "River")
                                insertPropertyInfo?.SetValue(insertModel, ((string)value).Replace(" ", ""));
                            else
                                insertPropertyInfo?.SetValue(insertModel, ((string)value).Trim());
                        }
                        else
                        {
                            PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                            insertPropertyInfo?.SetValue(insertModel, null);
                        }
                    }
                }

                //위도
                if (insertModel.Lat == null)
                {
                    if (insertModel.Lat_Do != null && insertModel.Lat_Bun != null && insertModel.Lat_Cho != null)
                    {
                        insertModel.Lat = GB.DmsToDegress(insertModel.Lat_Do ?? 0, insertModel.Lat_Bun ?? 0, insertModel.Lat_Cho ?? 0);
                    }
                }

                //경도
                if (insertModel.Lng == null)
                {
                    if (insertModel.Lng_Do != null && insertModel.Lng_Bun != null && insertModel.Lng_Cho != null)
                    {
                        insertModel.Lng = GB.DmsToDegress(insertModel.Lng_Do ?? 0, insertModel.Lng_Bun ?? 0, insertModel.Lng_Cho ?? 0);
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

    //일괄저장
    private bool AddSave()
    {
        const int batchSize = 500;

        try
        {
            GB.IsBusy = true;
            GB.ProgressValue = 0;
            GB.BusyContent = "자료 저장 중...";
            GB.IsIndeterminate = false;

            var entities = ConvertedCollection.ToList();

            if (entities.Count == 0)
                return true;

            using var context = new SDSMDbContext();
            using var transaction = context.Database.BeginTransaction();

            try
            {
                // 성능 최적화
                context.ChangeTracker.AutoDetectChangesEnabled = false;

                int total = entities.Count;
                int processed = 0;

                for (int i = 0; i < total; i += batchSize)
                {
                    var batch = entities.Skip(i).Take(batchSize).ToList();

                    context.Mammals.AddRange(batch);
                    context.SaveChanges();

                    // 메모리 해제 및 다음 배치 준비
                    context.ChangeTracker.Clear();

                    processed += batch.Count;
                    GB.ProgressValue = total > 0 ? (processed / (double)total) * 100d : 100d;
                }

                transaction.Commit();
                return true;
            }
            catch (Exception exInner)
            {
                try { transaction.Rollback(); } catch { }
                GB.LogWrite(GetType().Name, nameof(AddSave), exInner.Message);
                return false;
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
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "연도차수", ValueString = "B" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "조사일", ValueString = "C" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "날씨", ValueString = "D" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "하천명", ValueString = "E" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "지점명", ValueString = "F" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "일반명", ValueString = "G" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "학명", ValueString = "H" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "특징", ValueString = "I" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "위도_도", ValueString = "J" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "위도_분", ValueString = "K" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "위도_초", ValueString = "L" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "경도_도", ValueString = "M" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "경도_분", ValueString = "N" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "경도_초", ValueString = "O" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "위도", ValueString = "P" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "경도", ValueString = "Q" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "관찰지 유형", ValueString = "S" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "포획", ValueString = "T" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "관찰", ValueString = "U" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "울음", ValueString = "V" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "사체", ValueString = "W" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "족적", ValueString = "X" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "털", ValueString = "Y" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "식흔", ValueString = "Z" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "굴", ValueString = "AA" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "번식지", ValueString = "AB" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "배설물", ValueString = "AC" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "카메라", ValueString = "AD" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "기타(탐문 등)", ValueString = "AE" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "조사자", ValueString = "AF" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "특이사항", ValueString = "AG" });
    }

    private void GetSavedFileInfos()
    {
        using var context = new SDSMDbContext();

        SavedFileInfos = [.. context.SavedFileInfos.Where(m => m.Division == "포유류").OrderBy(m => m.SavedFileInfoId)];
    }

    #endregion
}
