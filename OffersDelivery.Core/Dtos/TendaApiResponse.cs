using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OffersDelivery.Core.Dtos;

public class TendaApiResponse
{
    [JsonPropertyName("Data")]
    public List<TendaMarket> Data { get; set; }
}

public class TendaMarket
{
    [JsonPropertyName("branch")]
    public TendaBranch Branch { get; set; }

    [JsonPropertyName("endDate")]
    public string EndDate { get; set; } = string.Empty;

    [JsonPropertyName("pages")]
    public List<TendaPage> Pages { get; set; }
}

public class TendaBranch
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class TendaPage
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;
}
