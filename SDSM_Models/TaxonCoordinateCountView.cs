using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 분류군별 좌표 개수 뷰
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
    [DisplayName("서식수변")] public int HabitatWaterEdgeCount { get; set; }
    [DisplayName("서식수변")] public int HabitatWaterEdgeDistinctCount { get; set; }
    [DisplayName("수질")] public int WaterQualityCount { get; set; }
    [DisplayName("수질")] public int WaterQualityDistinctCount { get; set; }
}
