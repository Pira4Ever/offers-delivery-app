using OffersDelivery.ViewModels;

namespace OffersDelivery;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is MarketsViewModel viewModel)
        {
            viewModel.LoadMarketsCommand.Execute(null);
        }
    }

    public void ChangeLoading() => LoadingOverlay.IsVisible = true;

}
