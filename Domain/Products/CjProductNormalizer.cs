using EbayAutomationService.Domain.Products;

// A class used to parse raw data from Cj to normalized Ebay listing 
public static class CjProductNormalizer
{

    // Normalizing object return from Cj to Normalied Produ
    public static List<NormalizedProduct> NormalizeProducts(List<CjProductDetail> cjProducts)
    {
        var results = new List<NormalizedProduct>();

        foreach (var cj in cjProducts)
        {
            // Filter: missing image
            if (string.IsNullOrWhiteSpace(cj.ProductImage))
                continue;

            // Filter: price > 100
            var price = ExtractMaxPrice(cj.SellPrice);
            if (price > 100m || price <= 0)
                continue;

            // Filter: US shipping
            if (cj.ShippingCountryCodes == null || !cj.ShippingCountryCodes.Contains("US"))
                continue;

            // Convert → NormalizedProduct
            var normalized = MapToNormalized(cj, price);

            // Validate required listing fields
            if (!IsValid(normalized))
                continue;

            results.Add(normalized);
        }

        return results;
    }
    private static NormalizedProduct MapToNormalized(CjProductDetail cj, decimal price)
    {
        return new NormalizedProduct
        {
            Supplier = "CJ",
            SupplierProductId = cj.Pid!,

            Title = string.IsNullOrWhiteSpace(cj.ProductNameEn)
                ? "Home Storage Organizer"
                : cj.ProductNameEn,

            DescriptionHtml = cj.Remark ?? "<p>No description provided.</p>",

            MainImage = cj.ProductImage!,
            GalleryImages = new List<string> { cj.ProductImage! },

            Price = Math.Round(price * 2.2m, 2), // profit margin
            Quantity = 5,

            EbayCategoryId = "329079", // placeholder (Home Storage)
            IsUSShippable = true,
            Brand = "Unbranded"
        };
    }
    private static decimal ExtractMaxPrice(string? range)
    {
        if (string.IsNullOrWhiteSpace(range))
            return 0;

        range = range.Replace(" ", "");
        var parts = range.Split(new[] { "--", "-" }, StringSplitOptions.RemoveEmptyEntries);

        decimal max = 0;

        foreach (var p in parts)
        {
            if (decimal.TryParse(p, out var value) && value > max)
                max = value;
        }

        return max;
    }
    private static bool IsValid(NormalizedProduct p)
    {
        if (string.IsNullOrWhiteSpace(p.SupplierProductId)) return false;
        if (string.IsNullOrWhiteSpace(p.Title)) return false;
        if (string.IsNullOrWhiteSpace(p.MainImage)) return false;
        if (p.GalleryImages == null || p.GalleryImages.Count == 0) return false;
        if (p.Price <= 0) return false;
        if (string.IsNullOrWhiteSpace(p.DescriptionHtml)) return false;
        if (p.Quantity <= 0) return false;
        if (!p.IsUSShippable) return false;
        if (string.IsNullOrWhiteSpace(p.EbayCategoryId)) return false;

        return true;
    }
}

