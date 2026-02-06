namespace EbayAutomationService.Domain.Products;

public class NormalizedProduct
{
    public string SupplierProductId { get; set; } = default!;
    public string Supplier { get; set; } = default!; // "CJ"

    public string Title { get; set; } = default!;
    public string DescriptionHtml { get; set; } = default!;

    // eBay mapping
    public string EbayCategoryId { get; set; } = default!;

    // images
    public string MainImage { get; set; } = default!;
    public List<string> GalleryImages { get; set; } = new();

    // pricing & inventory
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    // shipping
    public bool IsUSShippable { get; set; }

    // optional but useful
    public string Brand { get; set; } = "Unbranded";

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
