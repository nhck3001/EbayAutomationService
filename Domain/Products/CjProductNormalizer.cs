using System.Security.Cryptography;
using EbayAutomationService.Domain.Products;

// A class used to parse raw data from Cj to normalized Ebay listing 
public static class CjProductNormalizer
{

    // Normalizing object return from Cj to Normalied Produ
    public static (List<NormalizedProduct> Success, List<NormalizationReport> Failures)       
     NormalizeProducts(List<CjProductDetail> cjProducts)
    {
        var success = new List<NormalizedProduct>();
        var failures = new List<NormalizationReport>();

        foreach (var cj in cjProducts)
        {
            var report = new NormalizationReport
            {
                SupplierProductId = cj.Pid ?? ""
            };

            //Supplier-level checks (CJ problems)
            var price = ExtractMaxPrice(cj.SellPrice);
            if (string.IsNullOrWhiteSpace(cj.ProductNameEn))
            {
                report.Reasons.Add(NormalizationFailureReason.MissingTitle);
                continue;
            }
            else if (string.IsNullOrWhiteSpace(cj.ProductImage))
            {
                report.Reasons.Add(NormalizationFailureReason.MissingImage);
                continue;
            }
            else if (price <= 0 || price > 100)
            {
                report.Reasons.Add(NormalizationFailureReason.InvalidPrice);
                continue;
            }

            // Mapping 
            var normalized = MapToNormalized(cj, price);

            // Reuse IsValid
            var validation = Validate(normalized);

            if (!validation.IsValid)
            {
                report.Reasons.AddRange(validation.Reasons);
                failures.Add(report);
                continue;
            }

            success.Add(normalized);
        }

        return (success, failures);
    }


    private static NormalizedProduct MapToNormalized(CjProductDetail cj, decimal price)
    {
        return new NormalizedProduct
        {
            Supplier = "CJ",
            SupplierProductId = cj.Pid!,

            Title = cj.ProductNameEn!,

            DescriptionHtml = cj.Remark ?? "",

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
    public static ValidationResult Validate(NormalizedProduct product)
    {
        var result = new ValidationResult();


        if (product.MainImage == null || product.GalleryImages.Count == 0)
            result.Reasons.Add(NormalizationFailureReason.MissingImage);

        if (product.Price <=0 )
            result.Reasons.Add(NormalizationFailureReason.MissingPrice);

        if (product.Quantity <=0)
            result.Reasons.Add(NormalizationFailureReason.MissingQuantity);

        return result;
    }

}

