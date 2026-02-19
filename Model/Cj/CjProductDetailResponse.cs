// Model for ResponseFrom Cj that includes a product list
public class CjProductDetailResponse
{
    public int Code { get; set; }
    public bool Result { get; set; }
    public string Message { get; set; }
    // Inside here is additional info + PRODUCT DATA
    public CjProductDetail Data { get; set; }

    public string RequestId { get; set; }
}
// Model for each individual product
public class CjProductDetail
{
    // IDs are huge → string
    public string? Pid { get; set; } // Will be mapped to SKU

    // NOTE: productName is actually a JSON string that contains a JSON array
    public string? ProductName { get; set; }

    public string? ProductNameEn { get; set; } // Will be mapped to Title
    public string? ProductSku { get; set; }

    public string? ProductImage { get; set; }

    // NOT decimal (ranges like "2.09 -- 7.46")
    public string? SellPrice { get; set; } // WIll be mapped to Price

    // NOT decimal (ranges like "10.00-50.00")
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
    public List<CjVariant>? Variants { get; set; }
    public string? Description { get; set; }

    public string? MaterialNameEn { get; set; }

    public string? PackWeight { get; set; }

    public string? ProductKeyEn { get; set; }
}

public class CjVariant
{
    public string? Vid { get; set; }
    public string? Pid { get; set; }
    public string? VariantNameEn { get; set; }
    public string? VariantProperty { get; set; }
    public string? VariantSku { get; set; }
    public string? VariantImage { get; set; }
    public string? VariantKey { get; set; } // e.g. "White"

    public decimal? VariantSellPrice { get; set; }
    public double? VariantWeight { get; set; }

    public int? VariantLength { get; set; }
    public int? VariantWidth { get; set; }
    public int? VariantHeight { get; set; }
    public int? VariantVolume { get; set; } // mm3

    public string? VariantStandard { get; set; }
    
}



 