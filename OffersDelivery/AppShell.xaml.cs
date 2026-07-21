namespace OffersDelivery
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(OffersPage), typeof(OffersPage));
        }
    }
}
