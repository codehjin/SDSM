using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using SDSM_App.Models;
using SDSM_Models;

namespace SDSM_App.Commons;

/// <summary>
/// 조사자 앱 배포용 기준자료(reference.json) 내보내기.
/// 관리자 DB의 종별 과거 개체수(min/max/기록수)를 산출하여 JSON으로 저장한다.
/// </summary>
public static class ReferenceDataExporter
{
    public static void Export()
    {
        try
        {
            using var context = new SDSMDbContext();

            var data = new ReferenceData
            {
                Version = DateTime.Now.ToString("yyyyMMdd-HHmm"),
                GeneratedAt = DateTime.Now
            };

            // 어류: 종별 개체수 통계
            foreach (var g in context.FishSpeciess
                         .Where(x => x.IndividualCount != null && x.IndividualCount > 0
                                     && x.SpeciesKo != null && x.SpeciesKo != "")
                         .ToList()
                         .GroupBy(x => x.SpeciesKo!))
            {
                data.Fish[g.Key] = new SpeciesRange
                {
                    N = g.Count(),
                    Min = g.Min(x => x.IndividualCount!.Value),
                    Max = g.Max(x => x.IndividualCount!.Value)
                };
            }

            // 저서동물: 종별 개체수 통계
            foreach (var g in context.BenthosSpeciess
                         .Where(x => x.IndividualCount != null && x.IndividualCount > 0
                                     && x.SpeciesKo != null && x.SpeciesKo != "")
                         .ToList()
                         .GroupBy(x => x.SpeciesKo!))
            {
                data.Benthos[g.Key] = new SpeciesRange
                {
                    N = g.Count(),
                    Min = g.Min(x => x.IndividualCount!.Value),
                    Max = g.Max(x => x.IndividualCount!.Value)
                };
            }

            var dialog = new SaveFileDialog
            {
                Filter = "기준자료(*.json)|*.json",
                FileName = "reference.json"
            };
            if (dialog.ShowDialog() != true) return;

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(dialog.FileName, json);

            MessageBox.Show(
                $"기준자료를 내보냈습니다.\n어류 {data.Fish.Count}종 · 저서동물 {data.Benthos.Count}종\n\n" +
                @"조사자 PC의 %AppData%\SDSM_Surveyor\reference.json 위치에 배포하세요.",
                "기준자료 내보내기", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            GB.LogWrite(nameof(ReferenceDataExporter), nameof(Export), ex.Message);
            MessageBox.Show("기준자료 내보내기 중 오류가 발생했습니다.", "오류",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
