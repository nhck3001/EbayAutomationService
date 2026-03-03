
public class Sku
{
    public int Id { get; set; }

    public string SkuCode { get; set; } = null!;
    public string SkuStatus { get; set; } = SkuStatuses.Pending;

    public string Title { get; set; } = null!;

    public int Ebay_Category_Id { get; set; }
    public string Description { get; set; } = null!;

    public string[] ImageUrls { get; set; } = null!;

    // Store raw JSON string
    public string ItemSpecifics { get; set; } = null!;

    public decimal SellPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public int availableInventory { get; set; }


}
public static class SkuStatuses
{
    public const string Pending = "Pending";
    public const string InventoryCreatedid = "InventoryCreated";
    public const string Failed = "Failed";

}