using System.Text.Json.Serialization;

namespace OffersDelivery.Core.Dtos;

public class GetMarketsResponseDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("encoded_name")]
    public string EncodedName { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
