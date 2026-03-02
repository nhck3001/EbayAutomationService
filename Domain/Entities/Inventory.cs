
public class InventoryItem
{
    public int Id { get; set; }

    public string SkuCode { get; set; } = null!;

    public int Ebay_Category_Id { get; set; }
    public decimal SellPrice { get; set; }

    public string Status { get; set; } = InventoryStatus.Pending;
    public DateTime CreatedAt { get; set; }

    // Fk to Sku
    public Sku? sku{ get; set;}
}
public static class InventoryStatus
{
    public const string Pending = "Pending";
    public const string OfferCreated = "OfferCreated";
    public const string Failed = "Failed";

}