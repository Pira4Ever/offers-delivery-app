#if ANDROID
using Android.Content;
using AndroidX.Core.Content;
#endif

namespace OffersDelivery.Core.Services;
public class UpdateService
    {
        private readonly HttpClient _httpClient = new();

        public async Task DownloadAndInstallUpdateAsync(string apkUrl)
        {
            try
            {
                string fileName = "offers_delivery_update.apk";
                string localPath = Path.Combine(FileSystem.Current.CacheDirectory, fileName);

                using var response = await _httpClient.GetAsync(apkUrl);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = File.Create(localPath);
                await stream.CopyToAsync(fileStream);

                fileStream.Close();

#if ANDROID
                InstallApkAndroid(localPath);
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update failed: {ex.Message}");
            }
        }

    public async Task<double> GetLatestVersion()
    {
        return Double.Parse(await _httpClient.GetStringAsync("https://github.com/Pira4Ever/offers-delivery-app/releases/latest/download/version.txt"));
    }

#if ANDROID
        private static void InstallApkAndroid(string apkPath)
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            Java.IO.File file = new(apkPath);

            string authority = $"{context.PackageName}.fileprovider";
            Android.Net.Uri apkUri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, authority, file)!;

            Intent intent = new(Intent.ActionView);
            intent.SetDataAndType(apkUri, "application/vnd.android.package-archive");

            intent.SetFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask | ActivityFlags.ClearTop);

            context.StartActivity(intent);
        }
#endif
}