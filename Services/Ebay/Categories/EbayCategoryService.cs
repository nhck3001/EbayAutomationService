using System.Text;
using EbayAutomationService.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class EbayCategoryService
{
    private readonly EbayApiClient _api;

    public EbayCategoryService(EbayApiClient api)
    {
        _api = api;
    }
    /// <summary>
    /// This will get the default category for a marketplace
    /// </summary>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<string> getDefaultCategoryTreeID()
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.ebay.com/commerce/taxonomy/v1/get_default_category_tree_id?marketplace_id=EBAY_US");
        var response = await _api.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            // Throws a more informative exception, including the HTTP status code
            throw new HttpRequestException(
                $"eBay API request failed with status code {response.StatusCode}. Response: {response.Content}",
                null,
                response.StatusCode
            );
        }
        var treeID = Newtonsoft.Json.Linq.JObject.Parse(responseJson)["categoryTreeId"]?.ToString();
        return treeID ?? throw new Exception("No offerId returned.");
    }

    /// <summary> /// This will get suggested category based on categoryTreeID + suggessted phrase 
    /// /// </summary> /// <returns></returns> /// <exception cref="HttpRequestException"></exception> /// <exception cref="Exception"></exception> 
    public async Task<string> GetItemAspectForCategory( string categoryId = "")
    {
        HttpRequestMessage request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.ebay.com/commerce/taxonomy/v1/category_tree/0/get_item_aspects_for_category?category_id={categoryId}"
        );

        var response = await _api.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"eBay API request failed with status code {response.StatusCode}. Response: {responseJson}",
                null,
                response.StatusCode
            );
        }

        JObject json = JObject.Parse(responseJson);

        var aspects = json["aspects"] as JArray;

        if (aspects == null)
            return "{}";

        var schema = new EbayAspectSchema();

        foreach (var aspect in aspects)
        {
            var constraint = aspect["aspectConstraint"];
            if (constraint == null)
                continue;

            var name = aspect["localizedAspectName"]?.ToString();
            var dataType = constraint["aspectDataType"]?.ToString();

            if (string.IsNullOrWhiteSpace(name))
                continue;

            var aspectInfo = new EbayAspectInfo
            {
                Name = name,
                ValueType = dataType ?? "STRING"
            };

            bool isRequired = constraint["aspectRequired"]?.Value<bool>() == true;
            string usage = constraint["aspectUsage"]?.ToString() ?? "";

            if (isRequired)
            {
                schema.RequiredAspects.Add(aspectInfo);
            }
            else if (usage == "RECOMMENDED")
            {
                schema.RecommendedAspects.Add(aspectInfo);
            }
        }

        // return JSON string
        return Newtonsoft.Json.JsonConvert.SerializeObject(schema, Newtonsoft.Json.Formatting.Indented);
    }


    public async Task<string> getCompleteCategoryTree(string categoryTreeId = "0")
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://api.ebay.com/commerce/taxonomy/v1/category_tree/{categoryTreeId}");
        var response = await _api.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            // Throws a more informative exception, including the HTTP status code
            throw new HttpRequestException(
                $"eBay API request failed with status code {response.StatusCode}. Response: {response.Content}",
                null,
                response.StatusCode
            );
        }
        return responseJson;
    }

    public async Task<string> getSuggesstedCategory(string suggesstedPhrase)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://api.ebay.com/commerce/taxonomy/v1/category_tree/0/get_category_suggestions?q={suggesstedPhrase}");

        var response = await _api.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"eBay API request failed with status code {response.StatusCode}. Response: {response.Content}", null, response.StatusCode);
        }

        var treeID = JObject.Parse(responseJson)["categorySuggestions"][1]["category"]["categoryId"]?.ToString();
        return treeID ?? throw new Exception("No categoryId returned.");
    }

    public async Task<JObject> GetCategorySubtree( string categoryId)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.ebay.com/commerce/taxonomy/v1/category_tree/0/get_category_subtree?category_id={categoryId}"
        );

        var response = await _api.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception(json);

        return JObject.Parse(json);
    }

}
    
        