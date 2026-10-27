using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using Telerik.Windows.Documents.Fixed.Model;
using Telerik.Windows.Documents.Fixed.Model.Actions;
using Telerik.Windows.Documents.Fixed.Model.Navigation;

namespace SDSM_App.Converters;

/// <summary>
/// PDF 쪽(RadFixedPage) → 그 쪽의 장 · 절 제목 — 사용 설명서 창 쪽 목록(Styles\PdfResources.xaml PageNavigationStyle · 계획_관리자설명서v2 D6).
/// 제목은 PDF 책갈피에서 읽는다: 쪽마다 '그 쪽 또는 앞쪽에서 시작한 마지막 책갈피'(장 · 절 두 단계까지)의 제목.
/// 책갈피가 없는 PDF(v1.0 · 한컴 PDF)와 첫 책갈피 앞쪽(표지)은 빈 글 — 쪽 번호만 보인다.
/// </summary>
public class PdfPageTitleConverter : IValueConverter
{
    /// <summary>읽는 책갈피 깊이 — 1 = 장, 2 = 절.</summary>
    private const int MaxDepth = 2;

    //문서마다 쪽별 제목 — 책갈피가 있을 때만 담는다(문서가 닫히면 같이 사라짐)
    private static readonly ConditionalWeakTable<RadFixedDocument, string[]> Cache = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not RadFixedPage page || page.Parent is not RadFixedDocument document)
            return string.Empty;

        try
        {
            if (!Cache.TryGetValue(document, out var titles))
            {
                titles = BuildTitles(document);
                if (document.Bookmarks.Count > 0) Cache.AddOrUpdate(document, titles);
            }

            var index = document.Pages.IndexOf(page);
            return index >= 0 && index < titles.Length ? titles[index] : string.Empty;
        }
        catch (Exception ex)
        {
            GB.LogWrite(nameof(PdfPageTitleConverter), nameof(Convert), ex.Message);
            return string.Empty;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    //책갈피(문서 순서)를 쪽 번호로 펼친 뒤, 쪽마다 그 쪽까지 시작한 마지막 제목을 이어 붙인다
    private static string[] BuildTitles(RadFixedDocument document)
    {
        var starts = new List<(int Page, int Order, string Title)>();
        Walk(document, document.Bookmarks, 1, starts);

        var titles = new string[document.Pages.Count];
        var ordered = starts.OrderBy(s => s.Page).ThenBy(s => s.Order).ToList();
        var current = string.Empty;
        var k = 0;
        for (int p = 0; p < titles.Length; p++)
        {
            while (k < ordered.Count && ordered[k].Page <= p) current = ordered[k++].Title;
            titles[p] = current;
        }
        return titles;
    }

    private static void Walk(RadFixedDocument document, IEnumerable<BookmarkItem> items, int depth, List<(int Page, int Order, string Title)> starts)
    {
        foreach (var item in items)
        {
            var page = TargetPage(item);
            var index = page != null ? document.Pages.IndexOf(page) : -1;
            var title = (item.Title ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
            if (index >= 0 && title.Length > 0) starts.Add((index, starts.Count, title));

            if (depth < MaxDepth) Walk(document, item.Children, depth + 1, starts);
        }
    }

    //책갈피가 가리키는 쪽 — 바로 가리킴 · 이름 붙은 곳 · 이동 동작 순으로 본다
    private static RadFixedPage? TargetPage(BookmarkItem item)
    {
        if (item.Destination?.Page is RadFixedPage direct) return direct;
        if (item.NamedDestination?.Destination?.Page is RadFixedPage named) return named;

        foreach (var goTo in item.Actions.OfType<GoToAction>())
        {
            if (goTo.Destination?.Page is RadFixedPage p1) return p1;
            if (goTo.NamedDestination?.Destination?.Page is RadFixedPage p2) return p2;
        }
        return null;
    }
}
