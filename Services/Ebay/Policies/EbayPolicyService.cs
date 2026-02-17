using System.Text;
using EbayAutomationService.Services;
using Newtonsoft.Json.Linq;

public class EbayPolicyService
{
    private readonly EbayApiClient _api;

    public EbayPolicyService(EbayApiClient api)
    {
        _api = api;
    }
    private static string FindPolicyId(string json,string arrayName,string idField,string policyName)
    {
        var policies = JObject.Parse(json)[arrayName]!;
        foreach (var policy in policies)
        {
            if (policy["name"]?.ToString() == policyName)
                return policy[idField]!.ToString();
        }

        throw new Exception($"{idField} not found for policy '{policyName}'");
    }


    /// <summary>
    /// This method is to get the pyamentPolicyId by name
    /// </summary>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<string> GetPaymentPolicyId(string policyName)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://api.ebay.com/sell/account/v1/payment_policy?marketplace_id=EBAY_US"
        );

        var response = await _api.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception(json);

        return FindPolicyId(json, "paymentPolicies", "paymentPolicyId", policyName);
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
            ""returnShippingCostPayer"": ""BUYER"",
            ""returnPeriod"": {
                ""value"": 30,
                ""unit"": ""DAY""
            }
        }";
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/sell/account/v1/return_policy");
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
    public async Task<string> GetReturnPolicyId(string policyName)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://api.ebay.com/sell/account/v1/return_policy?marketplace_id=EBAY_US"
        );

        var response = await _api.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception(json);

        return FindPolicyId(json, "returnPolicies", "returnPolicyId", policyName);
    }

    
        /// <summary>
    /// This method is to get all the fulfillment policy ID
    /// </summary>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<string> GetFulfillmentPolicyId(string policyName)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://api.ebay.com/sell/account/v1/fulfillment_policy?marketplace_id=EBAY_US"
        );

        var response = await _api.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception(json);

        return FindPolicyId(json, "fulfillmentPolicies", "fulfillmentPolicyId", policyName);
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

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/sell/account/v1/payment_policy");
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