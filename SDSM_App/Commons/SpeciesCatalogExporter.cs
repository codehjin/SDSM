using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SDSM_App.Models;

namespace SDSM_App.Commons;

/// <summary>
/// [종목록 내보내기] — 관리자 종목록(어류 · 저서)을 조사자 앱의 species.json 형식으로 저장한다(2026-10-22 §22-3 ②).
/// 형식은 조사자 SpeciesListProvider · tools\build_species_json.py 와 같다:
///   맨 위 Version · GeneratedAt · Fish · Benthos(관찰형 Bird · Mammal · Amphibian 은 쓰지 않는다 — 조사자 쪽에서 합친다)
///   칸 이름 · 순서는 SDSM_Models 의 FishSpeciesList · BenthosSpeciesList 그대로(Id · 개체수 · 검색글 제외),
///   국명 순(서수 비교) · 들여쓰기 2칸 · 한글 그대로 · UTF-8(BOM 없음) · 줄바꿈 LF.
/// </summary>
public static class SpeciesCatalogExporter
{
    public static string VersionText(DateTime now) => $"jichim2026-30+admin-{now:yyMMdd}";

    /// <summary>JSON 글과 종 수 — 파일 저장과 시험이 같이 쓴다.</summary>
    public static (string Json, int Fish, int Benthos) Build(SDSMDbContext context, DateTime now)
    {
        var fish = context.FishSpeciesLists.AsNoTracking().ToList()
            .OrderBy(m => m.SpeciesKo, StringComparer.Ordinal).ThenBy(m => m.SpeciesEn, StringComparer.Ordinal)
            .Select(m => new
            {
                m.FishTrait, m.ToleranceGuild, m.FeedingGuild, m.HabitatGuild, m.Exotic, m.Endemic,
                m.Endangered1, m.Endangered2, m.NaturalMonument, m.LineageOrder,
                m.ClassKo, m.OrderKo, m.FamilyKo, m.SpeciesKo, m.SpeciesEn,
            }).ToList();

        var benthos = context.BenthosSpeciesLists.AsNoTracking().ToList()
            .OrderBy(m => m.SpeciesKo, StringComparer.Ordinal).ThenBy(m => m.SpeciesEn, StringComparer.Ordinal)
            .Select(m => new
            {
                m.SaprobicValue, m.IndicatorWeight, m.Endangered1, m.Endangered2, m.Endemic,
                m.PhylumKo, m.ClassKo, m.OrderKo, m.FamilyKo, m.SpeciesKo, m.SpeciesEn,
            }).ToList();

        var data = new
        {
            Version = VersionText(now),
            GeneratedAt = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second),   // 초 단위(조사자 판과 같은 꼴)
            Fish = fish,
            Benthos = benthos,
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
        return (json.Replace("\r\n", "\n"), fish.Count, benthos.Count);
    }

    public static void Export()
    {
        try
        {
            var now = DateTime.Now;
            using var context = new SDSMDbContext();
            var (json, fish, benthos) = Build(context, now);

            var dialog = new SaveFileDialog
            {
                Filter = "종목록(*.json)|*.json",
                FileName = "species.json"
            };
            if (dialog.ShowDialog() != true) return;

            File.WriteAllText(dialog.FileName, json, new UTF8Encoding(false));

            MessageBox.Show(
                $"종목록을 내보냈습니다({VersionText(now)}).\n어류 {fish:N0}종 · 저서동물 {benthos:N0}종\n\n" +
                "어류 · 저서만 들어 있다(조류 · 포유류 · 양서파충류 없음). 조사자 앱에는 조사자 쪽에서 관찰형 3종과 합쳐 새 판으로 넣는다 —\n" +
                @"이 파일을 그대로 %AppData%\SDSM_Surveyor\species.json 에 두면 조류 · 포유류 · 양서파충류 목록이 비어 보인다.",
                "종목록 내보내기", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            GB.LogWrite(nameof(SpeciesCatalogExporter), nameof(Export), ex.Message);
            MessageBox.Show("종목록 내보내기 중 오류가 발생했습니다.", "오류",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
