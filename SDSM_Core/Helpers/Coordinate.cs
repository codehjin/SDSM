using System.Globalization;
using System.Text.RegularExpressions;

namespace SDSM_Core.Helpers;

/// <summary>
/// 좌표 변환. 관리자·조사자가 함께 쓴다(05_REFACTORING R3).
///
/// 자료마다 표기가 다르다. 어느 쪽도 버릴 수 없어 **두 표기를 모두 받는다.**
///  · 밑줄형   `36_47_00.5464`         — 제출 엑셀(관리자 일괄입력)
///  · 접미형   `36°45'47.09"N`          — 조사자 앱 입력·표시
///  · 접두형   `N36°45'47.09"`          — 관리자 지도·목록 표시
///  · 십진     `36.763081`              — DB 저장값
///
/// ⚠ 표기는 여러 가지지만 **계산은 한 곳**이다. 도·분·초 ↔ 십진 변환과 반올림은
///   <see cref="DmsToDecimal"/>·<see cref="Split"/> 만 쓴다. 새 표기가 필요하면
///   파싱·포맷만 추가하고 계산을 다시 쓰지 말 것.
/// </summary>
public static class Coordinate
{
    /// <summary>조사자 앱 입력형 — `36°45'47.09"N` (반구 접미).</summary>
    private static readonly Regex SuffixDms = new(
        @"^\s*(\d+)\s*°\s*(\d+)\s*'\s*([\d.]+)\s*""?\s*([NSEWnsew])?\s*$", RegexOptions.Compiled);

    /// <summary>제출 엑셀형 — `36°47’00.5464“` / `36°47'00.5464''` (초 뒤 기호가 제각각).</summary>
    private static readonly Regex SymbolDms = new(
        @"(\d+)[°](\d+)[’'](\d+(?:\.\d+)?)([“""]|'{2})", RegexOptions.Compiled);

    // ── 핵심 계산 : 여기 말고 다른 곳에서 도분초 산술을 하지 않는다 ──────────────

    /// <summary>도·분·초 → 십진수. 반구가 S/W 면 음수. 소수점 6자리 반올림.</summary>
    public static double DmsToDecimal(int deg, int min, double sec, string? hemisphere = null)
    {
        double value = deg + min / 60d + sec / 3600d;

        if (hemisphere is not null)
        {
            if (hemisphere is "s" or "S" or "w" or "W")
                value = -value;
        }

        return Math.Round(value, 6, MidpointRounding.AwayFromZero);
    }

    /// <summary>십진수 → (도, 분, 초). 부호는 버린다(반구는 호출부가 붙인다).</summary>
    public static (int Deg, int Min, double Sec) Split(double value)
    {
        value = Math.Abs(value);
        int deg = (int)Math.Truncate(value);
        int min = (int)Math.Truncate((value - deg) * 60);
        double sec = ((value - deg) * 60 - min) * 60;
        return (deg, min, sec);
    }

    // ── 파싱 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// DMS 문자열 → (도, 분, 초). 밑줄형·기호형을 모두 받는다. 못 읽으면 전부 null.
    /// (관리자 `GB.ParseDmsString` 의 동작을 그대로 옮긴 것 — 일괄입력 엑셀이 이 형식이다.)
    /// </summary>
    public static (int? Deg, int? Min, double? Sec) ParseDms(string? dms)
    {
        if (string.IsNullOrWhiteSpace(dms)) return (null, null, null);

        // N·E 와 공백만 제거한다. S·W 를 지우면 '남/서'가 '북/동'이 되므로 건드리지 않는다.
        var cleaned = dms.Replace(" ", "")
                         .Replace("N", "", StringComparison.OrdinalIgnoreCase)
                         .Replace("E", "", StringComparison.OrdinalIgnoreCase);

        // ① 밑줄형 `36_47_00.5464`
        var parts = cleaned.Split('_');
        if (parts.Length == 3
            && int.TryParse(parts[0], out int d1)
            && int.TryParse(parts[1], out int m1)
            && double.TryParse(parts[2], out double s1))
            return (d1, m1, s1);

        // ② 기호형 `36°47’00.5464“`
        var match = SymbolDms.Match(cleaned);
        if (match.Success
            && int.TryParse(match.Groups[1].Value, out int d2)
            && int.TryParse(match.Groups[2].Value, out int m2)
            && double.TryParse(match.Groups[3].Value, out double s2))
            return (d2, m2, s2);

        return (null, null, null);
    }

    /// <summary>
    /// 십진 또는 접미형 DMS(`36°45'47.09"N`) 문자열 → 십진수. 못 읽으면 null.
    /// (조사자 앱 입력란이 이 형식이다.)
    /// </summary>
    public static double? ToDecimal(string? text)
    {
        var t = text?.Trim();
        if (string.IsNullOrEmpty(t)) return null;

        var m = SuffixDms.Match(t);
        if (m.Success)
        {
            double deg = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            double min = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            double sec = double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
            var hemi = m.Groups[4].Value.ToUpperInvariant();

            // 도 부분이 실수일 수 있어 정수 인자를 받는 DmsToDecimal 대신 직접 계산한다.
            double value = deg + min / 60 + sec / 3600;
            if (hemi is "S" or "W") value = -value;
            return Math.Round(value, 6);
        }

        return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var dec)
            ? Math.Round(dec, 6)
            : null;
    }

    // ── 포맷 ────────────────────────────────────────────────────────────────

    /// <summary>십진수 → 접미형 `36°45'47.09"N`. <paramref name="isLat"/> 로 N/S · E/W 를 정한다.</summary>
    public static string? ToDmsSuffix(double? value, bool isLat)
    {
        if (value is not double v) return null;

        var hemi = isLat ? (v < 0 ? "S" : "N") : (v < 0 ? "W" : "E");
        var (deg, min, sec) = Split(v);
        sec = Math.Round(sec, 2);

        // 반올림으로 60초/60분이 되는 경우 올림 처리
        if (sec >= 60) { sec -= 60; min += 1; }
        if (min >= 60) { min -= 60; deg += 1; }

        return $"{deg}°{min}'{sec.ToString("0.##", CultureInfo.InvariantCulture)}\"{hemi}";
    }

    /// <summary>
    /// `"위도,경도"` 십진 문자열 → 접두형 DMS 두 개(`N36°47'00.55"`, `E127°05'26.18"`).
    /// 값이 0이거나 못 읽으면 그 자리는 빈 문자열이다(관리자 표시 규약 그대로).
    /// </summary>
    public static (string Lat, string Lng) ToDmsPrefixPair(string coordinate)
    {
        string[] parts = (coordinate ?? string.Empty).Replace(" ", "").Split(',');

        double lat = parts.Length > 0 && double.TryParse(parts[0], out var la) ? la : 0;
        double lng = parts.Length > 1 && double.TryParse(parts[1], out var ln) ? ln : 0;

        return (Format(lat, lat >= 0 ? "N" : "S"), Format(lng, lng >= 0 ? "E" : "W"));

        static string Format(double value, string dir)
        {
            if (value == 0) return string.Empty;
            var (deg, min, sec) = Split(value);
            return $"{dir}{deg}°{min:00}'{sec:00.00}\"";
        }
    }

    /// <summary>좌표를 여는 지도 URL. 좌표가 없으면 null.</summary>
    public static string? MapUrl(double? lat, double? lng)
    {
        if (lat is not double la || lng is not double ln) return null;
        var q = $"{la.ToString(CultureInfo.InvariantCulture)},{ln.ToString(CultureInfo.InvariantCulture)}";
        return $"https://www.google.com/maps/search/?api=1&query={q}";
    }
}
