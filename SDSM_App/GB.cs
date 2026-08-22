using SDSM_App.Enums;
using SDSM_App.Models;
using SDSM_Core.Ecology;
using SDSM_Core.Helpers;
using SDSM_Models;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace SDSM_App;

public static class GB
{
    public static event PropertyChangedEventHandler? StaticPropertyChanged;

    private static void OnStaticPropertyChanged(string propertyName)
    {
        StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
    }

    //버전
    public static string Version { get; set; } = "0.0";

    //Sqlite 파일명
    public static string DBName { get; set; } = "SDSMDB.sqlite";

    //Sqlite Data Source
    public static string SqliteDataSource { get; set; } = @$"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DBName)};foreign keys=true;";

    //Cts
    public static CancellationTokenSource? GBCts { get; set; }

    //No Image
    public static string NoImagePath { get; set; } = "/Assets/Images/noimage.png";

    //API Key
    public static readonly string VMapKey = "C29644AC-E5AF-357E-8004-1CB0AB64AA41";

    //정수      
    public static string IntPattern { get; set; } = "[^0-9]+";

    //실수       
    public static string DoublePattern { get; set; } = "^[0-9]*(?:\\.[0-9]*)?$";

    //최소날짜
    public static DateTime MinDate { get; set; } = new DateTime(2000, 1, 1);

