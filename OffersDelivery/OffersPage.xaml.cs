using MauiNativePdfView.Abstractions;
using OffersDelivery.Core;
using OffersDelivery.Core.Dtos;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text;

namespace OffersDelivery;

[QueryProperty(nameof(EncodedName), "encodedNameParam")]
[QueryProperty(nameof(MarketName), "marketNameParam")]
public partial class OffersPage : ContentPage
{
    private readonly ApiClient _apiClient;
    private string _encodedName = "";
    private string _marketName = "";
    private bool _isContextLoading = false;
    private List<GetOffersResponseDto>? offers;
    private readonly HttpClient client;
    private int index = 0;
    private double currentScale = 1.0;
    private const double ZoomStep = 0.2;
    private const double MaxZoom = 3.0;
    private const double MinZoom = 1.0;

    private double originalWidth = 0;
    private double originalHeight = 0;
    private int pageCount = 1;

    public string EncodedName
    {
        get => _encodedName;
        set
        {
            _encodedName = value;
            OnPropertyChanged();
        }
    }

    public string MarketName
    {
        get => _marketName;
        set
        {
            _marketName = value;
            Title = value;
            OnPropertyChanged();
        }
    }

    public OffersPage(ApiClient apiClient)
	{
		InitializeComponent();
        BindingContext = this;
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                if (message.RequestUri?.Host?.EndsWith("roldao.com.br", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }
                return errors == SslPolicyErrors.None;
            }
        };
        client = new HttpClient(handler);
        _apiClient = apiClient;
        ZoomScrollView.SizeChanged += (s, e) =>
        {
            if (currentScale == 1.0)
            {
                ApplyZoom();
            }
        };
    }

    private async void OnPreviousPage(object sender, EventArgs e)
    {
        if (offers!.Count == 1) return;
        index--;
        if (index < 0) index = offers!.Count - 1;
        ChangePage();
    }

    private async void OnNextPage(object sender, EventArgs e)
    {
        originalWidth = ZoomScrollView.Width;
        originalHeight = ZoomScrollView.Height;
        if (offers!.Count == 1) return;
        index++;
        if (index > offers!.Count - 1) index = 0;
        ChangePage();
    }

    private async void ChangePage()
    {
        LoadingOverlay.IsVisible = true;
        Controls.IsVisible = false;
        if (offers![index].Type == "pdf")
        {
            var bytes = await GetPdfFromCacheAsync(offers![index].Url);
            pdfViewer.Source = PdfSource.FromBytes(bytes);
            pdfViewer.Reload();
            pdfViewer.GoToPage(0);
            pdfViewer.IsVisible = true;
            ZoomScrollView.IsVisible = false;
        }
        else if (offers![index].Type == "image")
        {
            List<string> sources = [];
            foreach (var image in offers![index].Pages) sources.Add(await GetImageFromCacheAsync(image));
            pageCount = sources.Count;
            imageViewer.ItemsSource = sources;
            pdfViewer.IsVisible = false;
            ZoomScrollView.IsVisible = true;
            currentScale = 1.0;
            originalHeight = 0;
            originalWidth = 0;
            ApplyZoom();
        }
        LoadingOverlay.IsVisible = false;
        Controls.IsVisible = true;
    }

    private void OnZoomIn(object sender, EventArgs e)
    {
        if (pdfViewer.IsVisible)
            pdfViewer.Zoom = Math.Min(pdfViewer.Zoom + 0.5f, pdfViewer.MaxZoom);
        else
            if (currentScale < MaxZoom)
            {
                currentScale += ZoomStep;
                ApplyZoom();
            }
    }

    private void OnZoomOut(object sender, EventArgs e)
    {
        if (pdfViewer.IsVisible)
            pdfViewer.Zoom = Math.Max(pdfViewer.Zoom - 0.5f, pdfViewer.MinZoom);
        else
            if (currentScale > MinZoom)
            {
                currentScale -= ZoomStep;
                ApplyZoom();
            }
    }

    private void ApplyZoom()
    {
        ZoomContainer.AnchorX = 0;
        ZoomContainer.AnchorY = 0;
        ZoomContainer.Scale = currentScale;

        if (originalWidth == 0)
        {
            originalWidth = ZoomScrollView.Width;
            originalHeight = ZoomScrollView.Height;
        }

        if (currentScale > 1.0)
        {
            ZoomContainer.WidthRequest = originalWidth * currentScale;
            ZoomContainer.HeightRequest = originalHeight * currentScale * ((pageCount == 1) ? 1 : 1.75 * (pageCount - 1));
        }
        else
        {
            ZoomContainer.WidthRequest = originalWidth;
            ZoomContainer.HeightRequest = originalHeight;
        }

        Console.WriteLine(ZoomContainer.Height);
        Console.WriteLine(ZoomContainer.HeightRequest);
    }

    private async Task<byte[]> GetPdfFromCacheAsync(string url)
    {
        string fileName = HashUrl(url) + ".pdf";
        string cachePath = Path.Combine(FileSystem.CacheDirectory, fileName);

        if (File.Exists(cachePath))
        {
            return await File.ReadAllBytesAsync(cachePath);
        }

        var bytes = await client.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(cachePath, bytes);

        return bytes;
    }

    private async Task<string> GetImageFromCacheAsync(string url)
    {
        var hash = HashUrl(url);

        var extension = Path.GetExtension(new Uri(url).AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".jpg";

        var fileName = $"{hash}{extension}";
        var localPath = Path.Combine(FileSystem.CacheDirectory, fileName);

        if (File.Exists(localPath))
            return localPath;

        var bytes = await client.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(localPath, bytes);

        return localPath;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isContextLoading) return;
        _isContextLoading = true;

        try
        {
            LoadingOverlay.IsVisible = true;
            Controls.IsVisible = false;
            offers = await _apiClient.GetOffersFromMarket(_encodedName);

            if (offers is not null && offers.Count > 0)
            {
                if (offers![0].Type == "pdf")
                {
                    var bytes = await GetPdfFromCacheAsync(offers![0].Url);
                    pdfViewer.Source = PdfSource.FromBytes(bytes);
                    pdfViewer.Reload();
                    pdfViewer.IsVisible = true;
                    ZoomScrollView.IsVisible = false;
                }
                else if (offers![0].Type == "image")
                {
                    List<string> sources = [];
                    foreach (var image in offers![0].Pages) sources.Add(await GetImageFromCacheAsync(image));
                    pageCount = sources.Count;
                    imageViewer.ItemsSource = sources;
                    pdfViewer.IsVisible = false;
                    ZoomScrollView.IsVisible = true;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro: {ex.Message}");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
            Controls.IsVisible = true;
            _isContextLoading = false;
        }
    }

    private static string HashUrl(string url)
    {
        return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(url)));
    }

    private async void OnShareClicked(object sender, EventArgs e)
    {
        if (offers![index].Type == "pdf")
        {
            string originalPath = Path.Combine(FileSystem.CacheDirectory, HashUrl(offers![index].Url) + ".pdf");
            string newPath = Path.Combine(FileSystem.CacheDirectory, $"Ofertas - {_marketName}.pdf");
            try
            {
                File.Copy(originalPath, newPath, overwrite: true);

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Compartilhar Ofertas",
                    File = new ShareFile(newPath)
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex}");
            }
        }
        else
        {
            List<ShareFile> files = [];
            int counter = 1;
            foreach (string url in offers![index].Pages)
            {
                var hash = HashUrl(url);

                string extension = Path.GetExtension(new Uri(url).AbsolutePath);
                if (string.IsNullOrWhiteSpace(extension))
                    extension = ".jpg";

                var fileName = $"{hash}{extension}";
                string originalPath = Path.Combine(FileSystem.CacheDirectory, fileName);
                string newPath = Path.Combine(FileSystem.CacheDirectory, $"Ofertas - {_marketName}-{counter}{extension}");
                File.Copy(originalPath, newPath, overwrite: true);
                files.Add(new ShareFile(newPath));
                counter++;
            }
            try
            {
                await Share.Default.RequestAsync(new ShareMultipleFilesRequest
                {
                    Title = "Compartilhar Ofertas",
                    Files = files
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex}");
            }
        }
    }
}