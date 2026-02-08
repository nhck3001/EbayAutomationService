namespace EbayAutomationService.Domain.Products;

public class NormalizedVariant
{
    public string VariantId { get; set; } = default!;

    public Dictionary<string, string> Attributes { get; set; } = new();
    // Example:
    // Color = Red
    // Size = XL

    public decimal Price { get; set; }

    public decimal Cost { get; set; }

    public int Quantity { get; set; }

    public decimal WeightGrams { get; set; }

    public string? Sku { get; set; }
}
