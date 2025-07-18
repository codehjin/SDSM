using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using SDSM_App.Enums;
using SDSM_App.InjectableServices;
using SDSM_App.Messengers;
using SDSM_App.Models;
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
/// 일괄등록 양서파충류
/// </summary>
public partial class BulkAmphibianReptileControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    private readonly (int, string)[] _weathers = [(1, "맑음"), (2, "흐림"), (3, "비(눈)")];

    #endregion

    #region 생성자

    public BulkAmphibianReptileControlViewModel()
    {
        GetColumnNumbers();
    }

    #endregion

    #region 속성

    //항목 열 번호 목록
    public List<ColumnNumber> ColumnNumbers { get; set; } = [];

    public RadSpreadsheet DataSpreadsheet { get; set; } = new();

    [ObservableProperty] private RadObservableCollection<ImportAmphibianReptile> _modelCollection = [];
    [ObservableProperty] private RadObservableCollection<AmphibianReptile> _convertedCollection = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DataImportExecutedCommand))]
    private string _sheetName = "양서파충류";

    [ObservableProperty] private int _firstRowNumber = 5;
    [ObservableProperty] private int _firstColumnNumber = 2;

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
        SheetName = "양서파충류";
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

    //엑셀->모델
    private List<ImportAmphibianReptile> DataTableToModels(Worksheet worksheet)
    {
        var importModels = new List<ImportAmphibianReptile>();
        try
        {
            CellRange usedCellRange = worksheet.UsedCellRange;
            for (int rowIndex = FirstRowNumber - 1; rowIndex <= usedCellRange.ToIndex.RowIndex; rowIndex++)
            {
                var firstValue = worksheet.Cells[rowIndex, FirstColumnNumber - 1].GetValue().Value.RawValue;
                if (!string.IsNullOrWhiteSpace(firstValue))
                {
                    var model = new ImportAmphibianReptile
                    {
                        YearChsu = firstValue,
                        YearValue = worksheet.Cells[rowIndex, 2].GetValue().Value.RawValue,
                        DayValue = worksheet.Cells[rowIndex, 4].GetValue().Value.RawValue,
                        Weather = worksheet.Cells[rowIndex, 5].GetValue().Value.RawValue,
                        River = worksheet.Cells[rowIndex, 6].GetValue().Value.RawValue,
                        Site = worksheet.Cells[rowIndex, 7].GetValue().Value.RawValue,
                        SpeciesKo = worksheet.Cells[rowIndex, 8].GetValue().Value.RawValue,
                        SpeciesEn = worksheet.Cells[rowIndex, 9].GetValue().Value.RawValue,
                        Feature = worksheet.Cells[rowIndex, 10].GetValue().Value.RawValue,
                        Trace1 = worksheet.Cells[rowIndex, 11].GetValue().Value.RawValue,
                        Trace2 = worksheet.Cells[rowIndex, 12].GetValue().Value.RawValue,
                        Trace3 = worksheet.Cells[rowIndex, 13].GetValue().Value.RawValue,
                        Trace4 = worksheet.Cells[rowIndex, 14].GetValue().Value.RawValue,
                        Trace5 = worksheet.Cells[rowIndex, 15].GetValue().Value.RawValue,
                        Trace6 = worksheet.Cells[rowIndex, 16].GetValue().Value.RawValue,
                        Lat_Do = worksheet.Cells[rowIndex, 17].GetValue().Value.RawValue,
                        Lat_Bun = worksheet.Cells[rowIndex, 18].GetValue().Value.RawValue,
                        Lat_Cho = worksheet.Cells[rowIndex, 19].GetValue().Value.RawValue,
                        Lng_Do = worksheet.Cells[rowIndex, 20].GetValue().Value.RawValue,
                        Lng_Bun = worksheet.Cells[rowIndex, 21].GetValue().Value.RawValue,
                        Lng_Cho = worksheet.Cells[rowIndex, 22].GetValue().Value.RawValue,
                        Lat = worksheet.Cells[rowIndex, 23].GetValue().Value.RawValue,
                        Lng = worksheet.Cells[rowIndex, 24].GetValue().Value.RawValue,
                        MajorCategory = worksheet.Cells[rowIndex, 26].GetValue().Value.RawValue,
                        MiddleCategory = worksheet.Cells[rowIndex, 27].GetValue().Value.RawValue,
                        Surveyor = worksheet.Cells[rowIndex, 28].GetValue().Value.RawValue,
                        Note = worksheet.Cells[rowIndex, 29].GetValue().Value.RawValue,
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
    private (List<ImportAmphibianReptile>, List<AmphibianReptile>) DataValidation(List<ImportAmphibianReptile> importModels)
    {
        StringBuilder sb = new();
        var converteds = new List<AmphibianReptile>();

        try
        {
            foreach (ImportAmphibianReptile model in importModels)
            {
                sb.Clear();

                var insertModel = new AmphibianReptile();

                //다시 검수 때문에 초기화
                model.ErrorContent = string.Empty;

                //import model
                PropertyInfo[] props = model.GetType().GetProperties();

                foreach (var prop in props)
                {
                    if (prop.Name == "ErrorContent" || prop.Name == "SurveyDate") continue;

                    var value = prop.GetValue(model);

                    if (prop.Name == "YearChsu" || prop.Name == "River" || prop.Name == "SpeciesKo")
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
                    else if (prop.Name == "YearValue" || prop.Name == "MonthValue" || prop.Name == "DayValue")
                    {
                        //정수 : 날짜
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
                    else if (prop.Name == "Trace1" || prop.Name == "Trace2" || prop.Name == "Trace3" || prop.Name == "Trace4" || prop.Name == "Trace5" || prop.Name == "Trace6")
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

                PropertyInfo? dateProp = insertModel.GetType().GetProperty("SurveyDate");

                //조사일
                if (insertModel.YearValue != null && insertModel.MonthValue != null && insertModel.DayValue != null)
                {
                    // 날짜 범위 체크
                    var minDate = GB.MinDate;
                    var maxDate = DateTime.Today;

                    var date = new DateTime(insertModel.YearValue.Value, insertModel.MonthValue.Value, insertModel.DayValue.Value);
                    
                    if (date < minDate || date > maxDate)
                    {
                        sb.Append(GB.GetImportErrorText(dateProp, ErrorType.Conversion));
                    }
                    else
                    {
                        //가져온값, 변환된값 둘다 대입
                        insertModel.SurveyDate = date;
                        model.SurveyDate = date;
                    }                    
                }
                else
                {
                    sb.Append(GB.GetImportErrorText(dateProp, ErrorType.Unique));
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

                    string strSql = $"INSERT INTO AmphibianReptile" +
                                     $"(YearChsu, SurveyDate, Weather, River, Site, " +
                                     $"SpeciesKo, SpeciesEn, Feature, " +
                                     $"Trace1, Trace2, Trace3, Trace4, Trace5, Trace5, Lat, Lng, " +
                                     $"MajorCategory, MiddleCategory, Surveyor, Note) " +
                                     $"VALUES " +
                                     $"(@YearChsu, @SurveyDate, @Weather, @River, @Site, " +
                                     $"@SpeciesKo, @SpeciesEn, @Feature, " +
                                     $"@Trace1, @Trace2, @Trace3, @Trace4, @Trace5, @Trace5, @Lat, @Lng, " +
                                     $"@MajorCategory, @MiddleCategory, @Surveyor, @Note)";

                    using var command = new SQLiteCommand(strSql, conn);

                    command.Parameters.AddWithValue("@YearChsu", model.YearChsu);
                    command.Parameters.AddWithValue("@SurveyDate", model.SurveyDate);
                    command.Parameters.AddWithValue("@Weather", model.Weather ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@River", model.River);
                    command.Parameters.AddWithValue("@Site", model.Site ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@SpeciesKo", model.SpeciesKo);
                    command.Parameters.AddWithValue("@SpeciesEn", model.SpeciesEn ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Feature", model.Feature ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Trace1", model.Trace1 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Trace2", model.Trace2 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Trace3", model.Trace3 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Trace4", model.Trace4 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Trace5", model.Trace5 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Trace6", model.Trace6 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Lat", model.Lat ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Lng", model.Lng ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@MajorCategory", model.MajorCategory ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@MiddleCategory", model.MiddleCategory ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Surveyor", model.Surveyor ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Note", model.Note ?? (object)DBNull.Value);

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
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "년", Value = 3 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "월", Value = 4 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "일", Value = 5 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "날씨", Value = 6 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "하천명", Value = 7 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "지점명", Value = 8 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "일반명", Value = 9 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "학명", Value = 10 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "특징", Value = 11 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "성체", Value = 12 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "유생", Value = 13 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "알", Value = 14 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "울음소리", Value = 15 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "로드킬", Value = 16 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "기타", Value = 17 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "위도_도", Value = 18 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "위도_분", Value = 19 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "위도_초", Value = 20 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "경도_도", Value = 21 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "경도_분", Value = 22 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "경도_초", Value = 23 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "위도", Value = 24 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "경도", Value = 25 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "대뷴류", Value = 27 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "중분류", Value = 28 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "조사자", Value = 29 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "특이사항", Value = 30 });
    }

    #endregion
}
