using Newtonsoft.Json.Linq;

public static class DeepSeekInputBuilder
{
    public static JObject Build(CjProductDetail product, CjVariant variant)
    {
        string name;
        double? sellPrice = 0;
        // Check if product or its variant has english name. Prefer variant english name
        if (!string.IsNullOrWhiteSpace(variant.VariantNameEn))
        {
            name = variant.VariantNameEn;
        }
        else if (!string.IsNullOrWhiteSpace(product.ProductName))
        {
            name = product.ProductName;
        }
        else
        {
            return null;
        }
        // Check if variant length, width, height exists
        if (!variant.VariantLength.HasValue || !variant.VariantWidth.HasValue || !variant.VariantHeight.HasValue || variant.VariantLength <= 0 || variant.VariantWidth <= 0 || variant.VariantHeight <= 0 )
        {
            return null;
        }
        // Check for valid sellPrice
        if (!variant.VariantSellPrice.HasValue && variant.VariantSellPrice > 0)
        {
            sellPrice = (double)variant.VariantSellPrice;
        }
        else if (!string.IsNullOrWhiteSpace(product.SellPrice) && double.TryParse(product.SellPrice, out var parsedPrice) && parsedPrice > 0)
        {
            sellPrice = parsedPrice;
        }
        else
        {
            return null;
        }

        // Convert dimensions from mm-> in
        string lengthIn = Math.Round((double)variant.VariantLength! / 25.4, 2) + " in";
        string widthIn = Math.Round((double)variant.VariantWidth! / 25.4, 2) + " in";
        string heightIn = Math.Round((double)variant.VariantHeight! / 25.4, 2) + " in";
        // If there is weight info, then convert it from g->lbs
        double? weightG = 0;
        double? weightLb = null;
        if (variant.VariantWeight.HasValue &&variant.VariantWeight > 0 )
        {
            weightG = variant.VariantWeight;
            weightLb = weightG.Value / 453.59237;

        }

        // Look for images of a product. Can't create a listing without an image so return null
        JArray images = new JArray();
        if (string.IsNullOrWhiteSpace(variant.VariantImage) && string.IsNullOrWhiteSpace(product.ProductImage))
        {
            return null;
        }
        if (!string.IsNullOrWhiteSpace(variant.VariantImage))
        {
            images.Add(variant.VariantImage);
        }
        if (!string.IsNullOrWhiteSpace(product.ProductImage))
        {
            try
            {
                var parsed = JArray.Parse(product.ProductImage);
                foreach (var img in parsed.Take(6))
                    images.Add(img);
            }
            catch { }
        }
        
        var recommended = new JObject();

        if (weightLb.HasValue)
        recommended["weight_lb"] = weightLb.Value;
        
           return new JObject
        {
            ["required"] = new JObject
            {
                ["variantSku"] = variant.VariantSku,
                ["dimensions_in"] = new JObject
                {
                    ["length"] = lengthIn,
                    ["width"] = widthIn,
                    ["height"] = heightIn
                },
                ["images"] = images,
                ["sellPrice"] = sellPrice
            },

            ["recommended"] = recommended,


            ["ai_input"] = new JObject
            {
                ["variantName"] = name,
                ["description"] = product.Description,
                ["variantProperty"] = variant.VariantProperty,
                ["variantKeywords"] = variant.VariantKey,
                ["variantStandard"] = variant.VariantStandard,
                ["productKeywords"] = product.ProductKeyEn
            }
        };
    }
}
