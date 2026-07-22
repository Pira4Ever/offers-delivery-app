using OffersDelivery.Core.Enums;
using OffersDelivery.Core.Models;
using SQLite;

namespace OffersDelivery.Core.Repositories;

public class OfferRepository
{
    private SQLiteAsyncConnection? _conn;

    private async Task Init()
    {
        if (_conn is not null) return;

        _conn = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "Offers.db"), SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _conn.CreateTableAsync<OfferModel>();
    }

    public static int MarketToInteger(Market market)
    {
        return market switch
        {
            Market.ROLDAO => 0,
            Market.PAGUE_MENOS => 1,
            Market.SAO_VICENTE => 2,
            Market.TENDA => 3,
            Market.DELTA => 4,
            _ => throw new NotImplementedException()
        };
    }

    public async Task<List<OfferModel>> GetOffersByMarket(Market market)
    {
        await Init();
        string query;
        if (market == Market.TENDA)
            query = "SELECT Market, OfferGroup, DueDate, Type, GROUP_CONCAT(Url, ',') AS Url FROM (SELECT OfferGroup, DueDate, Url, Market, Type FROM Offers WHERE Market = ? AND Type = 1 ORDER BY PageOrder ASC) GROUP BY OfferGroup, DueDate ORDER BY DueDate DESC;";
        else
            query = "SELECT Type, Url, DueDate FROM Offers WHERE Market = ?;";

        return await _conn!.QueryAsync<OfferModel>(query, MarketToInteger(market));
    }

    public async Task InsertOffer(bool isImage, OfferModel offer)
    {
        await Init();
        if (isImage)
        {
            string query = "INSERT INTO Offers (Id, Market, Type, Url, DueDate, PageOrder, OfferGroup) VALUES (?, ?, ?, ?, ?, ?, ?) ON CONFLICT(Id) DO NOTHING;";
            await _conn!.ExecuteAsync(query, offer.Id, offer.Market, offer.Type, offer.Url, offer.DueDate, offer.PageOrder, offer.OfferGroup);
        }
        else
        {
            string query = "INSERT INTO Offers (Id, Market, Type, Url, DueDate) VALUES (?, ?, ?, ?, ?) ON CONFLICT(Id) DO NOTHING;";
            await _conn!.ExecuteAsync(query, offer.Id, offer.Market, offer.Type, offer.Url, offer.DueDate);
        }
    }

    public async Task<List<OfferModel>> DeleteOld()
    {
        await Init();
        var offers = await _conn!.QueryAsync<OfferModel>("SELECT Type, Url, DueDate FROM Offers WHERE DueDate < date('now', '-1 day');");
        await _conn.ExecuteAsync("DELETE FROM Offers WHERE DueDate < date('now', '-1 day');");
        return offers;
    }
}
