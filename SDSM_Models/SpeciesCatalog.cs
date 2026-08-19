using System;
using System.Collections.Generic;

namespace SDSM_Models;

/// <summary>
/// 조사자 앱 종목록 카탈로그(species.json).
/// 관리자 마스터(FishSpeciesList·BenthosSpeciesList + 조류/포유류/양서파충류 출현종)를 내보내고,
/// 조사자 앱이 로드/교체한다. 하드코딩 대신 이 파일로 종목록을 관리한다.
/// </summary>
public class SpeciesCatalog
{
    public string Version { get; set; } = string.Empty;   // 예: admin-20260722
    public DateTime GeneratedAt { get; set; }

    public List<FishSpeciesList> Fish { get; set; } = new();       // 어류 마스터(길드·보호종 포함)
    public List<BenthosSpeciesList> Benthos { get; set; } = new(); // 저서 마스터(오탁치·가중치 포함)
    public List<string> Bird { get; set; } = new();                // 조류 국명
    public List<string> Mammal { get; set; } = new();              // 포유류 국명
    public List<string> Amphibian { get; set; } = new();           // 양서파충류 국명
}
