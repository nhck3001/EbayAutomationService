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
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.sandbox.ebay.com/commerce/taxonomy/v1/get_default_category_tree_id?marketplace_id=EBAY_US");
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
    public async Task<string> getSuggesstedCategory(string defaultCategoryTreeID, string suggesstedPhrase)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://api.sandbox.ebay.com/commerce/taxonomy/v1/category_tree/{defaultCategoryTreeID}/get_category_suggestions?q={suggesstedPhrase}");
        var response = await _api.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"eBay API request failed with status code {response.StatusCode}. Response: {response.Content}", null, response.StatusCode);
        }
        var treeID = JObject.Parse(responseJson)["categorySuggestions"][1]["category"]["categoryId"]?.ToString();
        return treeID ?? throw new Exception("No categoryId returned.");
    }
}
    
        