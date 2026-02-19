using Newtonsoft.Json.Linq;

public class EbayItemResearchData
{
    public string ItemId { get; set; } // This is used as SKU when creating a product
    public string Title { get; set; }
    public string Description { get; set; }

    public decimal Price { get; set; }
    public string Currency { get; set; }
    public List<string> Images { get; set; }
    public Dictionary<string, List<string>> ItemSpecifics { get; set; } = new();
    public string CategoryId { get; set; }

    public static EbayItemResearchData ExtractResearchData(JObject item)
{
    var data = new EbayItemResearchData
    {
        ItemId = item["itemId"]?.ToString(),
        Title = item["title"]?.ToString(),
        CategoryId = item["categoryId"]?.ToString(),
        Currency = item["price"]?["currency"]?.ToString()
    };

    // Price
    if (decimal.TryParse(item["price"]?["value"]?.ToString(), out var price))
    {
        data.Price = price;
    }

    // Images
    var primaryImage = item["image"]?["imageUrl"]?.ToString();
    if (!string.IsNullOrEmpty(primaryImage))
    {
        data.Images.Add(primaryImage);
    }
    // Additional images
    var additionalImages = item["additionalImages"];
    if (additionalImages != null)
    {
        foreach (var img in additionalImages)
        {
            var url = img["imageUrl"]?.ToString();
            if (!string.IsNullOrEmpty(url) && !data.Images.Contains(url))
            {
                data.Images.Add(url);
            }
        }
    }

    // Item specifics
    var specifics = item["itemSpecifics"];
    if (specifics != null)
    {
        foreach (var spec in specifics)
        {
            var name = spec["name"]?.ToString();
            var values = spec["values"]?.ToObject<List<string>>();

            if (!string.IsNullOrEmpty(name) && values != null)
            {
                data.ItemSpecifics[name] = values;
            }
        }
    }

    return data;
}

}
