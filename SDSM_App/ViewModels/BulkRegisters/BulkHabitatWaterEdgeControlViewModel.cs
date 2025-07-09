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
/// 일괄등록 서식수변
/// </summary>
public partial class BulkHabitatWaterEdgeControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    private readonly (int, string)[] _weathers = [(1, "맑음"), (2, "흐림"), (3, "비(눈)")];

    #endregion

    #region 생성자

    public BulkHabitatWaterEdgeControlViewModel()
    {
        GetColumnNumbers();
    }

    #endregion

    #region 속성

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
    private List<ImportHabitatWaterEdge> DataTableToModels(Worksheet worksheet)
    {
        var importModels = new List<ImportHabitatWaterEdge>();
        try
        {
            CellRange usedCellRange = worksheet.UsedCellRange;
            for (int columnIndex = FirstColumnNumber - 1; columnIndex <= usedCellRange.ToIndex.ColumnIndex; columnIndex++)
            {
                var firstValue = worksheet.Cells[FirstRowNumber - 1, columnIndex].GetValue().Value.RawValue;
                if (!string.IsNullOrWhiteSpace(firstValue))
                {
                    var (score, grade) = GetGrade(worksheet, columnIndex);

                    var model = new ImportHabitatWaterEdge
                    {
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
                        SurveyUnavailableReason = worksheet.Cells[13, columnIndex].GetValue().Value.RawValue,
                        Note = worksheet.Cells[14, columnIndex].GetValue().Value.RawValue,
                        EvaluationItem1 = worksheet.Cells[16, columnIndex].GetValue().Value.RawValue,
                        EvaluationItem2 = worksheet.Cells[17, columnIndex].GetValue().Value.RawValue,
                        EvaluationItem3 = worksheet.Cells[18, columnIndex].GetValue().Value.RawValue,
                        EvaluationItem4 = worksheet.Cells[19, columnIndex].GetValue().Value.RawValue,
                        EvaluationItem5 = worksheet.Cells[20, columnIndex].GetValue().Value.RawValue,
                        EvaluationItem6 = worksheet.Cells[21, columnIndex].GetValue().Value.RawValue,
                        EvaluationItem7 = worksheet.Cells[22, columnIndex].GetValue().Value.RawValue,
                        EvaluationItem8 = worksheet.Cells[23, columnIndex].GetValue().Value.RawValue,
                        EvaluationItem9 = worksheet.Cells[24, columnIndex].GetValue().Value.RawValue,
                        EvaluationItem10 = worksheet.Cells[25, columnIndex].GetValue().Value.RawValue,
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

                //다시 검수 때문에 초기화
                model.ErrorContent = string.Empty;

                //import model
                PropertyInfo[] props = model.GetType().GetProperties();

                foreach (var prop in props)
                {
                    if (prop.Name == "ErrorContent") continue;

                    var value = prop.GetValue(model);

                    if (prop.Name == "YearChsu" || prop.Name == "Side")
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
                    else if (prop.Name == "EvaluationItem1" || prop.Name == "EvaluationItem2" || prop.Name == "EvaluationItem3" || prop.Name == "EvaluationItem4" || prop.Name == "EvaluationItem5"
                        || prop.Name == "EvaluationItem6" || prop.Name == "EvaluationItem7" || prop.Name == "EvaluationItem8" || prop.Name == "EvaluationItem9" || prop.Name == "EvaluationItem10"
                        || prop.Name == "EvaluationScore")
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

                    string strSql = $"INSERT INTO HabitatWaterEdge" +
                                     $"(YearChsu, Site, SurveyDate, MajorRegion, MiddleRegion, " +
                                     $"River, RiverType, Lat, Lng, Weather, " +
                                     $"SurveyAgency, Surveyor, SurveyUnavailableReason, Note, EvaluationItem1, " +
                                     $"EvaluationItem2, EvaluationItem3, EvaluationItem4, EvaluationItem5, EvaluationItem6, " +
                                     $"EvaluationItem7, EvaluationItem8, EvaluationItem9, EvaluationItem10, EvaluationScore, EvaluationGrade) " +
                                     $"VALUES " +
                                     $"(@YearChsu, @Site, @SurveyDate, @MajorRegion, @MiddleRegion, " +
                                     $"@River, @RiverType, @Lat, @Lng, @Weather, " +
                                     $"@SurveyAgency, @Surveyor, @SurveyUnavailableReason, @Note, @EvaluationItem1, " +
                                     $"@EvaluationItem2, @EvaluationItem3, @EvaluationItem4, @EvaluationItem5, @EvaluationItem6, " +
                                     $"@EvaluationItem7, @EvaluationItem8, @EvaluationItem9, @EvaluationItem10, @EvaluationScore, @EvaluationGrade)";

                    using var command = new SQLiteCommand(strSql, conn);

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
                    command.Parameters.AddWithValue("@SurveyUnavailableReason", model.SurveyUnavailableReason ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Note", model.Note ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EvaluationItem1", model.EvaluationItem1 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EvaluationItem2", model.EvaluationItem2 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EvaluationItem3", model.EvaluationItem3 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EvaluationItem4", model.EvaluationItem4 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EvaluationItem5", model.EvaluationItem5 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EvaluationItem6", model.EvaluationItem6 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EvaluationItem7", model.EvaluationItem7 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EvaluationItem8", model.EvaluationItem8 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EvaluationItem9", model.EvaluationItem9 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EvaluationItem10", model.EvaluationItem10 ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EvaluationScore", model.EvaluationScore ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EvaluationGrade", model.EvaluationGrade ?? (object)DBNull.Value);

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

    #endregion

    #region 등급계산

    //평가등급
    private static (string , string) GetGrade(Worksheet worksheet, int columnIndex)
    {
        string grade = "-";
        double score = 0;
        double total = 0;

        for (int row = 16; row <= 25; row++)
        {
            var cellValue = worksheet.Cells[row, columnIndex].GetValue().Value.RawValue;
            if (double.TryParse(cellValue, out double v))
                total += v;
        }

        score = total != 0 ? Math.Round(total / 2, 1, MidpointRounding.AwayFromZero) : 0;

        // "접근불가"가 아니면 등급 산정
        var unavailable = worksheet.Cells[13, columnIndex].GetValue().Value.RawValue.Replace(" ", "");
        if (unavailable.Contains("접근불가") == false)
        {
            if (score >= 80) grade = "A";
            else if (score >= 60) grade = "B";
            else if (score >= 40) grade = "C";
            else if (score >= 20) grade = "D";
            else if (score >= 0) grade = "E";
        }

        return (score.ToString(), grade);
    }

    #endregion
}
