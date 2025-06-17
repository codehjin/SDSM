using System.ComponentModel;
using System.Reflection;

namespace SDSM_App.Commons;

/// <summary>
/// 클레스 이름과 디스플레이 이름
/// </summary>
/// <typeparam name="T"></typeparam>
public class KeyValueProperties<T>
{
    public static Dictionary<string, string> GetDisplayNameKeyValues()
    {
        var columns = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(x => x.GetCustomAttribute<DisplayNameAttribute>() != null)
            .Select(x => new { key = x.Name, value = x.GetCustomAttribute<DisplayNameAttribute>()!.DisplayName })
            .ToDictionary(x => x.key, x => x.value);
        return columns;
    }

    public static Dictionary<string, string> GetDescriptionKeyValues()
    {
        var columns = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(x => x.GetCustomAttribute<DescriptionAttribute>() != null)
            .Select(x => new { key = x.Name, value = x.GetCustomAttribute<DescriptionAttribute>()!.Description })
            .ToDictionary(x => x.key, x => x.value);
        return columns;
    }
}
