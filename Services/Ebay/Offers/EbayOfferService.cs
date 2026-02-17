using System.Text;
using EbayAutomationService.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    public async Task<string> CreateOffer(
        string sku,
        string categoryId,
        decimal price,
        string merchantLocationKey,
        string paymentPolicyId,
        string fulfillmentPolicyId,
        string returnPolicyId
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
        Console.WriteLine("CreateOffer payload:\n" + jsonBody);

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/sell/inventory/v1/offer");
        HttpResponseMessage response;
        string responseText;
        try
        {
            response = await _api.SendAsync(request, jsonBody, true);
            responseText = await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException)
        {
            throw;
        }

        // Created successfully
        if (response.IsSuccessStatusCode)
        {
            var json = JObject.Parse(responseText);
            var offerId = json["offerId"]!.ToString();
            return offerId;
        }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"eBay API error: {response.StatusCode}");
                Console.WriteLine($"Response: {errorContent}");
                throw new HttpRequestException($"eBay API request failed with status code {response.StatusCode}. Response: {errorContent}");
            }
        else
        {
            Console.WriteLine("CreateOffer failed:");
            Console.WriteLine(responseText);

            var errorJson = JObject.Parse(responseText);
            var error = errorJson["errors"]?.FirstOrDefault();

            if (error?["errorId"]?.Value<int>() == 25002)
            {
                // Offer already exists → extract offerId
                var offerId = error["parameters"]
                    ?.FirstOrDefault(p => p["name"]?.ToString() == "offerId")
                    ?["value"]
                    ?.ToString();

                if (!string.IsNullOrEmpty(offerId))
                {
                    Console.WriteLine($"Offer already exists. Publishing offer {offerId}");
                    return offerId;
                }
            }

            throw new HttpRequestException("CreateOffer failed");
        }

        
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
    public async Task publishOffer(string offerId)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"https://api.ebay.com/sell/inventory/v1/offer/{offerId}/publish");
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