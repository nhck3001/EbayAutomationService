public class DeepSeekInput
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public List<string> Images { get; set; } = new();
    public string Brand { get; set; } = "Unbranded";
    public decimal SellPrice { get; set; }

    public string? ItemLength { get; set; }
    public string? ItemWidth { get; set; }
    public string? ItemHeight { get; set; }

    public int? WeightLb { get; set; }
    public int? WeightOz { get; set; }
    public string? Material { get; set; }

    public string? Specification { get; set; }
    public string? AttributeKeyword { get; set; }

    public static DeepSeekInput? Build(CjProductDetail product, CjVariant variant)
    {
        string name;
        // ---------------------------- MANDATORY ATTRIBUTES ----------------------------
        // Check if product or its variant has english name. Prefer variant english name
        if (!string.IsNullOrWhiteSpace(variant.VariantNameEn))
        {
            name = variant.VariantNameEn;
        }
        else if (!string.IsNullOrWhiteSpace(product.ProductNameEn))
        {
            name = product.ProductNameEn;
        }
        else
        {
            return null;
        }

        // Look for images of a product. Can't create a listing without an image so return null
        // Look for images of a product. Can't create a listing without an image so return null
        var images = new List<string>();
        
        // Fixed: Removed .Distinct() which doesn't work on strings
        if (!string.IsNullOrWhiteSpace(variant.VariantImage))
        {
            images.Add(variant.VariantImage);
        }
        
        if (!string.IsNullOrWhiteSpace(product.ProductImage))
        {
            try
            {
                // If ProductImage is a JSON array string, parse it
                if (product.ProductImage.TrimStart().StartsWith("["))
                {
                    var parsed = Newtonsoft.Json.Linq.JArray.Parse(product.ProductImage);
                    foreach (var img in parsed.Take(6))
                    {
                        images.Add(img.ToString());
                    }
                }
                else
                {
                    images.Add(product.ProductImage);
                }
            }
            catch
            {
                // If parsing fails, add as single image
                images.Add(product.ProductImage);
            }
        }

        // Remove duplicates
        images = images.Distinct().ToList();

        if (images.Count == 0)
        {
            return null;
        }

        // Look for description of a product. Can't create a listing without desription so return null
        string description = "";
        if (!string.IsNullOrWhiteSpace(product.Description))
        {
            description = product.Description;
        }
        else
        {
            return null;
        }
        // Check for valid sellPrice. If sell doesn't exist, can't list => retur null
        decimal sellPrice = 0;
        if (variant.VariantSellPrice.HasValue && variant.VariantSellPrice > 0)
        {
            sellPrice = (decimal)variant.VariantSellPrice;
        }
        else if (!string.IsNullOrWhiteSpace(product.SellPrice) && decimal.TryParse(product.SellPrice, out var parsedPrice) && parsedPrice > 0)
        {
            sellPrice = parsedPrice;
        }
        else
        {
            return null;
        }
        // ---------------------------- END OF MANDATORY ATTRIBUTES ----------------------------

        // ---------------------------- LENGTH/WIDTH/HEIGHT/WEIGHT/VOLUME ATTRIBUTES ----------------------------
        // Check if length exist. Length attribute only exist in variant
        string lengthIn = null;
        if (variant.VariantLength.HasValue && variant.VariantLength > 0)
        {
            lengthIn = Math.Ceiling((double)variant.VariantLength / 25.4) + " in";
        }

        // Check if width exist. Width attribute only exist in variant
        string widthIn = null;
        if (variant.VariantWidth.HasValue && variant.VariantWidth > 0)
        {
            widthIn = Math.Ceiling((double)variant.VariantWidth / 25.4) + " in";
        }

        // Check if Height exist. Height attribute only exist in variant
        string heightIn = null;
        if (variant.VariantHeight.HasValue && variant.VariantHeight > 0)
        {
            heightIn = Math.Ceiling((double)variant.VariantHeight / 25.4) + " in";
        }

        // If there is weight info, then convert it from g->lbs
        int? weightLb = null;
        int? weightOz = null;
        if (variant.VariantWeight.HasValue && variant.VariantWeight > 0)
        {
                // Convert grams to total pounds
                double totalLb = (double)variant.VariantWeight.Value / 453.59237;
                
                // Get whole pounds (floor) and remaining ounces
                int wholePounds = (int)Math.Floor(totalLb);
                double remainingOunces = (totalLb - wholePounds) * 16;
                
                // Round remaining ounces and cap at 15
                int roundedOunces = (int)Math.Round(remainingOunces);
                if (roundedOunces > 15) roundedOunces = 15;
                
                weightLb = wholePounds;
                weightOz = roundedOunces;
        }
        // Fall back to product weight if variant weight don't exist
        else if (!String.IsNullOrEmpty(product.ProductWeight) && double.TryParse(product.ProductWeight, out var ProductWeight) && ProductWeight > 0)
        {
            // Convert grams to total pounds
            double totalLb = (double)ProductWeight / 453.59237;
            
            // Get whole pounds (floor) and remaining ounces
            int wholePounds = (int)Math.Floor(totalLb);
            double remainingOunces = (totalLb - wholePounds) * 16;
            
            // Round remaining ounces and cap at 15
            int roundedOunces = (int)Math.Round(remainingOunces);
            if (roundedOunces > 15) roundedOunces = 15;
            
            weightLb = wholePounds;
            weightOz = roundedOunces;
        }

        string material = null;
        if (!String.IsNullOrEmpty(product.MaterialNameEn))
        {
            material = product.MaterialNameEn;
        }

        // ---------------------------- END OF LENGTH/WIDTH/HEIGHT/WEIGHT/ ATTRIBUTES ----------------------------

        // ---------------------------- OTHER ATTRIBUTES ----------------------------

        //string standard = null;         // Sepcification description
        //if (!String.IsNullOrEmpty(variant.VariantStandard))
        //{
        //    standard =variant.VariantStandard;
        //}

        string key = null; // Attribute keyword
        if (!String.IsNullOrEmpty(product.ProductKeyEn))
        {
            key = product.ProductKeyEn;
        }
        var result = new DeepSeekInput
        {
            Name = name,
            Description = description,
            Images = images,
            SellPrice = sellPrice,
        };

        // Only add dimensions if they exist
        if (lengthIn != null) result.ItemLength = lengthIn;
        if (widthIn != null) result.ItemWidth = widthIn;
        if (heightIn != null) result.ItemHeight= heightIn;

        // Only add weight if it exists
        if (weightLb.HasValue) result.WeightLb= weightLb.Value;
        if (weightOz.HasValue) result.WeightOz = weightOz.Value;

        // Only add other fields if they exist
        if (material != null) result.Material = material;
        //if (standard != null) result.Specification = standard;
        if (key != null) result.AttributeKeyword = key;

        return result;
                
    }
}
