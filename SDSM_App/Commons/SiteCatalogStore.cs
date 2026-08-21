using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using SDSM_Models;

namespace SDSM_App.Commons;

/// <summary>
/// 조사지점 마스터(`sites.json`) 읽기·쓰기.
/// ⚠ 지점 마스터의 **원본은 DB가 아니라 이 파일**이다(ADMIN_TASKS §2-5).
/// 작업본은 실행 폴더에 두어 `SDSMDB.sqlite` 와 같은 자리에서 관리한다.
/// </summary>
public static class SiteCatalogStore
{
    /// <summary>작업용 마스터 파일. 조사자 배포본은 여기서 [내보내기]로 만든다.</summary>
    public static string MasterPath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sites.json");

    /// <summary>지점 사진 루트. 설정값이 비면 `&lt;앱폴더&gt;\SitePhotos`.</summary>
    public static string PhotoRoot
    {
        get
        {
            var configured = Properties.Settings.Default.SitePhotoRoot;
            return string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SitePhotos")
                : configured;
        }
    }

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping   // 한글이 \uXXXX 로 깨지지 않게
    };

    /// <summary>마스터를 읽는다. 파일이 없거나 손상되면 빈 카탈로그를 돌려준다(앱을 막지 않는다).</summary>
    public static SiteCatalog Load(string? path = null)
    {
        var target = path ?? MasterPath;
        try
        {
            if (!File.Exists(target)) return new SiteCatalog();
            var data = JsonSerializer.Deserialize<SiteCatalog>(File.ReadAllText(target), ReadOptions);
            return data ?? new SiteCatalog();
        }
        catch (Exception ex)
        {
            GB.LogWrite(nameof(SiteCatalogStore), nameof(Load), ex.Message);
            return new SiteCatalog();
        }
    }

    /// <summary>마스터를 저장한다. 저장 직전에 버전·생성시각을 갱신한다.</summary>
    public static void Save(SiteCatalog catalog, string? path = null)
    {
        catalog.GeneratedAt = DateTime.Now;
        catalog.Version = $"sites-{catalog.GeneratedAt:yyyyMMdd}";
        File.WriteAllText(path ?? MasterPath, JsonSerializer.Serialize(catalog, WriteOptions));
    }

    /// <summary>
    /// 사진 원본을 사진 루트로 복사하고 상대경로를 돌려준다.
    /// 저장 위치는 `&lt;PhotoRoot&gt;\{Project}\{SiteName}\{파일명}`.
    /// 같은 이름이 있으면 뒤에 `_1`, `_2` … 를 붙여 덮어쓰지 않는다.
    /// </summary>
    public static string CopyPhoto(string sourceFile, SurveySite site)
    {
        var project = Sanitize(site.Project ?? "미분류");
        var name = Sanitize(string.IsNullOrWhiteSpace(site.SiteName) ? "미지정" : site.SiteName);

        var dir = Path.Combine(PhotoRoot, project, name);
        Directory.CreateDirectory(dir);

        var baseName = Path.GetFileNameWithoutExtension(sourceFile);
        var ext = Path.GetExtension(sourceFile);
        var target = Path.Combine(dir, baseName + ext);
        for (int i = 1; File.Exists(target); i++)
            target = Path.Combine(dir, $"{baseName}_{i}{ext}");

        File.Copy(sourceFile, target);
        return Path.GetRelativePath(PhotoRoot, target);
    }

    /// <summary>상대경로 → 절대경로.</summary>
    public static string FullPath(SitePhoto photo) => Path.Combine(PhotoRoot, photo.RelPath);

    /// <summary>사진 파일이 실제로 있는지. 없어도 오류를 내지 않고 목록에는 남긴다.</summary>
    public static bool Exists(SitePhoto photo)
    {
        try { return File.Exists(FullPath(photo)); }
        catch { return false; }
    }

    private static string Sanitize(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
        return value.Trim();
    }
}
