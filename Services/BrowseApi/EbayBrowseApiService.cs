using System.Text;
using EbayAutomationService.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// This class is used to talk to the Inventory API
public class EbayBrowseApiService
{
    private readonly EbayApiClient _api;

    public EbayBrowseApiService(EbayApiClient api)
    {
        _api = api;
    }
    /// <summary>
    /// Fetch public listings from a specific eBay seller (no keyword filter)
    /// </summary>
    /// <param name="sellerUsername">The eBay seller username</param>
    /// <param name="limit">Max items per request (<= 50)</param>
    /// <param name="offset">Pagination offset</param>
    /// <returns>List of listing summaries</returns>
    public async Task<List<string>> GetItemIdsBySeller(
        string sellerUsername,
        int limit = 50,
        int offset = 0)
    {
        var itemIds = new List<string>();

        var filterValue = $"sellers:{{{sellerUsername}}}";
        var encodedFilter = Uri.EscapeDataString(filterValue);

        var url =
            $"https://api.ebay.com/buy/browse/v1/item_summary/search" +
            $"?q=%20" +
            $"&limit={limit}" +
            $"&offset={offset}" +
            $"&filter={encodedFilter}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        var response = await _api.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"eBay Browse API failed with status code {response.StatusCode}. Response: {responseJson}",
                null,
                response.StatusCode
            );
        }

        var obj = JObject.Parse(responseJson);

        var items = obj["itemSummaries"];
        if (items == null)
            return itemIds;

        foreach (var item in items)
        {
            var itemId = item["itemId"]?.ToString();
            if (!string.IsNullOrEmpty(itemId))
            {
                itemIds.Add(itemId);
            }
        }

        return itemIds;
    }

    // Get info of an item based on itemId
    public async Task<JObject> GetItemByItemId(string itemId, string marketplaceId = "EBAY_US")
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentException("itemId is required");

        var url = $"https://api.ebay.com/buy/browse/v1/item/{itemId}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Required for Browse API
        request.Headers.Add("X-EBAY-C-MARKETPLACE-ID", marketplaceId);

        var response = await _api.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Browse Item API failed with status code {response.StatusCode}. Response: {responseJson}",
                null,
                response.StatusCode
            );
        }

        var itemJson = JObject.Parse(responseJson);
        return itemJson;
    }




}