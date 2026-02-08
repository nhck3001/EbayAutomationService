using System.Text;
using EbayAutomationService.Services;
using Newtonsoft.Json;

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
    public async Task<string> createOffer(string sku)
    {
        var body = new
        {
            sku = sku,
            marketplaceId = "EBAY_US",
            format = "FIXED_PRICE",
        };
        var jsonBody = JsonConvert.SerializeObject(body);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.sandbox.ebay.com/sell/inventory/v1/offer");
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
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
        var offerId = Newtonsoft.Json.Linq.JObject.Parse(responseJson)["Id"]?.ToString();
        return offerId ?? throw new Exception("No offerId returned.");
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



        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, $"https://api.sandbox.ebay.com/sell/inventory/v1/offer/{offerID}");
        var response = await _api.SendAsync(request,jsonBody, true);
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
    public async Task publishOffer(string offerId)
    {

        using var client = new HttpClient();
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"https://api.sandbox.ebay.com/sell/inventory/v1/offer/{offerId}/publish");
        // Set the content type, and content-language header for the content as required in the ebay doc

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