using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SDSM_Models;

/// <summary>
/// 조사자 앱 종목록 카탈로그(species.json).
/// 관리자 마스터(FishSpeciesList·BenthosSpeciesList) + 조류/포유류/양서파충류 공식 종목록을 담는다.
/// 하드코딩 대신 이 파일로 종목록을 관리한다.
/// ※ 관찰형 3종은 국가생물종목록(국립생물자원관) 기반이며 `tools\build_species_json.py` 로 생성한다.
/// </summary>
public class SpeciesCatalog
{
    public string Version { get; set; } = string.Empty;   // 예: nibr-20260820
    public DateTime GeneratedAt { get; set; }

    public List<FishSpeciesList> Fish { get; set; } = new();       // 어류 마스터(길드·보호종 포함)
    public List<BenthosSpeciesList> Benthos { get; set; } = new(); // 저서 마스터(오탁치·가중치 포함)

    // 관찰형 3종 : 국명·학명·목·과·보호종. 구형 파일(국명 문자열 배열)도 그대로 읽힌다.
    [JsonConverter(typeof(ObservedSpeciesListConverter))]
    public List<ObservedSpecies> Bird { get; set; } = new();       // 조류

    [JsonConverter(typeof(ObservedSpeciesListConverter))]
    public List<ObservedSpecies> Mammal { get; set; } = new();     // 포유류

    [JsonConverter(typeof(ObservedSpeciesListConverter))]
    public List<ObservedSpecies> Amphibian { get; set; } = new();  // 양서파충류(양서류+파충류 통합)
}

/// <summary>
/// 관찰형(조류·포유류·양서파충류) 종목록 1행.
/// 어류·저서동물처럼 지수 계산에 쓰는 형질값은 없고, 식별·보호종 표기에 필요한 항목만 둔다.
/// </summary>
public class ObservedSpecies
{
    /// <summary>대표국명(식별자). 원문 그대로 보존한다.</summary>
    public string SpeciesKo { get; set; } = string.Empty;

    /// <summary>학명(완성형).</summary>
    public string? SpeciesEn { get; set; }

    /// <summary>목 국명.</summary>
    public string? OrderKo { get; set; }

    /// <summary>과 국명.</summary>
    public string? FamilyKo { get; set; }

    /// <summary>멸종위기 야생생물 Ⅰ급 ("O").</summary>
    public string? Endangered1 { get; set; }

    /// <summary>멸종위기 야생생물 Ⅱ급 ("O").</summary>
    public string? Endangered2 { get; set; }

    /// <summary>천연기념물 ("O").</summary>
    public string? NaturalMonument { get; set; }

    /// <summary>생태계교란생물 ("O"). 보호종과 의미가 반대이므로 표기를 섞지 않는다.</summary>
    public string? Invasive { get; set; }

    /// <summary>국가생물종번호(KTSN). 원본 목록 갱신 시 대조용.</summary>
    public string? Ktsn { get; set; }

    /// <summary>법정보호종 여부(멸종위기 Ⅰ·Ⅱ급 또는 천연기념물).</summary>
    [JsonIgnore]
    public bool IsProtected =>
        !string.IsNullOrWhiteSpace(Endangered1)
        || !string.IsNullOrWhiteSpace(Endangered2)
        || !string.IsNullOrWhiteSpace(NaturalMonument);

    /// <summary>생태계교란생물 여부.</summary>
    [JsonIgnore]
    public bool IsInvasive => !string.IsNullOrWhiteSpace(Invasive);

    public override string ToString() => SpeciesKo;
}

/// <summary>
/// 관찰형 종목록의 **하위 호환 역직렬화기**.
/// 구형 species.json 은 <c>["흰뺨검둥오리", "왜가리"]</c> 처럼 국명 문자열 배열이었다.
/// 문자열이면 SpeciesKo 만 채운 객체로 바꿔 읽어, 구형 파일을 쓰는 배포본이 종목록을 잃지 않게 한다.
/// </summary>
public sealed class ObservedSpeciesListConverter : JsonConverter<List<ObservedSpecies>>
{
    public override List<ObservedSpecies> Read(ref Utf8JsonReader reader, Type typeToConvert,
                                               JsonSerializerOptions options)
    {
        var list = new List<ObservedSpecies>();
        if (reader.TokenType == JsonTokenType.Null) return list;
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("종목록은 배열이어야 합니다.");

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.EndArray:
                    return list;

                // 구형 : 국명 문자열
                case JsonTokenType.String:
                    var ko = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(ko))
                        list.Add(new ObservedSpecies { SpeciesKo = ko.Trim() });
                    break;

                // 신형 : 객체
                case JsonTokenType.StartObject:
                    var item = JsonSerializer.Deserialize<ObservedSpecies>(ref reader, options);
                    if (item is not null && !string.IsNullOrWhiteSpace(item.SpeciesKo))
                        list.Add(item);
                    break;

                case JsonTokenType.Null:
                    break;

                default:
                    throw new JsonException($"종목록 항목이 문자열도 객체도 아닙니다: {reader.TokenType}");
            }
        }
        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<ObservedSpecies> value,
                               JsonSerializerOptions options)
    {
        // 저장은 항상 신형(객체 배열)으로 한다.
        writer.WriteStartArray();
        foreach (var item in value) JsonSerializer.Serialize(writer, item, options);
        writer.WriteEndArray();
    }
}
