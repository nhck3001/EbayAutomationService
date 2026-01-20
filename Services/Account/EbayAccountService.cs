using System.Text;
using EbayAutomationService.Services;
using Newtonsoft.Json;

// This class is used to communicate to the Ebay account API
public class EbayAccountService
{
    private readonly EbayApiClient _api;
    public EbayAccountService(EbayApiClient api)
    {
        _api = api;
    }
    /// <summary>
    /// Opt in to seller program. This is a prerequisite to be able to create listings through InventoryAPI
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task optInBusinessSellerProgram()
    {
        var body = new
        {
            programType = "SELLING_POLICY_MANAGEMENT",
        };

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.sandbox.ebay.com/sell/account/v1/program/opt_in");
        var json = JsonConvert.SerializeObject(body);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _api.SendAsync(request, true);
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
    /// This method gets a list of the seller programs that the seller has opted-in to.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task getOptedInPrograms()
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.sandbox.ebay.com/sell/account/v1/program/get_opted_in_programs");

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