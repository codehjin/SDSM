using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDSM_Models;

/// <summary>
/// 분류군별 좌표 개수 뷰로 자료수와 좌표수를 홈에서 표시
/// </summary>
[Table("View_TaxonCoordinateCount")]
public class TaxonCoordinateCountView
{
    [DisplayName("방류하천_어류")] public int FishDischargeCount { get; set; }
    [DisplayName("방류하천_어류")] public int FishDischargeDistinctCount { get; set; }
    [DisplayName("생태현황_어류")] public int FishEcologyCount { get; set; }
    [DisplayName("생태현황_어류")] public int FishEcologyDistinctCount { get; set; }

    [DisplayName("방류하천_저서동물")] public int BenthosDischargeCount { get; set; }
    [DisplayName("방류하천_저서동물")] public int BenthosDischargeDistinctCount { get; set; }
    [DisplayName("생태현황_저서동물")] public int BenthosEcologyCount { get; set; }
    [DisplayName("생태현황_저서동물")] public int BenthosEcologyDistinctCount { get; set; }

    [DisplayName("방류하천_조류")] public int BirdDischargeCount { get; set; }
    [DisplayName("방류하천_조류")] public int BirdDischargeDistinctCount { get; set; }
    [DisplayName("생태현황_조류")] public int BirdEcologyCount { get; set; }
    [DisplayName("생태현황_조류")] public int BirdEcologyDistinctCount { get; set; }

    [DisplayName("방류하천_포유류")] public int MammalDischargeCount { get; set; }
    [DisplayName("방류하천_포유류")] public int MammalDischargeDistinctCount { get; set; }
    [DisplayName("생태현황_포유류")] public int MammalEcologyCount { get; set; }
    [DisplayName("생태현황_포유류")] public int MammalEcologyDistinctCount { get; set; }

    [DisplayName("방류하천_식물상")] public int PlantDischargeCount { get; set; }
    [DisplayName("방류하천_식물상")] public int PlantDischargeDistinctCount { get; set; }
    [DisplayName("생태현황_식물상")] public int PlantEcologyCount { get; set; }
    [DisplayName("생태현황_식물상")] public int PlantEcologyDistinctCount { get; set; }

    [DisplayName("방류하천_식생단면")] public int VegetationENVDischargeCount { get; set; }
    [DisplayName("방류하천_식생단면")] public int VegetationENVDischargeDistinctCount { get; set; }
    [DisplayName("생태현황_식생단면")] public int VegetationENVEcologyCount { get; set; }
    [DisplayName("생태현황_식생단면")] public int VegetationENVEcologyDistinctCount { get; set; }

    [DisplayName("방류하천_양서파충류")] public int AmphibianReptileDischargeCount { get; set; }
    [DisplayName("방류하천_양서파충류")] public int AmphibianReptileDischargeDistinctCount { get; set; }
    [DisplayName("생태현황_양서파충류")] public int AmphibianReptileEcologyCount { get; set; }
    [DisplayName("생태현황_양서파충류")] public int AmphibianReptileEcologyDistinctCount { get; set; }

    [DisplayName("방류하천_서식수변")] public int HabitatWaterEdgeDischargeCount { get; set; }
    [DisplayName("방류하천_서식수변")] public int HabitatWaterEdgeDischargeDistinctCount { get; set; }
    [DisplayName("생태현황_서식수변")] public int HabitatWaterEdgeEcologyCount { get; set; }
    [DisplayName("생태현황_서식수변")] public int HabitatWaterEdgeEcologyDistinctCount { get; set; }

    [DisplayName("방류하천_수질")] public int WaterQualityDischargeCount { get; set; }
    [DisplayName("방류하천_수질")] public int WaterQualityDischargeDistinctCount { get; set; }
    [DisplayName("생태현황_수질")] public int WaterQualityEcologyCount { get; set; }
    [DisplayName("생태현황_수질")] public int WaterQualityEcologyDistinctCount { get; set; }
}
