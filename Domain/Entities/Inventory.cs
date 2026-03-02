
public class InventoryItem
{
    public int Id { get; set; }

    public int SkuId { get; set; } // FK to SKU table

    public string Status { get; set; } = InventoryStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public int AvailableInventory { get; set; }
    // Navigation
    public Sku? sku{ get; set;}
}
public static class InventoryStatus
{
    public const string Pending = "Pending";
    public const string OfferCreated = "OfferCreated";
    public const string Failed = "Failed";

}