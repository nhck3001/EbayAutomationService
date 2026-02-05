// Model for each individual product
public class CjProductDetail
{
    // IDs are huge → string
    public string? Pid { get; set; }

    // NOTE: productName is actually a JSON string that contains a JSON array
    public string? ProductName { get; set; }

    public string? ProductNameEn { get; set; }
    public string? ProductSku { get; set; }

    public string? ProductImage { get; set; }

    // ❌ NOT decimal (ranges like "2.09 -- 7.46")
    public string? SellPrice { get; set; }

    // ❌ NOT decimal (ranges like "10.00-50.00")
    public string? ProductWeight { get; set; }

    public string? ProductType { get; set; }
    public string? ProductUnit { get; set; }

    public string? CategoryName { get; set; }

    public int? ListingCount { get; set; }

    public string? Remark { get; set; }

    public string? AddMarkStatus { get; set; }
    public bool? IsFreeShipping { get; set; }

    // Unix timestamp (milliseconds)
    public long? CreateTime { get; set; }

    public object? IsVideo { get; set; }
    public int? SaleStatus { get; set; }
    public int? ListedNum { get; set; }

    public string? SupplierName { get; set; }
    public string? SupplierId { get; set; }

    public string? CategoryId { get; set; }
    public string? SourceFrom { get; set; }

    public List<string>? ShippingCountryCodes { get; set; }

    public string? ThreeCategoryName { get; set; }
    public string? TwoCategoryId { get; set; }
    public string? TwoCategoryName { get; set; }
    public string? OneCategoryId { get; set; }
    public string? OneCategoryName { get; set; }

    public int? CustomizationVersion { get; set; }
    public bool? IsTestProduct { get; set; }
}
