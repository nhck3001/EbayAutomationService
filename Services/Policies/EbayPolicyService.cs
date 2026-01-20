using System.Text;
using EbayAutomationService.Services;

public class EbayPolicyService
{
    private readonly EbayApiClient _api;

    public EbayPolicyService(EbayApiClient api)
    {
        _api = api;
    }

    /// <summary>
    /// This method is to get the pyamentPolicyId by name
    /// </summary>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<string> getPaymentPolicyIDByName(string policyName)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://api.sandbox.ebay.com/sell/account/v1/payment_policy/get_by_policy_name?marketplace_id=EBAY_US&name={policyName}&");
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
        string paymentPolicyId = Newtonsoft.Json.Linq.JObject.Parse(responseJson)["paymentPolicyId"]?.ToString() ?? throw new Exception("fulfillmentPolicyId not found");

        return paymentPolicyId;
    }

    /// <summary>
    /// This method is to get all the fulfillment policy ID
    /// </summary>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task createReturnPolicy()
    {
        string jsonBody = @"
        {
            ""name"": ""m********e"",
            ""marketplaceId"": ""EBAY_US"",
            ""refundMethod"": ""MONEY_BACK"",
            ""returnsAccepted"": true,
            ""returnShippingCostPayer"": ""SELLER"",
            ""returnPeriod"": {
                ""value"": 30,
                ""unit"": ""DAY""
            }
        }";
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.sandbox.ebay.com/sell/account/v1/return_policy");
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
    }

    /// <summary>
    /// This method is to get the return policy Id by name
    /// </summary>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<string> getReturnPolicyIDByName(string returnPolicyName)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://api.sandbox.ebay.com/sell/account/v1/return_policy/get_by_policy_name?marketplace_id=EBAY_US&name={returnPolicyName}&");
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
        string returnPolicyId = Newtonsoft.Json.Linq.JObject.Parse(responseJson)["returnPolicyId"]?.ToString() ?? throw new Exception("fulfillmentPolicyId not found");

        return returnPolicyId;
    }
    
        /// <summary>
    /// This method is to get all the fulfillment policy ID
    /// </summary>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<string> getFulfillmentPolicyIDs()
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.sandbox.ebay.com/sell/account/v1/fulfillment_policy/get_by_policy_name?marketplace_id=EBAY_US&name=MyPolicy&");

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
        string fulfillmentPolicyId = Newtonsoft.Json.Linq.JObject.Parse(responseJson)["fulfillmentPolicyId"]?.ToString() ?? throw new Exception("fulfillmentPolicyId not found");

        return fulfillmentPolicyId;
    }

    /// <summary>
    /// This method is used to create a payment policy ID
    /// </summary>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<string> createPaymentmentPolicy()
    {
        string jsonBody = @"
                        {
                            ""name"": ""MyPaymentPolicy"",
                            ""marketplaceId"": ""EBAY_US"",
                            ""categoryTypes"": [
                                {
                                    ""name"": ""ALL_EXCLUDING_MOTORS_VEHICLES""
                                }
                            ]
                        }";

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.sandbox.ebay.com/sell/account/v1/payment_policy");
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
        string paymentPolicyId = Newtonsoft.Json.Linq.JObject.Parse(responseJson)["fulfillmentPolicyId"]?.ToString() ?? throw new Exception("fulfillmentPolicyId not found");

        return paymentPolicyId;
    }
    
}