using System.Net.Http.Headers;
using System.Text.Json;
using EbayAutomationService.Services.CJ.Models;

public class CJApiClient
{
    private readonly HttpClient _httpClient;
    private readonly CjTokenManager _tokenManager;

    private const string BaseUrl = "https://developers.cjdropshipping.com/api2.0/v1/";

    public CJApiClient(CjTokenManager tokenManager)
    {
        _tokenManager = tokenManager;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json")
        );
    }
    // remove header and add it again, incase access token is updated
    private async Task AddAuthAsync()
    {
        var token = await _tokenManager.GetValidTokenAsync();
        _httpClient.DefaultRequestHeaders.Remove("CJ-Access-Token");
        _httpClient.DefaultRequestHeaders.Add("CJ-Access-Token", token);
    }

    private async Task<T> GetAsync<T>(string endpoint)
    {
        await AddAuthAsync();

        var response = await _httpClient.GetAsync(endpoint);
        var body = await response.Content.ReadAsStringAsync();
        //Console.WriteLine(body);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"CJ API error: {body}");
        try
        {
            // Check if body.code != 200
            // IsSuccessStatusCode can be 200 while body returns false
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("code", out var codeElement))
            {
                var code = codeElement.GetInt32();

                if (code != 200)
                {
                    throw new Exception($"CJ API returned code {code}: {body}");
                }
            }
            var result = JsonSerializer.Deserialize<T>(
                body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            return result;
        }
        catch (JsonException ex)
        {
            Console.WriteLine("PATH: " + ex.Path);
            throw;
        }
    }

    // ---------- READ-ONLY ENDPOINTS ----------

    /// <summary>
    /// List products from US warehouse only
    /// </summary>
    public Task<CjProductListResponse> GetUsWarehouseProductsAsync(int pageNum = 1,int pageSize = 50)
    {
        var endpoint =$"product/list?warehouseCode=US&pageNum={pageNum}&pageSize={pageSize}";

        return GetAsync<CjProductListResponse>(endpoint);
    }

    /// <summary>
    /// Get full product detail by pid
    /// </summary>
    public Task<CjProductListResponse> GetProductDetailAsync(string pid)
    {
        var endpoint = $"product/query?pid={pid}";
        return GetAsync<CjProductListResponse>(endpoint);
    }

    /// <summary>
    /// Get stock by warehouse + variant for a product
    /// </summary>
    public Task<CjProductVariantResponse> GetProductStockAsync(string pid)
    {
        var endpoint = $"product/variant/queryByPid?pid={pid}";
        return GetAsync<CjProductVariantResponse>(endpoint);
    }
}
