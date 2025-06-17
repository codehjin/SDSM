using System.Text.Json.Serialization;

namespace SDSM_App.Messengers;

public class JSMessage
{
    [JsonPropertyName("title")] public string Title { get; set; } = ""; // 제목
    [JsonPropertyName("lng")] public double Lng { get; set; } // 경도
    [JsonPropertyName("lat")] public double Lat { get; set; } // 위도
    [JsonPropertyName("address")] public string Address { get; set; } = ""; // 주소
    [JsonPropertyName("contents")] public string Contents { get; set; } = ""; // 내용
}
