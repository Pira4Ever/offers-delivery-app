using MauiNativePdfView;
using Microsoft.Extensions.Logging;
using OffersDelivery.Core;
using OffersDelivery.Core.Repositories;
using System.Net;
using System.Net.Security;
using CommunityToolkit.Maui;

namespace OffersDelivery
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiNativePdfView()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton<HttpClient>(sp =>
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                    {
                        if (message.RequestUri?.Host?.EndsWith("roldao.com.br", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            return true;
                        }
                        return errors == SslPolicyErrors.None;
                    },
                    AutomaticDecompression =
                        DecompressionMethods.GZip |
                        DecompressionMethods.Deflate |
                        DecompressionMethods.Brotli,
                    CookieContainer = new CookieContainer()
                };

                var client = new HttpClient(handler);

                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                client.DefaultRequestHeaders.Add("Referer", "https://google.com");
                client.DefaultRequestHeaders.Add("Connection", "keep-alive");

                return client;
            });
            builder.Services.AddSingleton<OfferRepository>();
            builder.Services.AddSingleton<ApiClient>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
