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
/// 일괄등록 식생단면 환경, 군락
/// </summary>
public partial class BulkVegetationENVControlViewModel : ObservableObject, ITransientService
{
    #region 필드

    private readonly (int, string)[] _weathers = [(1, "맑음"), (2, "흐림"), (3, "비(눈)")];

    private string _loadedFileName = string.Empty;

    // null : 생태현황, 방류하천 없음
    // tryue : 생태현황 / false : 방류하천
    private bool? _isEcology = null;

    #endregion

    #region 생성자

    public BulkVegetationENVControlViewModel()
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

    [ObservableProperty] private RadObservableCollection<ImportVegetationENV> _modelCollection = [];
    [ObservableProperty] private RadObservableCollection<VegetationENV> _convertedCollection = [];

    [ObservableProperty] private RadObservableCollection<ImportVegetationENVSpecies> _speciesCollection = [];
    [ObservableProperty] private RadObservableCollection<VegetationENVSpecies> _convertedSpeciesCollection = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DataImportExecutedCommand))]
    private string _sheetName = "식생단면_환경";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DataImportExecutedCommand))]
    private string _sheetNameSpecies = "식생단면_출현종";

    [ObservableProperty] private int _firstRowNumber = 3;
    [ObservableProperty] private int _firstColumnNumber = 1;
    [ObservableProperty] private int _speciesFirstRowNumber = 2;
    [ObservableProperty] private int _speciesFirstColumnNumber = 1;

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

    //종수 및 개체수
    public List<ClusterModel> ClusterModelData { get; set; } = [];

    #endregion

    #region ICommand 구현

    [RelayCommand]
    private void SheetNameResetExecuted()
    {
        SheetName = "식생단면_환경";
        SheetNameSpecies = "식생단면_출현종";
    }

    private bool CanDataImport()
    {
        return !string.IsNullOrWhiteSpace(SheetName) && !string.IsNullOrWhiteSpace(SheetNameSpecies); ;
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
        Worksheet? worksheetSpecies = null;

        try
        {
            using Stream input = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            workbook = await Task.Run(() => new XlsxFormatProvider().Import(input, null));
            bool isExist = false;
            bool isExistSpecies = false;

            //초기화
            Reset();

            foreach (Worksheet sheet in workbook.Sheets)
            {
                if (sheet.Name.Replace(" ", "").ToLower() == SheetName.Replace(" ", "").ToLower())
                {
                    worksheet = sheet;
                    isExist = true;
                }
                else if (sheet.Name.Replace(" ", "").ToLower() == SheetNameSpecies.Replace(" ", "").ToLower())
                {
                    worksheetSpecies = sheet;
                    isExistSpecies = true;
                }
            }

            var message = string.Empty;

            if (isExist == false && isExistSpecies == false)
            {
                message = $"엑셀 시트명 : {SheetName}와 {SheetNameSpecies}를 찾을 수 없습니다.{Environment.NewLine}";
            }
            else if (isExist == false && isExistSpecies)
            {
                message = $"엑셀 시트명 : {SheetName}를 찾을 수 없습니다.{Environment.NewLine}";
            }
            else if (isExist && isExistSpecies == false)
            {
                message = $"엑셀 시트명 : {SheetNameSpecies}를 찾을 수 없습니다.{Environment.NewLine}";
            }

            if (message.Length != 0)
            {
                MessageBox.Show(message, "엑셀 시트명 오류", MessageBoxButton.OK, MessageBoxImage.Warning);

                GB.IsBusy = false;
                GB.BusyContent = string.Empty;

                return;
            }

            //엑셀 -> 모델
            if (worksheet != null && worksheet.Rows.Count > 0 && worksheetSpecies != null && worksheetSpecies.Rows.Count > 0)
            {
                var models = await Task.Run(() => DataTableToModels(worksheet));
                var speciesModels = await Task.Run(() => DataTableToSpeciesModels(worksheetSpecies));

                if (models.Count == 0 && speciesModels.Count == 0)
                {
                    MessageBox.Show("식생단면_환경 및 군락 자료가 없습니다.", "가져오기 오류", MessageBoxButton.OK, MessageBoxImage.Warning);

                    GB.IsBusy = false;
                    GB.BusyContent = string.Empty;

                    return;
                }
                else if (models.Count == 0 && speciesModels.Count != 0)
                {
                    MessageBox.Show("식생단면_환경 자료가 없습니다.", "가져오기 오류", MessageBoxButton.OK, MessageBoxImage.Warning);

                    GB.IsBusy = false;
                    GB.BusyContent = string.Empty;

                    return;
                }
                else if (models.Count != 0 && speciesModels.Count == 0)
                {
                    MessageBox.Show("식생단면_군락 자료가 없습니다.", "가져오기 오류", MessageBoxButton.OK, MessageBoxImage.Warning);

                    GB.IsBusy = false;
                    GB.BusyContent = string.Empty;

                    return;
                }

                if (GB.IsBusy == false)
                    GB.IsBusy = true;

                GB.BusyContent = "자료 검수 중...";

                //종수 및 개체수
                await Task.Run(() =>
                {
                    var ids = speciesModels.GroupBy(m => m.ID).Select(m => new { ID = m.Key, Count = m.Count() });
                    foreach (var id in ids)
                    {
                        var speciesCount = speciesModels.Where(m => m.ID == id.ID).GroupBy(m => m.SpeciesKo).Count();
                        var parentModel = models.Where(m => m.ID == id.ID).FirstOrDefault();
                        if (parentModel != null)
                        {
                            parentModel.SpeciesCount = speciesCount;
                            parentModel.IndividualCount = id.Count;
                        }
                    }
                });

                var data = await Task.Run(() => DataValidation(models));
                ModelCollection.AddRange(data.Item1);
                ConvertedCollection.AddRange(data.Item2);

                ConvertedCount = ConvertedCollection.Count;
                ErrorCount = ConvertedCollection.Count(m => m.ErrorContent.Length > 0);

                var speciesData = await Task.Run(() => SpeciesDataValidation(speciesModels));
                SpeciesCollection.AddRange(speciesData.Item1);
                ConvertedSpeciesCollection.AddRange(speciesData.Item2);

                ConvertedSpeciesCount = SpeciesCollection.Count;
                ErrorSpeciesCount = ConvertedSpeciesCollection.Count(m => m.ErrorContent.Length > 0);
            }
            else
            {
                MessageBox.Show("식생단면_환경 또는 식생단면_군락 자료가 없습니다.", "가져오기 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                Division = "식생단면",
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

    //엑셀->모델 환경
    private List<ImportVegetationENV> DataTableToModels(Worksheet worksheet)
    {
        var importModels = new List<ImportVegetationENV>();

        try
        {
            CellRange usedCellRange = worksheet.UsedCellRange;

            for (int rowIndex = FirstRowNumber - 1; rowIndex <= usedCellRange.ToIndex.RowIndex; rowIndex++)
            {
                var firstValue = ((ICellValue)(worksheet.Cells[rowIndex, FirstColumnNumber - 1].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, FirstColumnNumber - 1].GetFormat().Value);
                if (!string.IsNullOrWhiteSpace(firstValue))
                {
                    //첫번째 항목ID의 값이 매칭번호
                    var model = new ImportVegetationENV
                    {
                        ID = firstValue,
                        SurveyYear = ((ICellValue)(worksheet.Cells[rowIndex, 1].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 1].GetFormat().Value),
                        Chsu = ((ICellValue)(worksheet.Cells[rowIndex, 2].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 2].GetFormat().Value),
                        SurveyDate = ((ICellValue)(worksheet.Cells[rowIndex, 3].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 3].GetFormat().Value),
                        Weather = ((ICellValue)(worksheet.Cells[rowIndex, 4].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 4].GetFormat().Value),
                        Site = ((ICellValue)(worksheet.Cells[rowIndex, 5].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 5].GetFormat().Value),
                        River = ((ICellValue)(worksheet.Cells[rowIndex, 6].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 6].GetFormat().Value),
                        Surveyor = ((ICellValue)(worksheet.Cells[rowIndex, 7].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 7].GetFormat().Value),
                        StartLat = ((ICellValue)(worksheet.Cells[rowIndex, 8].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 8].GetFormat().Value),
                        StartLng = ((ICellValue)(worksheet.Cells[rowIndex, 9].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 9].GetFormat().Value),
                        StartAddress = ((ICellValue)(worksheet.Cells[rowIndex, 10].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 10].GetFormat().Value),
                        EndLat = ((ICellValue)(worksheet.Cells[rowIndex, 11].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 11].GetFormat().Value),
                        EndLng = ((ICellValue)(worksheet.Cells[rowIndex, 12].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 12].GetFormat().Value),
                        StartAzimuth = ((ICellValue)(worksheet.Cells[rowIndex, 13].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 13].GetFormat().Value),
                        RLSubstrateStructure = ((ICellValue)(worksheet.Cells[rowIndex, 14].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 14].GetFormat().Value),
                        RLDisturbance = ((ICellValue)(worksheet.Cells[rowIndex, 15].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 15].GetFormat().Value),
                        ILLeftLandUse = ((ICellValue)(worksheet.Cells[rowIndex, 16].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 16].GetFormat().Value),
                        ILRightLandUse = ((ICellValue)(worksheet.Cells[rowIndex, 17].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 17].GetFormat().Value),
                        Distance = ((ICellValue)(worksheet.Cells[rowIndex, 18].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 18].GetFormat().Value),
                        Slope = ((ICellValue)(worksheet.Cells[rowIndex, 19].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 19].GetFormat().Value),
                        DepthWater = ((ICellValue)(worksheet.Cells[rowIndex, 20].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 20].GetFormat().Value),
                        HashingMaterial = ((ICellValue)(worksheet.Cells[rowIndex, 21].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 21].GetFormat().Value),
                        Cluster = ((ICellValue)(worksheet.Cells[rowIndex, 22].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 22].GetFormat().Value),
                        T1Height = ((ICellValue)(worksheet.Cells[rowIndex, 23].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 23].GetFormat().Value),
                        T1CoverDegree = ((ICellValue)(worksheet.Cells[rowIndex, 24].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 24].GetFormat().Value),
                        T2Height = ((ICellValue)(worksheet.Cells[rowIndex, 25].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 25].GetFormat().Value),
                        T2CoverDegree = ((ICellValue)(worksheet.Cells[rowIndex, 26].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 26].GetFormat().Value),
                        SHeight = ((ICellValue)(worksheet.Cells[rowIndex, 27].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 27].GetFormat().Value),
                        SCoverDegree = ((ICellValue)(worksheet.Cells[rowIndex, 28].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 28].GetFormat().Value),
                        HHeight = ((ICellValue)(worksheet.Cells[rowIndex, 29].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 29].GetFormat().Value),
                        HCoverDegree = ((ICellValue)(worksheet.Cells[rowIndex, 30].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 30].GetFormat().Value),
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

    //엑셀->모델 군락
    private List<ImportVegetationENVSpecies> DataTableToSpeciesModels(Worksheet worksheet)
    {
        var importModels = new List<ImportVegetationENVSpecies>();

        try
        {
            CellRange usedCellRange = worksheet.UsedCellRange;

            for (int rowIndex = SpeciesFirstRowNumber - 1; rowIndex <= usedCellRange.ToIndex.RowIndex; rowIndex++)
            {
                var firstValue = ((ICellValue)(worksheet.Cells[rowIndex, SpeciesFirstColumnNumber - 1].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, SpeciesFirstColumnNumber - 1].GetFormat().Value);
                if (!string.IsNullOrWhiteSpace(firstValue))
                {
                    //첫번째 항목ID의 값이 매칭번호
                    //1열은 조사지점으로 건너뛴다
                    var model = new ImportVegetationENVSpecies
                    {
                        ID = firstValue,
                        Layer = ((ICellValue)(worksheet.Cells[rowIndex, 2].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 2].GetFormat().Value),
                        SpeciesKo = ((ICellValue)(worksheet.Cells[rowIndex, 3].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 3].GetFormat().Value),
                        CoverDegree = ((ICellValue)(worksheet.Cells[rowIndex, 4].GetValue().Value)).GetResultValueAsString(worksheet.Cells[rowIndex, 4].GetFormat().Value),
                    };

                    importModels.Add(model);
                }
            }
            return importModels;
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(DataTableToSpeciesModels), ex.Message);

            return importModels;
        }
    }

    //기본정보 검수하기
    private (List<ImportVegetationENV>, List<VegetationENV>) DataValidation(List<ImportVegetationENV> importModels)
    {
        StringBuilder sb = new();
        var converteds = new List<VegetationENV>();

        try
        {
            foreach (ImportVegetationENV model in importModels)
            {
                sb.Clear();

                var insertModel = new VegetationENV();

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

                    if (prop.Name == "SpeciesCount" || prop.Name == "IndividualCount")
                    {
                        PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);
                        insertPropertyInfo?.SetValue(insertModel, value);
                    }
                    else if (prop.Name == "ID" && prop.Name == "Chsu")
                    {
                        PropertyInfo? insertPropertyInfo = insertModel.GetType().GetProperty(prop.Name);

                        //필수 문자
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            var v = ((string)value).Replace(" ", "");
                            if (v.Length > 0)
                            {
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
                    else if (prop.Name == "SurveyYear")
                    {
                        //필수 정수 (4자리)
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            var v = ((string)value).Replace(" ", "");
                            if (v.Length > 0)
                            {
                                if (v.Length >= 4 && int.TryParse(v[..4], out int year))
                                {
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
                    else if (prop.Name == "StartLat" || prop.Name == "EndLat")
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
                    else if (prop.Name == "StartLng" || prop.Name == "EndLng")
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
                    else if (prop.Name == "StartAzimuth" || prop.Name == "Distance" || prop.Name == "Slope" || prop.Name == "DepthWater"
                        || prop.Name == "T1Height" || prop.Name == "T1CoverDegree" || prop.Name == "T2Height" || prop.Name == "T2CoverDegree"
                        || prop.Name == "SHeight" || prop.Name == "SCoverDegree" || prop.Name == "HHeight" || prop.Name == "HCoverDegree")
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
    private (List<ImportVegetationENVSpecies>, List<VegetationENVSpecies>) SpeciesDataValidation(List<ImportVegetationENVSpecies> importModels)
    {
        StringBuilder sb = new();
        var converteds = new List<VegetationENVSpecies>();

        try
        {
            foreach (ImportVegetationENVSpecies model in importModels)
            {
                sb.Clear();

                var insertModel = new VegetationENVSpecies();

                //다시 검수 때문에 초기화
                model.ErrorContent = string.Empty;

                //import model
                PropertyInfo[] props = model.GetType().GetProperties();

                foreach (var prop in props)
                {
                    if (prop.Name == "ErrorContent") continue;

                    var value = prop.GetValue(model);

                    if (prop.Name == "CoverDegree")
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

                            if (prop.Name == "Layer")
                            {
                                insertPropertyInfo?.SetValue(insertModel, ((string)value).ToUpper().Replace(" ", ""));
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

            var repository = App.Current.Services.GetRequiredService<IVegetationENVRepository>();

            foreach (var model in ConvertedCollection)
            {
                progressCount++;
                GB.ProgressValue = (int)((progressCount / progressTotal) * 100);
                GB.BusyContent = $"자료 저장 중...{GB.ProgressValue}%";

                var macthID = model.ID ?? "";

                var addModel = await repository.InsertAsync(model);
                if (addModel != null)
                {
                    await Task.Run(() => SubBatchSave(addModel.VegetationENVId, macthID));
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

    private void SubBatchSave(int parentId, string macthID)
    {
        try
        {
            var addModels = ConvertedSpeciesCollection.Where(w => w.ID == macthID).ToList();

            if (addModels.Count == 0) return;

            string strConn = GB.SqliteDataSource;

            using var conn = new SQLiteConnection(strConn);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            foreach (var model in addModels)
            {
                string strSql = $"INSERT INTO VegetationENVSpecies" +
                                $"(Layer, SpeciesKo, CoverDegree, VegetationENVId) " +
                                $"VALUES " +
                                $"(@Layer, @SpeciesKo, @CoverDegree, @VegetationENVId)";

                using var command = new SQLiteCommand(strSql, conn);

                command.Parameters.AddWithValue("@Layer", model.Layer);
                command.Parameters.AddWithValue("@SpeciesKo", model.SpeciesKo);
                command.Parameters.AddWithValue("@CoverDegree", model.CoverDegree ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@VegetationENVId", parentId);

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

        ClusterModelData.Clear();
    }

    private void GetColumnNumbers()
    {
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "ID", Value = 1 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "연도", Value = 2 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "차수", Value = 3 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "조사일", Value = 4 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "날씨", Value = 5 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "조사지점", Value = 6 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "하천명", Value = 7 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "조사자", Value = 8 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "시점_위도", Value = 9 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "시점_경도", Value = 10 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "시점_행정구역명", Value = 11 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "종점_위도", Value = 12 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "종점_경도", Value = 13 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "시점_각도", Value = 14 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "제외지_하천구조", Value = 15 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "제외지_교란", Value = 16 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "제내지_좌안_토지이용", Value = 17 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "제내지_우안_토지이용", Value = 18 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "거리", Value = 19 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "경사", Value = 20 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "수심", Value = 21 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "하상재질", Value = 22 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "군집명", Value = 23 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "T1_높이", Value = 24 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "T1_피도", Value = 25 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "T2_높이", Value = 26 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "T2_피도", Value = 27 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "S_높이", Value = 28 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "S_피도", Value = 29 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "H_높이", Value = 30 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "H_피도", Value = 31 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "식생단면_출현종" });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "ID", Value = 1 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "계층", Value = 3 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "종명", Value = 4 });
        ColumnNumbers.Add(new ColumnNumber { ColumnName = "피도", Value = 5 });
    }

    private void GetSavedFileInfos()
    {
        using var context = new SDSMDbContext();

        SavedFileInfos = [.. context.SavedFileInfos.Where(m => m.Division == "식생단면").OrderBy(m => m.SavedFileInfoId)];
    }

    #endregion
}
