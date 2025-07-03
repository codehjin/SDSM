using System.IO;
using System.Reflection;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Diagnostics;
using SDSM_App.Enums;

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
    public static string SqliteDataSource { get; set; } = @$"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DBName)}";

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

    //좌표 도 텍스트 -> 도분초
    public static (string, string) DegressToDMS(string coordinate)
    {
        double lat;
        double lng;
        int beg;
        int min;
        double sec;

        string latValue = string.Empty;
        string lngValue = string.Empty;

        string[] latlngs = coordinate.Replace(" ", "").Split(',');

        lat = double.TryParse(latlngs[0], out lat) ? lat : 0;
        lng = double.TryParse(latlngs[1], out lng) ? lng : 0;

        string latDir = lat >= 0 ? "N" : "S";
        string lngDir = lng >= 0 ? "E" : "W";
        if (lat != 0)
        {
            lat = Math.Abs(lat);
            beg = (int)Math.Truncate(lat);
            min = (int)Math.Truncate((lat - beg) * 60);
            sec = ((lat - beg) * 60 - min) * 60;

            latValue = $"{latDir}{beg}°{min:00}\'{sec:00.00}\"";
        }

        if (lng != 0)
        {
            lng = Math.Abs(lng);
            beg = (int)Math.Truncate(lng);
            min = (int)Math.Truncate((lng - beg) * 60);
            sec = ((lng - beg) * 60 - min) * 60;

            lngValue = $"{lngDir}{beg}°{min:00}\'{sec:00.00}\"";
        }

        return (latValue, lngValue);
    }

    //좌표(위도,경도) 도 -> 도분초
    public static (int, int, double) DegressToDMS(double coordinate)
    {
        int _do = 0;
        int _bun = 0;
        double _cho = 0.0;

        if (coordinate != 0)
        {
            double _coordinate = Math.Abs(coordinate);
            _do = (int)Math.Truncate(_coordinate);
            _bun = (int)Math.Truncate((coordinate - _do) * 60);
            _cho = ((coordinate - _do) * 60 - _bun) * 60;
            _cho = Math.Round(_cho, 2, MidpointRounding.AwayFromZero);
        }

        return (_do, _bun, _cho);
    }

    //좌표 도분초 -> 도
    public static double DmsToDegress(int deg, int min, double sec, string? beg = null)
    {
        double degress = deg + min / 60d + sec / 3600d;

        if (beg != null)
        {
            if (beg == "s" || beg == "S" || beg == "w" || beg == "W")
            {
                degress = -degress;
            }
        }

        //소수점 6자리까지
        return Math.Round(degress, 6, MidpointRounding.AwayFromZero);
    }

    //파일크기 
    public static string BytesToString(long byteCount)
    {
        string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
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
        else if (content == ErrorType.ParentNull)
            return $"[조사NO없음]";
        else if (content == ErrorType.ListType)
            return $"[{fieldName}]목록없음";
        else if (content == ErrorType.Conversion)
            return $"[{fieldName}]형변환실패";
        else if (content == ErrorType.SpeciesDuplicate)
            return $"[종명중복]";
        else if (content == ErrorType.Duplicate)
            return $"[자료중복]";
        else
            return $"[기본정보없음]";
    }

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
