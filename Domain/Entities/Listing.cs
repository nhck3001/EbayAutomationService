
public class Listing
{
    public int Id { get; set; }

    public string Sku { get; set; } = null!;
    public string EbayItemId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Sku SkuEntity { get; set; } = null!;
}
