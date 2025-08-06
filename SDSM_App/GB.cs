using SDSM_App.Enums;
using SDSM_App.Models;
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

    /// <summary>
    /// DMS 문자열(예: "36°47’00.5464“", "36_47_00.5464", "36N_47_00.5464E")을 도, 분, 초로 분리 (N/E/공백 제거, 대소문자 무시)
    /// </summary>
    public static (int? Do, int? Bun, double? Cho) ParseDmsString(string? dms)
    {
        if (string.IsNullOrWhiteSpace(dms)) return (null, null, null);

        // N, E, 공백 제거 (대소문자 구분 없이)
        var cleaned = dms.Replace(" ", "")
                         .Replace("N", "", StringComparison.OrdinalIgnoreCase)
                         .Replace("E", "", StringComparison.OrdinalIgnoreCase);

        // 1. 언더스코어 구분 ("36_47_00.5464")
        var parts = cleaned.Split('_');
        if (parts.Length == 3)
        {
            if (int.TryParse(parts[0], out int _do) &&
                int.TryParse(parts[1], out int _bun) &&
                double.TryParse(parts[2], out double _cho))
            {
                return (_do, _bun, _cho);
            }
        }

        // 2. 특수문자 구분 ("36°47’00.5464“" 또는 "36°47'00.5464\"")
        //    초 뒤에 ", “, 또는 ''(작은따옴표 두 개) 모두 허용
        var dmsPattern = @"(\d+)[°](\d+)[’'](\d+(?:\.\d+)?)([“""]|'{2})";
        var match = System.Text.RegularExpressions.Regex.Match(cleaned, dmsPattern);
        if (match.Success)
        {
            if (int.TryParse(match.Groups[1].Value, out int _do) &&
                int.TryParse(match.Groups[2].Value, out int _bun) &&
                double.TryParse(match.Groups[3].Value, out double _cho))
            {
                return (_do, _bun, _cho);
            }
        }

        return (null, null, null);
    }

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

    #region 저서동물 등급계산

    /// <summary>
    /// 개체수 기준 우점도(DI) 계산
    /// (가장 큰 값 + 두 번째로 큰 값) / 전체 합계, 오류 시 1 반환
    /// </summary>
    public static double GetDI(IEnumerable<ImportBenthosSpecies> speciesData)
    {
        if (speciesData == null)
            return 1;

        // 0보다 큰 개체수만 추출
        var counts = speciesData.Select(x => x.IndividualCount ?? 0)
                                  .Where(x => x > 0)
                                  .OrderByDescending(x => x)
                                  .ToList();

        if (counts.Count == 0)
            return 1;

        double largest = counts.ElementAtOrDefault(0);
        double secondLargest = counts.ElementAtOrDefault(1);
        double sum = counts.Sum();

        if (sum == 0)
            return 1;

        return (largest + secondLargest) / sum;
    }

    /// <summary>
    /// Shannon-Wiener 다양도(H') 계산
    /// (엑셀: IFERROR(-SUMPRODUCT(개체수,LOG(개체수+(개체수=0),2))/SUM(개체수)+LOG(SUM(개체수),2),0))
    /// </summary>
    public static double GetH(IEnumerable<ImportBenthosSpecies> speciesData)
    {
        if (speciesData == null)
            return 0;

        // 개체수 배열 (null은 0으로 처리)
        var counts = speciesData.Select(x => x.IndividualCount ?? 0)
                                  .Where(x => x >= 0)
                                  .ToArray();

        double sum = counts.Sum();
        if (sum == 0)
            return 0;

        // -SUMPRODUCT(개체수, LOG(개체수 + (개체수=0), 2))
        double sumProduct = counts
            .Select(x => x * Math.Log(x == 0 ? 1 : x, 2))
            .Sum();

        double result = -sumProduct / sum + Math.Log(sum, 2);

        // NaN, Infinity 등 예외 처리
        if (double.IsNaN(result) || double.IsInfinity(result))
            return 0;

        return result;
    }

    /// <summary>
    /// 종풍부도(R1) 계산
    /// (엑셀: IFERROR((COUNTIF(개체수,">0")-1)/LN(SUM(개체수)),0))
    /// </summary>
    public static double GetR1(IEnumerable<ImportBenthosSpecies> speciesData)
    {
        if (speciesData == null)
            return 0;

        // 0보다 큰 개체수만 추출
        var counts = speciesData.Select(x => x.IndividualCount ?? 0)
                                  .Where(x => x > 0)
                                  .ToList();

        int S = counts.Count; // 출현 종수
        double N = counts.Sum(); // 전체 개체수

        if (N <= 0)
            return 0;

        double result = (S - 1) / Math.Log(N);

        // NaN, Infinity 등 예외 처리
        if (double.IsNaN(result) || double.IsInfinity(result))
            return 0;

        return result;
    }

    /// <summary>
    /// 균등도(J') 계산
    /// (엑셀: IFERROR(H'/LOG(출현종수,2),0))
    /// </summary>
    public static double GetJ(IEnumerable<ImportBenthosSpecies> speciesData)
    {
        if (speciesData == null)
            return 0;

        // 다양도(H') 계산
        double h = GetH(speciesData);

        // 출현 종수(S)
        int S = speciesData.Select(x => x.IndividualCount ?? 0)
                            .Count(x => x > 0);

        if (S <= 1)
            return 0;

        double denominator = Math.Log(S, 2);
        if (denominator == 0)
            return 0;

        double result = h / denominator;

        // NaN, Infinity 등 예외 처리
        if (double.IsNaN(result) || double.IsInfinity(result))
            return 0;

        return result;
    }

    /// <summary>
    /// BMI 점수 및 등급 계산 (조사불가 사유 반영)
    /// (엑셀: IFERROR(ROUND((4-SUMPRODUCT(RankScorer,RankScorer,RankScorer)/SUMPRODUCT(RankScorer,RankScorer))*25,1),0))
    /// </summary>
    public static (double?, string?) GetBMI(IEnumerable<ImportBenthosSpecies> speciesData, string? surveyUnavailableReason)
    {
        // 조사불가 사유가 있으면 등급은 "-"
        if (!string.IsNullOrWhiteSpace(surveyUnavailableReason))
        {
            var reason = surveyUnavailableReason.Replace(" ", "");
            if (reason == "접근불가" || reason == "건천화" || reason == "준설" || reason == "공사중")
                return (null, "-");
        }

        if (speciesData == null)
            return (null, null);

        var aArr = speciesData.Select(x => double.TryParse(x.SaprobicValue, out var v) ? v : 0.0).ToArray();
        var bArr = speciesData.Select(x => int.TryParse(x.IndicatorWeight, out var v) ? v : 0).ToArray();
        var cArr = speciesData.Select(x => x.RankScorer).ToArray();

        int len = Math.Min(aArr.Length, Math.Min(bArr.Length, cArr.Length));
        if (len == 0)
            return (null, null);

        double sumProduct3 = 0;
        double sumProduct2 = 0;
        for (int i = 0; i < len; i++)
        {
            sumProduct3 += aArr[i] * bArr[i] * cArr[i];
            sumProduct2 += bArr[i] * cArr[i];
        }

        if (sumProduct2 == 0)
            return (null, null);

        double value = (4 - (sumProduct3 / sumProduct2)) * 25;
        double rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);

        if (double.IsNaN(rounded) || double.IsInfinity(rounded))
            return (null, null);

        // 등급 산정
        string grade;
        if (rounded >= 80) grade = "A";
        else if (rounded >= 65) grade = "B";
        else if (rounded >= 50) grade = "C";
        else if (rounded >= 35) grade = "D";
        else if (rounded >= 0) grade = "E";
        else grade = "Check";

        return (rounded, grade);
    }
  
    // range : 개체수 목록, value : 종 개체수
    public static int GetRankScorer(int[] range, int value)
    {
        if (value == 0)
            return 0;

        // 0보다 큰 값만 카운트
        var valid = range.Where(x => x > 0).ToArray();
        if (valid.Length == 0)
            return 0;

        // 내림차순 순위 (엑셀 RANK와 동일)
        int rank = valid.Count(x => x > value) + 1;
        double percentile = (double)rank / valid.Length;

        if (percentile <= 0.2) return 5;
        if (percentile <= 0.4) return 4;
        if (percentile <= 0.6) return 3;
        if (percentile <= 0.8) return 2;
        return 1;
    }


    #endregion

    #region 어류 등급계산

    public static (ClusterModel? cluster,  double?, string?) GetFAI(IEnumerable<ImportFishSpecies> speciesData, string? surveyUnavailableReason, int abnormalCount, int chasu = 0)
    {
        // 조사불가 사유가 있으면 등급은 "-"
        if (!string.IsNullOrWhiteSpace(surveyUnavailableReason))
        {
            var reason = surveyUnavailableReason.Replace(" ", "");
            if (reason == "접근불가" || reason == "건천화" || reason == "준설" || reason == "공사중")
                return (null,null, "-");
        }

        if (speciesData == null)
            return (null, null, null);

        int 총종수 = speciesData.Count(x => x.IndividualCount != null && x.IndividualCount > 0);
        int 총개체수 = speciesData.Sum(x => x.IndividualCount) ?? 0;

        int 외래종_종수 = speciesData.Count(x => x.Exotic == "O" && x.IndividualCount != null && x.IndividualCount > 0);
        int 외래종_개체수 = speciesData.Where(x => x.Exotic == "O" && x.IndividualCount != null && x.IndividualCount > 0).Sum(x => x.IndividualCount ?? 0);

        int 국내종총종수 = 총종수 - 외래종_종수;
        int 국내종총개체수 = 총개체수 - 외래종_개체수;

        int 여울성저서종수 = speciesData.Count(x => x.HabitatGuild == "RB" && x.IndividualCount != null && x.IndividualCount > 0);
        int 민감종수 = speciesData.Count(x => x.ToleranceGuild == "SS" && x.IndividualCount != null && x.IndividualCount > 0);

        int 내성종Sum = speciesData.Where(x => x.ToleranceGuild == "TS" && x.IndividualCount != null && x.IndividualCount > 0).Sum(x => x.IndividualCount ?? 0);
        double 내성종개체수비율 = 총개체수 != 0 ? (double)내성종Sum / 총개체수 * 100 : 0;

        int 잡식종Sum = speciesData.Where(x => x.FeedingGuild == "O" && x.IndividualCount != null && x.IndividualCount > 0).Sum(x => x.IndividualCount ?? 0);
        double 잡식종개체수비율 = 총개체수 != 0 ? (double)잡식종Sum / 총개체수 * 100 : 0;

        int 충식종Sum_I = speciesData.Where(x => x.FeedingGuild == "I" && x.IndividualCount != null && x.IndividualCount > 0).Sum(x => x.IndividualCount ?? 0);
        int 충식종SumI_Exotic = speciesData.Where(x => x.FeedingGuild == "I" && x.Exotic == "O" && x.IndividualCount != null && x.IndividualCount > 0).Sum(x => x.IndividualCount ?? 0);
        double 충식종개체수비율 = 총개체수 != 0 ? (double)(충식종Sum_I - 충식종SumI_Exotic) / 총개체수 * 100.0 : 0;

        int 비정상개체수 = abnormalCount;
        double 비정상개체수비율 = 비정상개체수 != 0 ? (비정상개체수 / 총개체수) * 100 : 0;

        double m1 = GetM1Value(chasu, 국내종총종수);
        double m2 = GetM2Value(chasu, 여울성저서종수);
        double m3 = GetM3Value(chasu, 민감종수);
        double m4 = GetM4Value(내성종개체수비율);
        double m5 = GetM5Value(잡식종개체수비율);
        double m6 = GetM6Value(충식종개체수비율);
        double m7 = GetM7Value(chasu, 국내종총개체수);
        double m8 = GetM8Value(비정상개체수비율);

        var cluster = new ClusterModel 
        {
            TotalIndividualCount = 총개체수,
            TotalSpeciesCount = 총종수,
            M1Value = 국내종총종수,
            M2Value = 여울성저서종수,
            M3Value = 민감종수,
            M4Value = Math.Round(내성종개체수비율, 1),
            M5Value = Math.Round(잡식종개체수비율, 1),
            M6Value = Math.Round(충식종개체수비율, 1),
            M7Value = 국내종총개체수,
        };

        var mValues = new List<double> { m1, m2, m3, m4, m5, m6, m7, m8 };
        var (score, grade) = GetFAIValue(mValues, surveyUnavailableReason);

        return (cluster, score, grade);
    }

    //국내종총종수
    private static double GetM1Value(int chasu, int value)
    {
        return chasu switch
        {
            1 => value <= 1 ? 0 : (value == 2 ? 6.25 : 12.5),
            2 => value <= 2 ? 0 : (value <= 5 ? 6.25 : 12.5),
            3 => value <= 4 ? 0 : (value <= 8 ? 6.25 : 12.5),
            4 => value <= 5 ? 0 : (value <= 11 ? 6.25 : 12.5),
            5 => value <= 7 ? 0 : (value <= 14 ? 6.25 : 12.5),
            6 => value <= 9 ? 0 : (value <= 18 ? 6.25 : 12.5),
            7 => value <= 11 ? 0 : (value <= 22 ? 6.25 : 12.5),
            _ => 0
        };
    }

    //여울성저서종수
    private static double GetM2Value(int chasu, int value)
    {
        return chasu switch
        {
            1 => value == 0 ? 0 : (value == 1 ? 6.25 : 12.5),
            2 => value == 0 ? 0 : (value <= 2 ? 6.25 : 12.5),
            3 => value == 0 ? 0 : (value <= 2 ? 6.25 : 12.5),
            4 => value <= 1 ? 0 : (value <= 3 ? 6.25 : 12.5),
            5 => value == 0 ? 0 : (value <= 2 ? 6.25 : 12.5),
            6 => value == 0 ? 0 : (value <= 2 ? 6.25 : 12.5),
            7 => value == 0 ? 0 : (value == 1 ? 6.25 : 12.5),
            _ => 0
        };
    }

    //민감종수
    private static double GetM3Value(int chasu, int value)
    {
        return chasu switch
        {
            1 => value == 0 ? 0 : (value <= 2 ? 6.25 : 12.5),
            2 => value == 0 ? 0 : (value <= 3 ? 6.25 : 12.5),
            3 => value <= 1 ? 0 : (value <= 4 ? 6.25 : 12.5),
            4 => value <= 1 ? 0 : (value <= 4 ? 6.25 : 12.5),
            5 => value <= 1 ? 0 : (value <= 4 ? 6.25 : 12.5),
            6 => value == 0 ? 0 : (value <= 3 ? 6.25 : 12.5),
            7 => value == 0 ? 0 : (value <= 2 ? 6.25 : 12.5),
            _ => 0
        };
    }

    //내성종 개체수 비율
    private static double GetM4Value(double value)
    {
        if (value > 70)
            return 0;
        else if (value >= 30)
            return 6.25;
        else
            return 12.5;
    }

    //잡식종 개체수 비율
    private static double GetM5Value(double value)
    {
        if (value > 70)
            return 0;
        else if (value >= 30)
            return 6.25;
        else
            return 12.5;
    }

    //충식종개체수비율
    private static double GetM6Value(double value)
    {
        if (value < 20)
            return 0;
        else if (value <= 45)
            return 6.25;
        else
            return 12.5;
    }

    //채집된 국내종 총 개체수
    private static double GetM7Value(int chasu, int Value)
    {
        return chasu switch
        {
            1 => Value <= 10 ? 0 : (Value <= 20 ? 6.25 : 12.5),
            2 => Value <= 30 ? 0 : (Value <= 55 ? 6.25 : 12.5),
            3 => Value <= 50 ? 0 : (Value <= 90 ? 6.25 : 12.5),
            4 => Value <= 60 ? 0 : (Value <= 115 ? 6.25 : 12.5),
            5 => Value <= 80 ? 0 : (Value <= 160 ? 6.25 : 12.5),
            6 => Value <= 100 ? 0 : (Value <= 200 ? 6.25 : 12.5),
            7 => Value <= 120 ? 0 : (Value <= 240 ? 6.25 : 12.5),
            _ => 0
        };
    }

    //비정상종 개체수 비율
    private static double GetM8Value(double value)
    {
        if (value > 1)
            return 0;
        else if (value > 0)
            return 6.25;
        else
            return 12.5;
    }

    //
    private static (double?, string?) GetFAIValue(List<double> mValues, string? surveyUnavailableReason)
    {
        // 조사불가 사유가 있으면 등급은 "-"
        if (!string.IsNullOrWhiteSpace(surveyUnavailableReason))
        {
            var reason = surveyUnavailableReason.Replace(" ", "");
            if (reason == "접근불가" || reason == "건천화" || reason == "준설" || reason == "공사중")
                return (null, "-");
        }

        // mValues: m1, m2, m3, m4, m5, m6, m7, m8
        double sum = mValues.Sum();
        double rounded = Math.Round(sum, 1, MidpointRounding.AwayFromZero);

        string grade;
        // 등급 산정 (엑셀 IFERROR IF 중첩과 동일)
        if (double.IsNaN(rounded) || double.IsInfinity(rounded))
        {
            grade = "E";
            return (null, grade);
        }
        else if (rounded >= 80)
            grade = "A";
        else if (rounded >= 60)
            grade = "B";
        else if (rounded >= 40)
            grade = "C";
        else if (rounded >= 20)
            grade = "D";
        else if (rounded >= 0)
            grade = "E";
        else
            grade = "Null";

        return (rounded, grade);
    }

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
