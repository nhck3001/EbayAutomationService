
public class Sku
{
    public int Id { get; set; }

    public string SkuCode { get; set; } = null!;
    public string Pid { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;

    public string[] ImageUrls { get; set; } = null!;

    // Store raw JSON string
    public string ItemSpecifics { get; set; } = null!;

    public decimal SellPrice { get; set; }

    public SkuStatus SkuStatus { get; set; } = SkuStatus.Pending;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Listing? Listing { get; set; }
}
public enum SkuStatus
{
    Pending,
    InventoryCreated,
    OfferCreated,
    Published,
    Failed,
    Rejected
}