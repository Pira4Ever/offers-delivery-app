using System.Text.Json.Serialization;

namespace OffersDelivery.Core.Dtos;

public class GetOffersResponseDto
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("due_date")]
    public DateTime DueDate { get; set; }

    [JsonPropertyName("offer_group")]
    public string OfferGroup { get; set; } = string.Empty;

    [JsonPropertyName("pages")]
    public List<string> Pages { get; set; } = [];
}
