using System.Text;
using EbayAutomationService.Services;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

// This class is used to talk to the Inventory API
public class EbayInventoryApiClient
{
    private readonly EbayApiClient _api;

    public EbayInventoryApiClient(EbayApiClient api)
    {
        _api = api;
    }

    // This is used to create/update an InventoryItem, which is used to create listings later on
    // This represent a product, not the listing itself
    // Will return an OperationResult object. This object will decide what the pipeline will do next
    public async Task<OperationResult> CreateOrUpdateInventoryItem(
        string sku,
        string title,
        string description,
        List<string> images,
        Dictionary<string, List<string>> itemSpecifics,
        int quantity
    )
    {
        var body = new
        {
            product = new
            {
                title = title,
                description = description,
                aspects = itemSpecifics,
                imageUrls = images
            },
            condition = "NEW",
            availability = new
            {
                shipToLocationAvailability = new
                {
                    quantity = quantity
                }
            }
        };

        var jsonBody = JsonConvert.SerializeObject(body);

        var url = $"https://api.ebay.com/sell/inventory/v1/inventory_item/{sku}";
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        var response = await _api.SendAsync(request, jsonBody, true);
        // Success
        if (response.IsSuccessStatusCode)
        {
            return OperationResult.Success();
        }
        // handling errors
        var responseJson = JObject.Parse(await response.Content.ReadAsStringAsync());
        var error = responseJson["errors"]!.FirstOrDefault();

        // Mostlikely a listing error
        if (error?["errorId"].Value<int>() == 25718)
        {
            if (error["message"].Value<string>().Contains("Invalid value for title"))
            {
                Log.Warning($"Invalid title value. {error["message"]}. Mark as processed and move on");
                return OperationResult.Invalid(message: $"{error["message"]}. Mark as processed and move on)");
            }
        }
        // Most likely a listing error
        if (error?["errorId"].Value<int>() == 25002)
        {
            // "Features" field is too long
            if (error["message"].Value<string>().Contains("is too long"))
            {
                // Business logic. Log and then Ignore for now
                Log.Warning($"Invalid title value. {error["message"]}. Mark as processed and move on");
                return OperationResult.Invalid(message: $"{error["message"]}. Mark as processed and move on");
            }
        }

        // For all other exceptions log and continue. Mark as failed
        Log.Warning($"Create inventory for {sku} fail. {error?["errorId"]}: {error["message"]}.");
        return OperationResult.Invalid(message: $"{error["message"]}");
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
    /// <returns> an OperationResult object deciding what the next steps would be</returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task CreateInventoryLocationIfNotExists(string merchantLocationKey)
    {
        var url = $"https://api.ebay.com/sell/inventory/v1/location/{merchantLocationKey}";

        var body = new
        {
            name = "Main US Shipping Location",
            merchantLocationStatus = "ENABLED",
            locationTypes = new[] { "WAREHOUSE" },
            location = new
            {
                address = new
                {
                    addressLine1 = "123 Main St",
                    city = "Dallas",
                    stateOrProvince = "TX",
                    postalCode = "75201",
                    country = "US"
                }
            }
        };

        var json = JsonConvert.SerializeObject(body);

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await _api.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        // Location already exists → safe to ignore
        if (!response.IsSuccessStatusCode)
        {
            if (
                response.StatusCode == System.Net.HttpStatusCode.Conflict ||
                responseJson.Contains("25803") // location key already exists
            )
            {
                Console.WriteLine($"Merchant location '{merchantLocationKey}' already exists. Skipping creation.");
                return;
            }

            throw new HttpRequestException(
                $"CreateInventoryLocation failed ({response.StatusCode}): {responseJson}"
            );
        }

        Console.WriteLine($"Merchant location '{merchantLocationKey}' created or already enabled.");
    }



    /// <summary>
    /// get all inventoryLocations
    /// </summary>
    /// <param name="merchantLocationKey"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<string> getMerchantLocationKey()
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