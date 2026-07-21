using OffersDelivery.Core;

namespace OffersDelivery
{
    public partial class App : Application
    {
        private readonly ApiClient _apiClient;
        public App(ApiClient apiClient)
        {
            _apiClient = apiClient;
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
    }
}