using AngleSharp;
using LaYumba.Functional;
using Microsoft.VisualBasic;
using OffersDelivery.Core.Dtos;
using OffersDelivery.Core.Models;
using OffersDelivery.Core.Repositories;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
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
        markets.Add(new GetMarketsResponseDto { EncodedName = "DELTA", Name = "Delta Supermercados", Url = "delta.png" });
        markets.Add(new GetMarketsResponseDto { EncodedName = "SAO_ROQUE", Name = "São Roque Supermercados", Url = "sao_roque.jpg" });
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
            "DELTA" => (await ScrapDelta()).Match(
                            Exception: ex => throw ex,
                            Success: response => response
                            ),
            "SAO_ROQUE" => (await ScrapSaoRoque()).Match(
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
                var resp = await _httpClient.GetAsync(@"https://roldao.com.br/ofertas/");
                var html = await resp.Content.ReadAsStringAsync();
                var context = BrowsingContext.New(Configuration.Default);
                var doc = await context.OpenAsync(req => req.Content(html));
                var divs = doc.QuerySelectorAll("#ofertasnow > div > div > div .jet-listing-grid__item[data-post-id]");

                List<GetOffersResponseDto> offers = [];
                List<OfferModel> offerModels = [];

                foreach (var item in divs)
                {
                    var postId = item.GetAttribute("data-post-id");
                    var response = await _httpClient.PostAsync("https://roldao.com.br/wp-admin/admin-ajax.php", new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            { "action", "elem_get_pdf_oferta_iframe" },
                            { "post_id", postId! }
                        }));

                    var jsonResp = await response.Content.ReadFromJsonAsync<RoldaoApiResponse>();
                    string dateString;
                    try
                    {
                        dateString = item.QuerySelector(".periodo-oferta")!.TextContent.Split(" a ")[1];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        dateString = item.QuerySelector(".periodo-oferta")!.TextContent;
                    }
                    var dueDate = DateTime.ParseExact(dateString, "dd.MM", CultureInfo.InvariantCulture);
                    dueDate = dueDate < DateTime.Today ? dueDate.AddYears(1) : dueDate;

                    offers.Add(new GetOffersResponseDto
                    {
                        DueDate = dueDate,
                        Type = "pdf",
                        Url = jsonResp!.Data.Url
                    });

                    offerModels.Add(new OfferModel
                    {
                        DueDate = dueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        Market = OfferRepository.MarketToInteger(Enums.Market.ROLDAO),
                        Url = jsonResp!.Data.Url,
                        Type = 0,
                        Id = await GetHash(jsonResp!.Data.Url)
                    });
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
                    OfferGroup = item.OfferGroup!,
                    Pages = [.. item.Url.Split(',')]
                });
            }

            return offers;
        }
    }

    private async Task<Exceptional<List<GetOffersResponseDto>>> ScrapDelta()
    {
        var lastUpdate = Preferences.Default.Get("last_delta", DateTime.Now.AddHours(-2));
        if (DateTime.Now >= lastUpdate.AddHours(1) && Connectivity.Current.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.Internet)
        {
            try
            {
                var resp = await _httpClient.GetAsync(@"https://www.deltasuper.com.br/ofertas-salto/");
                if (!resp.IsSuccessStatusCode) return new List<GetOffersResponseDto>();
                var html = await resp.Content.ReadAsStringAsync();
                var context = BrowsingContext.New(Configuration.Default);
                var doc = await context.OpenAsync(req => req.Content(html));
                List<GetOffersResponseDto> offers = [];
                List<OfferModel> offerModels = [];
                foreach (var item in doc.QuerySelectorAll(".jet-listing-grid__items")[0].Children)
                {
                    string dueDate;
                    DateTime date;
                    var regex = DateRegex().Matches(item.QuerySelectorAll("h2")[item.QuerySelectorAll("h2").Count - 1].TextContent.ToLower());
                    if (regex.Count == 0) date = DateTime.Now.AddDays(5).Date;
                    else
                    {
                        dueDate = regex[0].Value;
                        date = DateTime.ParseExact(dueDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);
                    }

                    dueDate = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var link = item.GetElementsByTagName("a")[0].GetAttribute("href")!;

                    var response = await _httpClient.GetAsync(link);
                    var html1 = await response.Content.ReadAsStringAsync();
                    var doc1 = await context.OpenAsync(req => req.Content(html1));

                    List<string> pages = [];
                    string id = Guid.NewGuid().ToString();
                    int counter = 0;
                    foreach (var item1 in doc1.QuerySelectorAll(".gallery-icon a"))
                    {
                        string url = item1.GetAttribute("href")!;
                        offerModels.Add(new OfferModel
                        {
                            Id = await GetHash(url),
                            DueDate = dueDate,
                            Market = OfferRepository.MarketToInteger(Enums.Market.DELTA),
                            OfferGroup = id,
                            PageOrder = counter,
                            Type = 1,
                            Url = url
                        });
                        pages.Add(url);
                        counter++;
                    }
                    offers.Add(new GetOffersResponseDto
                    {
                        Pages = pages,
                        DueDate = date,
                        Type = "image",
                        OfferGroup = id
                    });
                }

                foreach (var item in offerModels)
                {
                    await _repository.InsertOffer(true, item);
                }

                Preferences.Default.Set("last_delta", DateTime.Now);
                return offers;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
        else
        {
            var offerModels = await _repository.GetOffersByMarket(Enums.Market.DELTA);
            List<GetOffersResponseDto> offers = [];
            foreach (var item in offerModels)
            {
                offers.Add(new GetOffersResponseDto
                {
                    DueDate = DateTime.Parse(item.DueDate),
                    Type = item.Type == 0 ? "pdf" : "image",
                    OfferGroup = item.OfferGroup!,
                    Pages = [.. item.Url.Split(',')]
                });
            }

            return offers;
        }
    }

    private async Task<Exceptional<List<GetOffersResponseDto>>> ScrapSaoRoque()
    {
        var lastUpdate = Preferences.Default.Get("last_sao_roque", DateTime.Now.AddHours(-2));
        if (DateTime.Now >= lastUpdate.AddHours(1) && Connectivity.Current.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.Internet)
        {
            var resp = await _httpClient.GetAsync(@"https://www.smsr.com.br/sr/ofertas/");
            var html = await resp.Content.ReadAsStringAsync();
            var context = BrowsingContext.New(Configuration.Default);
            var doc = await context.OpenAsync(req => req.Content(html));
            var anchors = doc.QuerySelectorAll(".gallery-item a");

            List<GetOffersResponseDto> offers = [];
            List<OfferModel> offerModels = [];

            foreach (var anchor in anchors)
            {
                string id = Guid.NewGuid().ToString();
                List<string> pages = [];
                offerModels.Add(new OfferModel
                {
                    Id = await GetHash(anchor.GetAttribute("href")!),
                    DueDate = DateTime.Now.AddDays(5).Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Market = OfferRepository.MarketToInteger(Enums.Market.SAO_ROQUE),
                    OfferGroup = id,
                    PageOrder = 1,
                    Type = 1,
                    Url = anchor.GetAttribute("href")!
                });
                pages.Add(anchor.GetAttribute("href")!);

                offers.Add(new GetOffersResponseDto
                {
                    Pages = pages,
                    DueDate = DateTime.Now.AddDays(5).Date,
                    Type = "image",
                    OfferGroup = id
                });
            }

            foreach (var item in offerModels)
            {
                await _repository.InsertOffer(true, item);
            }
            Preferences.Default.Set("last_sao_roque", DateTime.Now);
            return offers;
        }
        else
        {
            var offerModels = await _repository.GetOffersByMarket(Enums.Market.SAO_ROQUE);
            List<GetOffersResponseDto> offers = [];
            foreach (var item in offerModels)
            {
                offers.Add(new GetOffersResponseDto
                {
                    DueDate = DateTime.Parse(item.DueDate),
                    Type = item.Type == 0 ? "pdf" : "image",
                    OfferGroup = item.OfferGroup!,
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
                path = Path.Combine(Microsoft.Maui.Storage.FileSystem.CacheDirectory, fileName);
            }
            else
            {
                var hash = HashUrl(offer.Url);

                var extension = Path.GetExtension(new Uri(offer.Url).AbsolutePath);
                if (string.IsNullOrWhiteSpace(extension))
                    extension = ".jpg";

                var fileName = $"{hash}{extension}";
                path = Path.Combine(Microsoft.Maui.Storage.FileSystem.CacheDirectory, fileName);
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