    //로그작성
    public static void LogWrite(string model, string method, string error)
    {
        //현재위치경로(Environment.CurrentDirectory)
        string currentDirPath = AppDomain.CurrentDomain.BaseDirectory.ToString();
        Debug.WriteLine(currentDirPath);
        //Logs 디렉토리 경로
        string dirPath = Path.Combine(currentDirPath, "Logs");
        Debug.WriteLine(dirPath);
        //로그파일경로
        string filePath = dirPath + @"\Log_" + DateTime.Today.ToString("yyyyMMdd") + ".log";

        DirectoryInfo di = new(dirPath);
        FileInfo fi = new(filePath);

        try
        {
            //디렉토리가 없는 경우 생성
            if (!di.Exists)
            {
                Directory.CreateDirectory(dirPath);
            }

            //오류 메세지
            string errorMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff},Error,{model},{method},{error}";

            //로그 파일 유무
            if (!fi.Exists)
            {
                using StreamWriter sw = new(filePath);
                sw.WriteLine(errorMessage);
                sw.Close();
            }
            else
            {
                using StreamWriter sw = File.AppendText(filePath);
                sw.WriteLine(errorMessage);
                sw.Close();
            }
        }
        catch { }
    }

    public static DataTable ToDataTable<T>(List<T> items)
    {
        DataTable dt = new(typeof(T).Name);

        foreach (PropertyInfo info in typeof(T).GetProperties())
        {
            var description = GetDisplayName(info);
            if (!string.IsNullOrEmpty(description))
            {
                dt.Columns.Add(new DataColumn(description));
            }
        }

        foreach (T item in items)
        {
            DataRow row = dt.NewRow();
            foreach (PropertyInfo info in typeof(T).GetProperties())
            {
                var description = GetDisplayName(info);
                if (!string.IsNullOrEmpty(description))
                {
                    row[description] = info.GetValue(item, null) ?? DBNull.Value;
                }
            }
            dt.Rows.Add(row);
        }
        return dt;
    }

    //모델 필드명, 설명 가져오기
    public static Dictionary<string, string> GetFieldDescription<T>()
    {
        var columns = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                               .Where(x => x.GetCustomAttribute<DisplayNameAttribute>() != null)
                               .Select(x => new { key = x.Name, value = x.GetCustomAttribute<DescriptionAttribute>()!.Description.Replace(" ", "").ToUpper() })
                               .ToDictionary(x => x.key, x => x.value);
        return columns;
    }

    //모델 설명 목록
    public static string GetDescription(PropertyInfo prop)
    {
        if (prop.GetCustomAttribute<DescriptionAttribute>() != null && prop.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute description)
        {
            return description.Description;
        }
        else
        {
            return string.Empty;
        }
    }

    //모델 필드명, 디스플레이명 가져오기
    public static Dictionary<string, string> GetFieldDisplayName<T>()
    {
        var columns = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                               .Where(x => x.GetCustomAttribute<DisplayNameAttribute>() != null)
                               .Select(x => new { key = x.Name, value = x.GetCustomAttribute<DisplayNameAttribute>()!.DisplayName.Replace(" ", "").ToUpper() })
                               .ToDictionary(x => x.key, x => x.value);
        return columns;
    }

    //모델 디스플레이명 목록
    public static string GetDisplayName(PropertyInfo prop)
    {
        if (prop.GetCustomAttribute<DisplayNameAttribute>() != null && prop.GetCustomAttribute<DisplayNameAttribute>() is DisplayNameAttribute displayName)
        {
            return displayName.DisplayName;
        }
        else
        {
            return string.Empty;
        }
    }

    //날짜 차이
    public static string GetDiffTimeString(DateTime? startDate, DateTime? endDate)
    {
        if (startDate != null && endDate != null
            && DateTime.TryParse(startDate.ToString(), out DateTime start) && DateTime.TryParse(endDate.ToString(), out DateTime end))
        {
            TimeSpan timeDiff = end - start;
            int diffDays = timeDiff.Days;
            int diffHours = timeDiff.Hours;
            int diffMiniute = timeDiff.Minutes;
            int diffSecond = timeDiff.Seconds;

            return $"{diffDays}일 {diffHours}시간 {diffMiniute}분 {diffSecond}초";
        }
        else
        {
            return string.Empty;
        }
    }

    //날짜 형식(yyyyMMdd) -> DateTime
    public static DateTime? ToDateFromInt(int yyyymmdd)
    {
        var s = yyyymmdd.ToString();
        if (s.Length != 8) return null;
        if (DateTime.TryParseExact(s, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var dt))
        {
            if (dt.Month < 1 || dt.Month > 12) return null;
            if (dt.Day < 1 || dt.Day > 31) return null;
            return dt;
        }
        return null;
    }

    //날짜 형식(yyMMdd) -> DateTime
    public static DateTime? ToDateFrom6Int(int yymmdd)
    {
        var s = yymmdd.ToString("D6");
        if (s.Length != 6) return null;
        int yy = int.Parse(s[..2]);
        string yyyymmdd;
        if (yy >= 50)
            yyyymmdd = "19" + s;
        else
            yyyymmdd = "20" + s;
        if (DateTime.TryParseExact(yyyymmdd, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var dt))
        {
            if (dt.Month < 1 || dt.Month > 12) return null;
            if (dt.Day < 1 || dt.Day > 31) return null;
            return dt;
        }
        return null;
    }

    //백분율 구하기
    public static double GetPercentage(double value, double total, int decimalplaces)
    {
        return Math.Round(value * 100 / total, decimalplaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>`"위도,경도"` → 접두형 DMS 두 개(`N36°47'00.55"`). 계산은 SDSM_Core 하나뿐이다(R3).</summary>
    public static (string, string) DegressToDMS(string coordinate)
        => Coordinate.ToDmsPrefixPair(coordinate);

    //좌표(위도,경도) 도 -> 도분초
    /// <summary>도·분·초 → 십진수(소수점 6자리). 반구 S/W 면 음수. 계산은 SDSM_Core 하나뿐이다(R3).</summary>
    public static double DmsToDegress(int deg, int min, double sec, string? beg = null)
        => Coordinate.DmsToDecimal(deg, min, sec, beg);

    /// <summary>DMS 문자열(밑줄형·기호형) → (도, 분, 초). 파싱은 SDSM_Core 하나뿐이다(R3).</summary>
    public static (int? Do, int? Bun, double? Cho) ParseDmsString(string? dms)
        => Coordinate.ParseDms(dms);

    //파일크기 
    public static string BytesToString(long byteCount)
    {
        string[] suf = ["B", "KB", "MB", "GB", "TB", "PB", "EB"]; //Longs run out around EB
        if (byteCount == 0)
        {
            return "0" + suf[0];
        }
        long bytes = Math.Abs(byteCount);
        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        double num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return (Math.Sign(byteCount) * num).ToString() + suf[place];
    }

    //일괄작성
    public static string GetImportErrorText(PropertyInfo? prop, ErrorType content)
    {
        string fieldName = string.Empty;

        if (prop != null)
        {
            if (prop.GetCustomAttribute<DisplayNameAttribute>() != null && prop.GetCustomAttribute<DisplayNameAttribute>() is DisplayNameAttribute displayName)
                fieldName = displayName.DisplayName;
            else
                fieldName = prop.Name;
        }

        if (content == ErrorType.Unique)
            return $"[{fieldName}]필수누락";
        else if (content == ErrorType.ListType)
            return $"[{fieldName}]목록없음";
        else if (content == ErrorType.Conversion)
            return $"[{fieldName}]형변환실패";
        else if (content == ErrorType.SpeciesDuplicate)
            return $"[종명중복]";
        else if (content == ErrorType.Duplicate)
            return $"[자료중복]";
        else if (content == ErrorType.YearError)
            return $"[연도차수]연도오류";
        else if (content == ErrorType.SiteError)
            return $"[{fieldName}]목록없음";
        else
            return $"[기본정보없음]";
    }

    #region 프로젝트 사업장 지점명 목록

    public static List<SiteDivision> ProjectSiteNames { get; set; } = [];  

    #endregion

    #region 저서동물 등급계산

    // ⚠ 계산식은 SDSM_Core\Ecology\BenthosCalculator 하나뿐이다(05_REFACTORING R2).
    //    관리자·조사자가 같은 코드를 쓴다. 여기는 반환 형태만 맞추는 얇은 어댑터다.
    //    값을 고쳐야 하면 SDSM_Core 를 고치고 tools\CalcParity 로 회귀 대조할 것.

    /// <summary>개체수 기준 우점도(DI). (최대 + 차순) / 전체 합계, 오류 시 1.</summary>
    public static double GetDI(IEnumerable<ImportBenthosSpecies> speciesData)
        => BenthosCalculator.GetDI(speciesData);

    /// <summary>다양도지수 H' (Shannon-Weaver).</summary>
    public static double GetH(IEnumerable<ImportBenthosSpecies> speciesData)
        => BenthosCalculator.GetH(speciesData);

    /// <summary>풍부도지수 R1 (Margalef).</summary>
    public static double GetR1(IEnumerable<ImportBenthosSpecies> speciesData)
        => BenthosCalculator.GetR1(speciesData);

    /// <summary>균등도지수 J' (Pielou).</summary>
    public static double GetJ(IEnumerable<ImportBenthosSpecies> speciesData)
        => BenthosCalculator.GetJ(speciesData);

    /// <summary>
    /// 저서동물지수 BMI 와 등급.
    /// 일괄입력·편집화면 모두 "자료가 있으면 조사한 것"이므로 0종을 선언으로 본다
    /// (조사자 앱의 `출현종 없음(0종)` 토글과 같은 취급 — 12_CALC_FIX §2).
    /// </summary>
    public static (double?, string?) GetBMI(IEnumerable<ImportBenthosSpecies> speciesData, string? surveyUnavailableReason)
    {
        if (speciesData == null)
            return (null, null);

        var r = BenthosCalculator.CalculateBmiDetail(speciesData, surveyUnavailableReason, noSpeciesDeclared: true);
        return (r.Score, r.Grade);
    }

    /// <summary>개체수 순위점수(엑셀 RANK 백분위 → 5~1). 0이면 순위에서 제외.</summary>
    public static int GetRankScorer(int[] range, int value)
        => BenthosCalculator.GetRankScorer(range, value);

    #endregion

    #region 어류 등급계산

    // ⚠ 계산식은 SDSM_Core\Ecology\EcologyCalculator 하나뿐이다(05_REFACTORING R2).
    //    M1~M8 구간표·비율 계산·등급 판정이 전부 그쪽에 있다. 여기는 어댑터다.

    /// <summary>
    /// 어류평가지수 FAI. 산출값(ClusterModel)·총점·등급을 함께 돌려준다.
    /// 일괄입력·편집화면 모두 "자료가 있으면 조사한 것"이므로 0종을 선언으로 본다
    /// (0종 → 점수 0 · 등급 E, 12_CALC_FIX §2).
    /// </summary>
    public static (ClusterModel? cluster, double?, string?) GetFAI(IEnumerable<ImportFishSpecies> speciesData, string? surveyUnavailableReason, int abnormalCount, int chasu = 0)
    {
        var r = EcologyCalculator.CalculateFaiDetail(
            speciesData, surveyUnavailableReason, abnormalCount, chasu, noSpeciesDeclared: true);

        // 조사불가는 산출값을 내지 않는다(종전과 동일하게 cluster = null)
        if (r.Unavailable)
            return (null, null, r.Grade);

        if (speciesData == null)
            return (null, null, null);

        var cluster = new ClusterModel
        {
            TotalIndividualCount = r.TotalIndiv,
            TotalSpeciesCount = r.TotalSpecies,
            M1Value = r.DomesticSpecies,
            M2Value = r.RiffleBenthic,
            M3Value = r.Sensitive,
            M4Value = Math.Round(r.TolerantRatio, 1, MidpointRounding.AwayFromZero),
            M5Value = Math.Round(r.OmnivoreRatio, 1, MidpointRounding.AwayFromZero),
            M6Value = Math.Round(r.InsectRatio, 1, MidpointRounding.AwayFromZero),
            M7Value = r.DomesticIndiv,
        };

        return (cluster, r.Score, r.Grade);
    }

    #endregion

    #region 수질등급

    // ⚠ 등급 기준표는 SDSM_Core\Ecology\WaterQualityCalculator 하나뿐이다(05_REFACTORING R2).

    public static int? GetPhGrade(double? value) => WaterQualityCalculator.PhGrade(value);
    public static string? GetBodGrade(double? value) => WaterQualityCalculator.BodGrade(value);
    public static string? GetCodGrade(double? value) => WaterQualityCalculator.CodGrade(value);
    public static string? GetTocGrade(double? value) => WaterQualityCalculator.TocGrade(value);
    public static int? GetSsGrade(double? value) => WaterQualityCalculator.SsGrade(value);
    public static string? GetDoGrade(double? value) => WaterQualityCalculator.DoGrade(value);
    public static string? GetTpGrade(double? value) => WaterQualityCalculator.TpGrade(value);
    public static string? GetEColiGrade(double? value) => WaterQualityCalculator.EColiGrade(value);

    #endregion

    #region 서식수변 등급계산

    // ⚠ 계산식은 SDSM_Core\Ecology\HabitatEvaluator 하나뿐이다(2026-08-22).
    //    종전에는 편집화면·일괄입력이 **각자 계산식을 들고 있었다.** 판정 기준을 고칠 때
    //    한쪽만 고치면 같은 자료가 입력 경로에 따라 다른 등급이 되므로 이 어댑터로 모았다.
    //    (docs\_excel_formula_audit.md §5-5 — "양쪽 동시 적용")

    /// <summary>
    /// 서식·수변환경 평가점수와 등급. 평가항목 10개 합계 ÷ 2, 등급 A80·B60·C40·D20·E.
    /// 조사불가 사유는 <b>접근불가 하나뿐</b>이다(어류 FAI 는 4종 — 엑셀·코드 모두 그렇고 의도된 차이다).
    /// 항목을 하나라도 고르면 점수를 낸다. 전부 미선택일 때만 <c>null</c>(미평가) 이다.
    /// </summary>
    public static (double?, string?) GetHabitatGrade(IEnumerable<double?> items, string? surveyUnavailableReason)
        => HabitatEvaluator.Evaluate(items, surveyUnavailableReason);

    #endregion

    #region MessageText

    public static string SaveMessage { get; set; } = "자료가 저장되었습니다.";
    public static string EditMessage { get; set; } = "자료가 수정되었습니다.";
    public static string DeleteMessage { get; set; } = "자료가 삭제되었습니다.";
    public static string RestoreMessage { get; set; } = "복원 후 자료가 삭제되었습니다.";
    public static string ExportMessage { get; set; } = "저장되었습니다.";
    public static string ErrorMessage { get; set; } = "자료를 저장하는 중 오류가 발생했습니다.";

    #endregion

    #region StaticPropertyChanged 속성

    public static int MinPageSize { get; set; } = 10;
    public static int AvgPageSize { get; set; } = 10_000;
    public static int MaxPageSize { get; set; } = 1_000_000;
    public static string UserPassword { get; set; } = "0000";

    //페이징 개수
    private static int _currentPageSize = 10_000;
    public static int CurrentPageSize
    {
        get => _currentPageSize;
        set
        {
            _currentPageSize = value;
            OnStaticPropertyChanged(nameof(CurrentPageSize));
        }
    }

    //메인 메뉴 오버레이
    private static bool _isMinimal;
    public static bool IsMinimal
    {
        get => _isMinimal;
        set
        {
            _isMinimal = value;
            OnStaticPropertyChanged(nameof(IsMinimal));
        }
    }

    //화면잠금
    private static bool _isLockFloating = false;
    public static bool IsLockFloating
    {
        get => _isLockFloating;
        set
        {
            _isLockFloating = value;
            OnStaticPropertyChanged(nameof(IsLockFloating));
        }
    }

    //메인 Busy
    private static bool _isBusy = false;
    public static bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnStaticPropertyChanged(nameof(IsBusy)); }
    }

    private static string _busyContent = "로딩 중...";
    public static string BusyContent
    {
        get => !string.IsNullOrEmpty(_busyContent) ? _busyContent : "로딩 중...";
        set { _busyContent = value; OnStaticPropertyChanged(nameof(BusyContent)); }
    }

    private static bool _isIndeterminate = true;
    public static bool IsIndeterminate
    {
        get => _isIndeterminate;
        set { _isIndeterminate = value; OnStaticPropertyChanged(nameof(IsIndeterminate)); }
    }

    private static double _progressValue = 0d;
    public static double ProgressValue
    {
        get => _progressValue;
        set { _progressValue = value; OnStaticPropertyChanged(nameof(ProgressValue)); }
    }

    private static Visibility _busyIndicatorVisibility = Visibility.Visible;
    public static Visibility BusyIndicatorVisibility
    {
        get => _busyIndicatorVisibility;
        set { _busyIndicatorVisibility = value; OnStaticPropertyChanged(nameof(BusyIndicatorVisibility)); }
    }

    private static Visibility _busyCancelVisibility = Visibility.Collapsed;
    public static Visibility BusyCancelVisibility
    {
        get => _busyCancelVisibility;
        set { _busyCancelVisibility = value; OnStaticPropertyChanged(nameof(BusyCancelVisibility)); }
    }

    #endregion
}
