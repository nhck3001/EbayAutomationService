using Newtonsoft.Json.Linq;

public class EbayItemResearchData
{
    public string ItemId { get; set; }
    public string Title { get; set; }
    public decimal? Price { get; set; }
    public string Currency { get; set; }
    public string Image { get; set; }
    public List<string> AdditionalImages { get; set; } = new();
    public Dictionary<string, List<string>> ItemSpecifics { get; set; } = new();
    public string CategoryId { get; set; }

    public static EbayItemResearchData ExtractResearchData(JObject item)
{
    var data = new EbayItemResearchData
    {
        ItemId = item["itemId"]?.ToString(),
        Title = item["title"]?.ToString(),
        CategoryId = item["categoryId"]?.ToString(),
        Image = item["image"]?["imageUrl"]?.ToString(),
        Currency = item["price"]?["currency"]?.ToString()
    };

    // Price
    if (decimal.TryParse(item["price"]?["value"]?.ToString(), out var price))
    {
        data.Price = price;
    }

    // Additional images
    var additionalImages = item["additionalImages"];
    if (additionalImages != null)
    {
        foreach (var img in additionalImages)
        {
            var url = img["imageUrl"]?.ToString();
            if (!string.IsNullOrEmpty(url))
                data.AdditionalImages.Add(url);
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
