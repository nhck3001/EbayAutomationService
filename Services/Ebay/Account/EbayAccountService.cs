// This class is needed for setting up only, not needed during operation
using EbayAutomationService.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    ///  EbayProgramType.OutOfStockControl => "OUT_OF_STOCK_CONTROL",
    ///  EbayProgramType.PartnerMotorsDealer => "PARTNER_MOTORS_DEALER",
    /// EbayProgramType.SellingPolicyManagement => "SELLING_POLICY_MANAGEMENT",
  
  public async Task optInBusinessSellerProgram(string ebayProgramType = "SELLING_POLICY_MANAGEMENT")
    {
        var body = new
        {
            programType = ebayProgramType,
        };

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.sandbox.ebay.com/sell/account/v1/program/opt_in");
        var json = JsonConvert.SerializeObject(body);
        var response = await _api.SendAsync(request, json, true);
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
  public async Task<List<string>> getOptedInPrograms()
  {
      HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.ebay.com/sell/account/v1/program/get_opted_in_programs");

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
      
      return JObject.Parse(responseJson)["programs"]
          .Select(p => p["programType"]?.ToString())
          .Where(p => !string.IsNullOrEmpty(p))
          .ToList();
  }
}