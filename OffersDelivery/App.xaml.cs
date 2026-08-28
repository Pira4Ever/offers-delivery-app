using OffersDelivery.Core;
using OffersDelivery.Core.Services;

namespace OffersDelivery
{
    public partial class App : Application
    {
        private readonly ApiClient _apiClient;
        private readonly UpdateService _updateService;
        public App(ApiClient apiClient, UpdateService updateService)
        {
            _apiClient = apiClient;
            _updateService = updateService;
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            _ = DeleteOld();
            return new Window(new AppShell());
        }

        private async Task DeleteOld()
        {
            await _apiClient.DeleteOld();
        }

        protected override void OnStart()
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;
            Task.Run(async () =>
            {
                try
                {
                    var appVersion = Double.Parse(AppInfo.Current.VersionString);
                    var latestVersion = await _updateService.GetLatestVersion();
                    if (latestVersion > appVersion)
                    {
                        await Task.Delay(2000);

                        var page = Windows[0].Page;

                        bool userAgreed = await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            if (page != null)
                            {
                                return await page.DisplayAlertAsync(
                                    "Atualização disponível",
                                    "Uma nova versão do aplicativo está disponível. Deseja atualizar agora?",
                                    "Atualizar",
                                    "Depois"
                                );
                            }
                            return false;
                        });

                        if (userAgreed)
                        {
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                if (page != null)
                                {
                                    if (page is MainPage mainPage)
                                    {
                                        mainPage.ChangeLoading();
                                    }
                                }
                            });
                            await _updateService.DownloadAndInstallUpdateAsync("https://github.com/Pira4Ever/offers-delivery-app/releases/latest/download/OffersDelivery.apk");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Startup update prompt failed: {ex.Message}");
                }
            });
        }
    }
}