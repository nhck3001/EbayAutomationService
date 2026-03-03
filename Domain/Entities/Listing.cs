
public class Listing
{
    public int Id { get; set; }

    public string listingId { get; set; } = null!;
    public int OfferId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public OfferItem Offer { get; set; } = null!;
}
