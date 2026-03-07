using System.Text;
using EbayAutomationService.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

public class EbayOfferApiClient
{
    private readonly EbayApiClient _api;

    public EbayOfferApiClient(EbayApiClient api)
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
    public async Task<OperationResult> CreateOffer(
        string sku,
        string categoryId,
        decimal price,
        string merchantLocationKey,
        string paymentPolicyId,
        string fulfillmentPolicyId,
        string returnPolicyId,
        CancellationToken stoppingToken
    )
    {
        
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
        HttpResponseMessage response = await _api.SendAsync(request,stoppingToken,  jsonBody, true);
        string responseText = await response.Content.ReadAsStringAsync();
        var responseJson = JObject.Parse(responseText);

        if (response.IsSuccessStatusCode)
        {
            return OperationResult.Success(value: responseJson["offerId"].Value<string>());
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
                return OperationResult.Exists(value: existingOfferId);
            }
        }
        // Create an offer of a sku that doesn't exist
        if (error?["errorId"]?.Value<int>() == 25702 && error["message"].Value<string>().Contains("could not be found"))
        {
            // Offer already exists → extract offerId

                return OperationResult.Invalid("Created an offer based on non-existing sku. Mark as failed");
            
        }
        // For every failed offer, log error, mark as FAILED and move on
        Log.Warning($"Failed: {error?["errorId"]?? "Not existing Error ID"} {responseText}");
        return OperationResult.Invalid();       
    }



    /// <summary>
    /// Update an offer based on offerID. Returns nothing
    /// </summary>
    /// <param name="sku"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task updateOffer(string offerID, string jsonBody, CancellationToken stoppingToken)
    {

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, $"https://api.ebay.com/sell/inventory/v1/offer/{offerID}");
        var response = await _api.SendAsync(request, stoppingToken, jsonBody, true);
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
    public async Task<OperationResult> publishOffer(string offerId, CancellationToken stoppingToken)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"https://api.ebay.com/sell/inventory/v1/offer/{offerId}/publish");
        // Set the content type, and content-language header for the content as required in the ebay doc

        var response = await _api.SendAsync(request,stoppingToken);
        var responseJson = JObject.Parse(await response.Content.ReadAsStringAsync());
        if (response.IsSuccessStatusCode)
        {
            return OperationResult.Success(value: responseJson["listingId"].Value<string>());
        }
        // Error handling
        var error = responseJson["errors"]!.FirstOrDefault();

        if (error?["errorId"].Value<int>() == 25002)
        {
            if (error["message"].Value<string>().Contains("is too long") || // Fields are too long 
                error["message"].Value<string>().Contains("Picture Policy") // Picture not meeting requirements
                )
            {
                return OperationResult.Invalid($"A field is too long. Failed {offerId}");
            }
        }

        // Other errors, log 
        return OperationResult.Invalid($"Publish offer {offerId} fail. {error?["errorId"]}. {error["message"].Value<string>()}");  
}
    /// <summary>
    /// Get all offers for a specified SKU. 
    /// </summary>
    /// <param name="sku"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<JToken> getOffers(string sku, CancellationToken stoppingToken)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://api.ebay.com/sell/inventory/v1/offer?sku={sku}");
        var response = await _api.SendAsync(request, stoppingToken);
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
        var offerId = Newtonsoft.Json.Linq.JObject.Parse(responseJson)["offers"].First;
        return offerId ?? throw new Exception("No offerId returned.");
    }



}