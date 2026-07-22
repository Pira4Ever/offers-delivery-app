using SQLite;

namespace OffersDelivery.Core.Models;

[Table("Offers")]
public class OfferModel
{
    [PrimaryKey, Column("Id")]
    public string Id { get; set; } = "";

    [Column("Market")]
    public int Market { get; set; }

    [Column("Type")]
    public int Type { get; set; }

    [Column("Url")]
    public string Url { get; set; } = "";

    [Column("DueDate")]
    public string DueDate { get; set; } = "";

    [Column("PageOrder")]
    public int PageOrder { get; set; }

    [Column("OfferGroup")]
    public string? OfferGroup { get; set; }
}
