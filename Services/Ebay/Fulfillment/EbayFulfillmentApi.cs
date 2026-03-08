using EbayAutomationService.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

public class EbayFulfillmentApi
{
    private readonly EbayApiClient _api;

    public EbayFulfillmentApi(EbayApiClient api)
    {
        _api = api;
    }

    /// <summary>
    /// Get orders that are not fulfilled yet
    /// </summary>
    public async Task<List<EbayOrderResponse>> GetPendingOrders(CancellationToken stoppingToken, string fulfillmentStatus = "NOT_STARTED|IN_PROGRESS")
    {
        var url = $"https://api.ebay.com/sell/fulfillment/v1/order?filter=orderfulfillmentstatus:{{{fulfillmentStatus}}}";

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

        var response = await _api.SendAsync(request, stoppingToken);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to fetch orders. Status: {response.StatusCode}. Response: {responseText}");}

        var json = JObject.Parse(responseText);
        var orders = json["orders"].ToObject<List<EbayOrderResponse>>();

        return orders;
    }

    /// <summary>
    /// Get details of a specific order
    /// </summary>
    public async Task<JObject> GetOrderDetail(string orderId, CancellationToken stoppingToken)
    {
        var url = $"https://api.ebay.com/sell/fulfillment/v1/order/{orderId}";

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

        var response = await _api.SendAsync(request, stoppingToken);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to fetch order {orderId}. Status: {response.StatusCode}. Response: {responseText}"
            );
        }

        return JObject.Parse(responseText);
    }

    /// <summary>
    /// Upload tracking information to eBay
    /// </summary>
    public async Task<OperationResult> UploadTracking(
        string orderId,
        string lineItemId,
        string trackingNumber,
        string carrier,
        CancellationToken stoppingToken)
    {
        var body = new
        {
            lineItems = new[]
            {
                new
                {
                    lineItemId = lineItemId,
                    quantity = 1
                }
            },
            trackingNumber = trackingNumber,
            shippingCarrierCode = carrier
        };

        var jsonBody = JsonConvert.SerializeObject(body);

        var url = $"https://api.ebay.com/sell/fulfillment/v1/order/{orderId}/shipping_fulfillment";

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);

        var response = await _api.SendAsync(request, stoppingToken, jsonBody, true);

        var responseText = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return OperationResult.Success();
        }

        JObject responseJson = JObject.Parse(responseText);
        var error = responseJson["errors"]?.FirstOrDefault();

        Log.Warning($"Tracking upload failed for order {orderId}. {responseText}");

        return OperationResult.Invalid(
            $"Tracking upload failed: {error?["errorId"]} {error?["message"]}"
        );
    }
}