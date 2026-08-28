using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OffersDelivery.Core.Dtos;

public class RoldaoApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public required RoldaoData Data { get; set; }
}

public class RoldaoData
{
    [JsonPropertyName("url")]
    public required string Url { get; set; }
}