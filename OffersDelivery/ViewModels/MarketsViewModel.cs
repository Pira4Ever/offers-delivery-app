using OffersDelivery.Core;
using OffersDelivery.Core.Dtos;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace OffersDelivery.ViewModels;

public partial class MarketsViewModel : INotifyPropertyChanged
{
    public ObservableCollection<GetMarketsResponseDto> Markets { get; set; } = [];
    public ICommand ChangePageCommand { get; }

    public MarketsViewModel()
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
            }
        };

        LoadMarketsCommand = new Command(async () => await LoadMarketsAsync());
        ChangePageCommand = new Command<string>(async (market) =>
        {
            string marketName = "";
            foreach (var item in Markets)
            {
                if (item.EncodedName == market)
                {
                    marketName = item.Name;
                    break;
                }
            }
            await Shell.Current.GoToAsync($"{nameof(OffersPage)}?encodedNameParam={market}&marketNameParam={marketName}");
        });
    }

    public ICommand LoadMarketsCommand { get; }

    private async Task LoadMarketsAsync()
    {
        var lista = ApiClient.GetMarkets();

        Markets.Clear();
        foreach (var item in lista)
        {
            Markets.Add(item);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
