using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 분류군별 좌표 개수 뷰로 자료수와 좌표수를 홈에서 표시
/// </summary>
[Table("View_TaxonCoordinateCount")]
public class TaxonCoordinateCountView
{
    [DisplayName("어류")] public int FishCount { get; set; }
    [DisplayName("어류")] public int FishDistinctCount { get; set; }
    [DisplayName("저서동물")] public int BenthosCount { get; set; }
    [DisplayName("저서동물")] public int BenthosDistinctCount { get; set; }
    [DisplayName("조류")] public int BirdCount { get; set; }
    [DisplayName("조류")] public int BirdDistinctCount { get; set; }
    [DisplayName("포유류")] public int MammalCount { get; set; }
    [DisplayName("포유류")] public int MammalDistinctCount { get; set; }
    [DisplayName("식물상")] public int PlantCount { get; set; }
    [DisplayName("식물상")] public int PlantDistinctCount { get; set; }
    [DisplayName("식생단면")] public int VegetationENVCount { get; set; }
    [DisplayName("식생단면")] public int VegetationENVDistinctCount { get; set; }
    [DisplayName("양서파충류")] public int AmphibianReptileCount { get; set; }
    [DisplayName("양서파충류")] public int AmphibianReptileDistinctCount { get; set; }
    [DisplayName("서식수변")] public int HabitatWaterEdgeCount { get; set; }
    [DisplayName("서식수변")] public int HabitatWaterEdgeDistinctCount { get; set; }
    [DisplayName("수질")] public int WaterQualityCount { get; set; }
    [DisplayName("수질")] public int WaterQualityDistinctCount { get; set; }
}
