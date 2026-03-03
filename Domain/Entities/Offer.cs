
public class OfferItem
{
    public int Id { get; set; }
    public string OfferId{ get; set; }
    public int InventoryId { get; set; } // FK to Inventory table
    public int Quantity { get; set; } 
    public int Ebay_Category_Id { get; set; }

    public string Status { get; set; } = OfferStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public decimal SellPrice { get; set; }
    // Navigation
    public InventoryItem? Inventory{ get; set;}
}
public static class OfferStatus
{
    public const string Pending = "Pending";
    public const string ListingCreated = "ListingCreated";
    public const string Failed = "Failed";

}