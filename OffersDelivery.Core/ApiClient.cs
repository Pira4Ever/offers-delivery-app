using AngleSharp;
using LaYumba.Functional;
using OffersDelivery.Core.Dtos;
using OffersDelivery.Core.Models;
using OffersDelivery.Core.Repositories;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace OffersDelivery.Core;

public partial class ApiClient(HttpClient httpClient, OfferRepository repository)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly OfferRepository _repository = repository;
    public static List<GetMarketsResponseDto> GetMarkets()
    {
        List<GetMarketsResponseDto> markets = [];
        markets.Add(new GetMarketsResponseDto { EncodedName = "ROLDAO", Name = "Roldão Atacadista", Url = "roldao.png" });
        markets.Add(new GetMarketsResponseDto { EncodedName = "SAO_VICENTE", Name = "São Vicente", Url = "sao_vicente.png" });
        markets.Add(new GetMarketsResponseDto { EncodedName = "PAGUE_MENOS", Name = "Pague Menos", Url = "pague_menos.jpg" });
        markets.Add(new GetMarketsResponseDto { EncodedName = "TENDA", Name = "Tenda Atacado", Url = "tenda.png" });
        return markets;
    }

    public async Task<List<GetOffersResponseDto>> GetOffersFromMarket(string market)
    {
        return market switch
        {
            "ROLDAO" => (await ScrapRoldao()).Match(
                            Exception: ex => throw ex,
                            Success: response => response
                            ),
            "SAO_VICENTE" => (await ScrapSaoVicente()).Match(
                            Exception: ex => throw ex,
                            Success: response => response
                            ),
            "PAGUE_MENOS" => (await ScrapPagueMenos()).Match(
                            Exception: ex => throw ex,
                            Success: response => response
                            ),
            "TENDA" => (await ScrapTenda()).Match(
                            Exception: ex => throw ex,
                            Success: response => response
                            ),
            _ => [],
        };
    }

    private async Task<Exceptional<List<GetOffersResponseDto>>> ScrapRoldao()
    {
        var lastUpdate = Preferences.Default.Get("last_roldao", DateTime.Now.AddHours(-2));
        if (DateTime.Now >= lastUpdate.AddHours(1) && Connectivity.Current.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.Internet)
        {
            try
            {
                var resp = await _httpClient.GetAsync(@"https://roldao.com.br/ofertas-do-roldao/");
                var statusCode = resp.StatusCode;
                var html = await resp.Content.ReadAsStringAsync();
                var context = BrowsingContext.New(Configuration.Default);
                var doc = await context.OpenAsync(req => req.Content(html));
                var divs = doc.QuerySelectorAll(".post-item.isotope-item");

                List<GetOffersResponseDto> offers = [];
                List<OfferModel> offerModels = [];

                foreach (var item in divs)
                {
                    var text = item.QuerySelector(".post-excerpt")?.TextContent.ToLower()!;
                    if (!(text.Contains("salto") && text.Contains("exceto")))
                    {
                        var link = item.QuerySelector(".post-title")!.QuerySelector("a")!.GetAttribute("href");

                        var response = await _httpClient.GetAsync(link);
                        var statusCode1 = response.StatusCode;
                        var html1 = await response.Content.ReadAsStringAsync();
                        var pdfDoc = await context.OpenAsync(req => req.Content(html1));
                        string encodedJson;
                        try
                        {
                            encodedJson = pdfDoc.QuerySelector("#real3d_flipbook_embed-js-extra")!.InnerHtml!.Split("= \"")[1].Split("\";")[0];
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                        var json = encodedJson.Replace("\\", "");

                        var dueDate = DateRegex().Matches(text)[0].Value;
                        var date = DateTime.ParseExact(dueDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);

                        JsonNode dom = JsonNode.Parse(json)!;
                        var pdfLink = dom["pdfUrl"]?.GetValue<string>();
                        offers.Add(new GetOffersResponseDto
                        {
                            DueDate = date,
                            Type = "pdf",
                            Url = pdfLink!
                        });

                        offerModels.Add(new OfferModel
                        {
                            DueDate = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                            Market = OfferRepository.MarketToInteger(Enums.Market.ROLDAO),
                            Url = pdfLink!,
                            Type = 0,
                            Id = await GetHash(pdfLink!)
                        });
                    }
                }

                foreach (var item in offerModels)
                {
                    await _repository.InsertOffer(false, item);
                }
                Preferences.Default.Set("last_roldao", DateTime.Now);
                return offers;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
        else
        {
            var offerModels = await _repository.GetOffersByMarket(Enums.Market.ROLDAO);
            List<GetOffersResponseDto> offers = [];
            foreach (var item in offerModels)
            {
                offers.Add(new GetOffersResponseDto
                {
                    DueDate = DateTime.Parse(item.DueDate),
                    Type = item.Type == 0 ? "pdf" : "image",
                    Url = item.Url
                });
            }

            return offers;
        }
    }

    private async Task<Exceptional<List<GetOffersResponseDto>>> ScrapSaoVicente()
    {
        var lastUpdate = Preferences.Default.Get("last_sao_vicente", DateTime.Now.AddHours(-2));
        if (DateTime.Now >= lastUpdate.AddHours(1) && Connectivity.Current.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.Internet)
        {
            try
            {
                var resp = await _httpClient.GetAsync(@"https://www.svicente.com.br/ofertas");
                var statusCode = resp.StatusCode;
                var html = await resp.Content.ReadAsStringAsync();
                var context = BrowsingContext.New(Configuration.Default);
                var doc = await context.OpenAsync(req => req.Content(html));
                var anchors = doc.QuerySelectorAll("#Salto .viewFlyer__imageContainer.img_desktop a.is__desktop");
                List<GetOffersResponseDto> offers = [];
                List<OfferModel> offerModels = [];
                foreach (var item in anchors)
                {
                    var pdfLink = $"https://www.svicente.com.br{item.GetAttribute("href")!}";
                    offers.Add(new GetOffersResponseDto
                    {
                        DueDate = DateTime.Now.AddDays(5).Date,
                        Type = "pdf",
                        Url = pdfLink!
                    });
                    offerModels.Add(new OfferModel
                    {
                        DueDate = DateTime.Now.AddDays(5).Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        Market = OfferRepository.MarketToInteger(Enums.Market.SAO_VICENTE),
                        Url = pdfLink!,
                        Type = 0,
                        Id = await GetHash(pdfLink!)
                    });
                }
                foreach (var item in offerModels)
                {
                    await _repository.InsertOffer(false, item);
                }
                Preferences.Default.Set("last_sao_vicente", DateTime.Now);
                return offers;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
        else
        {
            var offerModels = await _repository.GetOffersByMarket(Enums.Market.SAO_VICENTE);
            List<GetOffersResponseDto> offers = [];
            foreach (var item in offerModels)
            {
                offers.Add(new GetOffersResponseDto
                {
                    DueDate = DateTime.Parse(item.DueDate),
                    Type = item.Type == 0 ? "pdf" : "image",
                    Url = item.Url
                });
            }

            return offers;
        }
    }

    private async Task<Exceptional<List<GetOffersResponseDto>>> ScrapPagueMenos()
    {
        var lastUpdate = Preferences.Default.Get("last_pague_menos", DateTime.Now.AddHours(-2));
        if (DateTime.Now >= lastUpdate.AddHours(1) && Connectivity.Current.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.Internet)
        {
            try
            {
                var resp = await _httpClient.GetAsync(@"https://www.superpaguemenos.com.br/jornal-de-ofertas/");
                var statusCode = resp.StatusCode;
                var html = await resp.Content.ReadAsStringAsync();
                var context = BrowsingContext.New(Configuration.Default);
                var doc = await context.OpenAsync(req => req.Content(html));
                var banners = doc.QuerySelectorAll(".showcase-shelf-banner");

                List<GetOffersResponseDto> offers = [];
                List<OfferModel> offerModels = [];
                foreach (var banner in banners)
                {
                    if (banner is null) continue;
                    if (banner.ParentElement!.GetAttribute("data-cidade")!.Contains("Salto - Loja 29"))
                    {
                        var anchors = banner.GetElementsByTagName("a");
                        if (anchors.Count < 1) continue;
                        var link = anchors[0].GetAttribute("href");
                        if (link!.StartsWith('/')) link = "https://www.superpaguemenos.com.br" + link;
                        offers.Add(new GetOffersResponseDto
                        {
                            DueDate = DateTime.Now.AddDays(5).Date,
                            Type = "pdf",
                            Url = link!,
                        });
                        offerModels.Add(new OfferModel
                        {
                            DueDate = DateTime.Now.AddDays(5).Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                            Market = OfferRepository.MarketToInteger(Enums.Market.PAGUE_MENOS),
                            Url = link!,
                            Type = 0,
                            Id = await GetHash(link!)
                        });
                    }
                }
                foreach (var item in offerModels)
                {
                    await _repository.InsertOffer(false, item);
                }
                Preferences.Default.Set("last_pague_menos", DateTime.Now);
                return offers;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
        else
        {
            var offerModels = await _repository.GetOffersByMarket(Enums.Market.PAGUE_MENOS);
            List<GetOffersResponseDto> offers = [];
            foreach (var item in offerModels)
            {
                offers.Add(new GetOffersResponseDto
                {
                    DueDate = DateTime.Parse(item.DueDate),
                    Type = item.Type == 0 ? "pdf" : "image",
                    Url = item.Url
                });
            }

            return offers;
        }
    }

    private async Task<Exceptional<List<GetOffersResponseDto>>> ScrapTenda()
    {
        var lastUpdate = Preferences.Default.Get("last_tenda", DateTime.Now.AddHours(-2));
        if (DateTime.Now >= lastUpdate.AddHours(1) && Connectivity.Current.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.Internet)
        {
            try
            {
                var resp = await _httpClient.GetAsync(@"https://api.tendaatacado.com.br/api/public/branch/flyers");
                if (resp.StatusCode == HttpStatusCode.NotFound) return new List<GetOffersResponseDto>();
                var json = await resp.Content.ReadFromJsonAsync<TendaApiResponse>();
                List<GetOffersResponseDto> offers = [];
                List<OfferModel> offerModels = [];
                foreach (var item in json!.Data)
                {
                    if (item.Branch.Name == "Salto")
                    {
                        string id = Guid.NewGuid().ToString();
                        List<string> pages = [];

                        foreach (var page in item.Pages)
                        {
                            offerModels.Add(new OfferModel
                            {
                                Id = await GetHash(page.Image!),
                                DueDate = DateTime.ParseExact(item.EndDate.Replace('-', '/'), "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                Market = OfferRepository.MarketToInteger(Enums.Market.TENDA),
                                OfferGroup = id,
                                PageOrder = page.Order,
                                Type = 1,
                                Url = page.Image!
                            });
                            pages.Add(page.Image!);
                        }

                        offers.Add(new GetOffersResponseDto
                        {
                            Pages = pages,
                            DueDate = DateTime.ParseExact(item.EndDate.Replace('-', '/'), "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None),
                            Type = "image",
                            OfferGroup = id
                        });
                        break;
                    }
                }

                foreach (var item in offerModels)
                {
                    await _repository.InsertOffer(true, item);
                }

                Preferences.Default.Set("last_tenda", DateTime.Now);
                return offers;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
        else
        {
            var offerModels = await _repository.GetOffersByMarket(Enums.Market.TENDA);
            List<GetOffersResponseDto> offers = [];
            foreach (var item in offerModels)
            {
                offers.Add(new GetOffersResponseDto
                {
                    DueDate = DateTime.Parse(item.DueDate),
                    Type = item.Type == 0 ? "pdf" : "image",
                    OfferGroup = item.OfferGroup,
                    Pages = [.. item.Url.Split(',')]
                });
            }

            return offers;
        }
    }

    private async Task<string> GetHash(string url)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();

        byte[] hashBytes = await SHA256.HashDataAsync(stream);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public async Task DeleteOld()
    {
        var offers = await _repository.DeleteOld();
        foreach (var offer in offers)
        {
            string path;
            if (offer.Type == 0)
            {
                string fileName = HashUrl(offer.Url) + ".pdf";
                path = Path.Combine(FileSystem.CacheDirectory, fileName);
            }
            else
            {
                var hash = HashUrl(offer.Url);

                var extension = Path.GetExtension(new Uri(offer.Url).AbsolutePath);
                if (string.IsNullOrWhiteSpace(extension))
                    extension = ".jpg";

                var fileName = $"{hash}{extension}";
                path = Path.Combine(FileSystem.CacheDirectory, fileName);
            }
            File.Delete(path);
        }
    }

    private static string HashUrl(string url)
    {
        return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(url)));
    }

    [GeneratedRegex(@"\b\d{2}/\d{2}/\d{4}\b")]
    private static partial Regex DateRegex();
}
