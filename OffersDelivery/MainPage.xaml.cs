using OffersDelivery.Core;
using OffersDelivery.ViewModels;
using System.Windows.Input;

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
}
