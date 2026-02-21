using System.Text;
using EbayAutomationService.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

public class EbayOfferService
{
    private readonly EbayApiClient _api;

    public EbayOfferService(EbayApiClient api)
    {
        _api = api;
    }
    /// <summary>
    /// Create an offer based on SKU. It will return an offerID
    /// </summary>
    /// <param name="sku"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<(string Status, string? OfferId)> CreateOffer(
        string sku,
        string categoryId,
        decimal price,
        string merchantLocationKey,
        string paymentPolicyId,
        string fulfillmentPolicyId,
        string returnPolicyId
    )
    {
        Log.Information($"Trying to create an offer object for sku {sku}");
        
        var body = new
        {
            sku = sku,
            marketplaceId = "EBAY_US",
            format = "FIXED_PRICE",
            availableQuantity = 1,
            categoryId = categoryId,
            merchantLocationKey = merchantLocationKey,
            listingPolicies = new
            {
                paymentPolicyId = paymentPolicyId,
                fulfillmentPolicyId = fulfillmentPolicyId,
                returnPolicyId = returnPolicyId
            },
            pricingSummary = new
            {
                price = new
                {
                    value = price,
                    currency = "USD"
                }
            }
        };

        var jsonBody = JsonConvert.SerializeObject(body);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/sell/inventory/v1/offer");
        HttpResponseMessage response = await _api.SendAsync(request, jsonBody, true);
        string responseText = await response.Content.ReadAsStringAsync();
        var responseJson = JObject.Parse(responseText);

        if (response.IsSuccessStatusCode)
        {
            Log.Information($"Create offer for {sku} successfully");
            return (SkuStatuses.OfferCreated, responseJson["offerId"].Value<string>());
        }

        // handling failures
        // Check for specific error codes
        var error = responseJson["errors"]?.FirstOrDefault();
        // Handling cases where offer already exist
        if (error?["errorId"]?.Value<int>() == 25002 && error["message"].Value<string>().Contains("Offer entity already exists"))
        {
            // Offer already exists → extract offerId
            var existingOfferId = error["parameters"]?.FirstOrDefault(p => p["name"]?.ToString() == "offerId")?["value"]?.ToString();
            if (!string.IsNullOrEmpty(existingOfferId))
            {
                Log.Information($"Offer already created. Using existing offerId {existingOfferId}");
            return (SkuStatuses.OfferCreated, existingOfferId);
            }
        }
        // For every failed offer, log error, mark as FAILED and move on
        Log.Warning($"Failed: {error?["errorId"]?? "Not existing Error ID"} {responseText}");
        return (SkuStatuses.Failed,"");        
    }



    /// <summary>
    /// Update an offer based on offerID. Returns nothing
    /// </summary>
    /// <param name="sku"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task updateOffer(string offerID, string fulfillmentPolicyId, string paymentPolicyId, string returnPolicyId, string merchantLocationKey)
    {
        var body = new
        {
            availableQuantity = 60,
            categoryId = "30120",
            listingDescription = "We need to put something here even though it's not necessary",
            listingPolicies = new
            {
                fulfillmentPolicyId = fulfillmentPolicyId,
                paymentPolicyId = paymentPolicyId,
                returnPolicyId = returnPolicyId
            },
            pricingSummary = new
            {
                price = new
                {
                    currency = "USD",
                    value = "260.00"
                }
            },
            merchantLocationKey = merchantLocationKey,
            listingDuration = "DAYS_30"
        };
        var jsonBody = JsonConvert.SerializeObject(body);



        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, $"https://api.ebay.com/sell/inventory/v1/offer/{offerID}");
        var response = await _api.SendAsync(request, jsonBody, true);
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

    /// <summary>
    /// publish an offer based on offerID. Returns nothing
    /// </summary>
    /// <param name="sku"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<string> publishOffer(string offerId, string sku)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"https://api.ebay.com/sell/inventory/v1/offer/{offerId}/publish");
        // Set the content type, and content-language header for the content as required in the ebay doc

        var response = await _api.SendAsync(request);
        var responseJson = JObject.Parse(await response.Content.ReadAsStringAsync());
        if (response.IsSuccessStatusCode)
        {
            Log.Information($"Publish offer {sku} successfully");
            return SkuStatuses.Published;
        }
        // Error handling
        var error = responseJson["errors"]!.FirstOrDefault();

        if (error?["errorId"].Value<int>() == 25002)
        {
            if (error["message"].Value<string>().Contains("is too long"))
            {
                Log.Information($"A field is too long. simply ignore for now {sku}");
                return SkuStatuses.Failed;
            }
            else
            {
                
            }
            // Business logic. Log and then Ignore for now
            Log.Warning($"Publish offer {sku} fail. {response.StatusCode}. {response.Content}");
            throw new HttpRequestException($"Publish offer {sku} failed. Please handle error");
        }

        // Other errors, log and throw
        Log.Warning($"Publish offer {sku} fail. {response.Content}");
        return SkuStatuses.OfferCreated;
        throw new HttpRequestException($"Publish offer {sku} failed. Please handle error");
    }
    /// <summary>
    /// Get all offers for a specified SKU. 
    /// </summary>
    /// <param name="sku"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<string> getOffers(string sku)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://api.ebay.com/sell/inventory/v1/offer?sku={sku}");
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
        var offerId = Newtonsoft.Json.Linq.JObject.Parse(responseJson)["offers"]?.ToString();
        return offerId ?? throw new Exception("No offerId returned.");
    }



}