
public class Sku
{
    public int Id { get; set; }

    public string SkuCode { get; set; } = null!;
    public string Pid { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string? OfferId { get; set; } = null;
    public string Description { get; set; } = null!;

    public string[] ImageUrls { get; set; } = null!;

    // Store raw JSON string
    public string ItemSpecifics { get; set; } = null!;

    public decimal SellPrice { get; set; }

    public string SkuStatus { get; set; } = SkuStatuses.Pending;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Listing? Listing { get; set; }

}
public static class SkuStatuses
{
    public const string Pending = "PENDING";
    public const string InventoryCreatedid = "InventoryCreated";
    public const string Rejected = "REJECTED";

    public const string OfferCreated = "OfferCreated";
    public const string Published = "Published";
    public const string Failed = "Failed";

}