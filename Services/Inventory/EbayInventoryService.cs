using System.Text;
using EbayAutomationService.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// This class is used to talk to the Inventory API
public class EbayInventoryService
{
    private readonly EbayApiClient _api;

    public EbayInventoryService(EbayApiClient api)
    {
        _api = api;
    }

    // This is used to mass create InventoryItem, which is used to create listings later on
    public async Task CreateInventoryItem(string sku)
    {
        using var client = new HttpClient();

        var body = new
        {
            product = new
            {
                title = $"Test Product {sku}",
                description = "This is a sandbox test product.",
                aspects = new { Brand = new[] { "TestBrand" } }
            },
            condition = "NEW",
            availability = new
            {
                shipToLocationAvailability = new
                {
                    quantity = 10
                }
            }
        };


        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "https://api.ebay.com/sell/inventory/v1/inventory_item/" + sku);
        var json = JsonConvert.SerializeObject(body);
        var response = await _api.SendAsync(request, json, true);

        if (!response.IsSuccessStatusCode)
        {
            // Throws a more informative exception, including the HTTP status code
            throw new HttpRequestException(
                $"eBay API request failed with status code {response.StatusCode}. Response: {json}",
                null,
                response.StatusCode
            );
        }
    }

    // Get the number of Inventory Items that user has
    public async Task<List<string>> getAllSku()
    {
        var skus = new List<string>();
        int offset = 0;
        int limit = 25;
        int total = int.MaxValue;
        // HttpClient() object allows sending Http request to api servers
        // Set up the header value
        while (offset < total)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://api.ebay.com/sell/inventory/v1/inventory_item?offset={offset}&limit={limit}");
            // Get the response in the form of HttpResponseMessage object
            var response = await _api.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Throws a more informative exception, including the HTTP status code
                throw new HttpRequestException(
                    $"eBay API request failed with status code {response.StatusCode}. Response: {json}",
                    null,
                    response.StatusCode
                );
            }
            var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

            total = obj["total"]!.Value<int>();
            // Handle when there's no inventory item
            if (total == 0)
            {
                return skus;
            }
            foreach (var item in obj["inventoryItems"]!)
            {
                skus.Add(item["sku"]!.ToString());
            }

            offset += limit;

        }

        return skus;
    }

    // Used to get the number InventoryItem
    public async Task<string> GetInventoryItemCount()
    {

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.ebay.com/sell/inventory/v1/inventory_item?limit=2&offset=0");
        var response = await _api.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            // Throws a more informative exception, including the HTTP status code
            throw new HttpRequestException(
                $"eBay API request failed with status code {response.StatusCode}. Response: {response.Content}",
                null,
                response.StatusCode
            );
        }
        return Newtonsoft.Json.Linq.JObject.Parse(json).ToString(Newtonsoft.Json.Formatting.Indented);
    }

    /// <summary>
    /// Create an InventoryLocation, identified by merchantLocationKey
    /// </summary>
    /// <param name="merchantLocationKey"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task createInventoryLocation(string merchantLocationKey)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"https://api.ebay.com/sell/inventory/v1/location/{merchantLocationKey}");
        var body = new
        {
            location = new
            {
                address = new
                {
                    city = "S*****e",
                    stateOrProvince = "**",
                    country = "US"
                }
            },
            name = "W********1",
            merchantLocationStatus = "ENABLED",
            locationTypes = new[] { "WAREHOUSE" }
        };
        var json = JsonConvert.SerializeObject(body);
        var response = await _api.SendAsync(request, json, true);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            if (responseJson.Contains("25803"))
            {
                // Log it if you want, then return to "abort" the crash.
                // This swallows the error and lets the program continue.
                Console.WriteLine("Merchant Location Key already exists. Skipping creation.");
                return;
            }
            // Throws a more informative exception, including the HTTP status code
            throw new HttpRequestException(
                $"eBay API request failed with status code {response.StatusCode}. Response: {response.Content}",
                null,
                response.StatusCode
            );
        }
        var treeID = Newtonsoft.Json.Linq.JObject.Parse(responseJson)["categoryID"]?.ToString();
    }


    /// <summary>
    /// get all inventoryLocations
    /// </summary>
    /// <param name="merchantLocationKey"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<string> getInventoryLocations()
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://api.ebay.com/sell/inventory/v1/location?");
        var response = await _api.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            if (responseJson.Contains("25803"))
            {
                // Log it if you want, then return to "abort" the crash.
                // This swallows the error and lets the program continue.
                Console.WriteLine("Merchant Location Key already exists. Skipping creation.");
                return "";
            }
            // Throws a more informative exception, including the HTTP status code
            throw new HttpRequestException(
                $"eBay API request failed with status code {response.StatusCode}. Response: {response.Content}",
                null,
                response.StatusCode
            );
        }
        var obj = JObject.Parse(responseJson);

        var merchantLocationKey = JObject.Parse(responseJson)
                            ["locations"]?
                            .FirstOrDefault()?
                            ["merchantLocationKey"]?
                            .ToString()
                            ?? throw new Exception("merchantLocationKey not found");

        return merchantLocationKey;

    }

    /// <summary>
    /// This method is used to delete inventory items by SKU
    /// </summary>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task deleteInventoryItem(string sku)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, $"https://api.ebay.com/sell/inventory/v1/inventory_item/{sku}");
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
    }

}