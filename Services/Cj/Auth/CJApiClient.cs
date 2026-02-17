using System.Net.Http.Headers;
using System.Text.Json;
using EbayAutomationService.Helper;
using EbayAutomationService.Models;
using Newtonsoft.Json;

public class CJApiClient
{
    private readonly HttpClient _httpClient;
    private readonly CjTokenManager _tokenManager;
    // Semaphore lock to ensure 1 thead can call API at a time
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private const string BaseUrl = "https://developers.cjdropshipping.com/api2.0/v1/";

    // Make sure every API request is called no more often than every 1.1 s
    // Because Cj QPS is 1 per second. This is global.
    private async Task ExecuteWithCjRateLimitAsync(Func<Task> action)
    {
        await _rateLimiter.WaitAsync();
        try
        {
            var elapsed = DateTime.UtcNow - _lastRequestTime;
            if (elapsed.TotalMilliseconds < 1100)
            {
                await Task.Delay(1100 - (int)elapsed.TotalMilliseconds);
            }

            await action(); //  the ACTUAL CJ call happens here

            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public CJApiClient(HttpClient httpClient, CjTokenManager tokenManager)
    {
        _tokenManager = tokenManager;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")
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
        T result = default!;

        await ExecuteWithCjRateLimitAsync(async () =>
        {
            await AddAuthAsync(); // token refresh is now protected

            var response = await _httpClient.GetAsync(endpoint);
            var body = await response.Content.ReadAsStringAsync();
            // Http error handling
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"CJ HTTP error: {body}");                
            }

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("code", out var codeEl))
            {
                var code = codeEl.GetInt32();
                if (code != 200)
                    throw new Exception($"CJ API error {code}: {body}");
            }

            try
            {
                result = JsonConvert.DeserializeObject<T>(body)!;
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                // Log the error
                Console.WriteLine($"[ERROR] Failed to deserialize response to {typeof(T).Name}: {ex.Message}");
                Console.WriteLine($"[ERROR] Response body: {body}");
                
                // Return default value for T (null for reference types)
                result = default!;
                
                // Or throw a more meaningful exception
                throw new Exception($"Failed to parse CJ API response to {typeof(T).Name}", ex);
            }
                    });

        return result;
    }

    // ---------- READ-ONLY ENDPOINTS ----------

    /// <summary>
    /// Get full product detail by pid
    /// </summary>
    public Task<CjProductDetailResponse> GetProductDetailAsync(string pid)
    {
        var endpoint = $"product/query?pid={pid}";
        return GetAsync<CjProductDetailResponse>(endpoint);
    }
    // By Default, Loop through 50 page, 50 products each page to LOOK for PIDs only
    // shoe rack DONE
    // shoe organizer DONE
    // shoe storage DONE
    // shoe cabinet
    // shoe shelf
    // shoe shelves
    // shoe stand
    // shoe holder
    // shoe storage cabinet
    // shoe storage rack
    // shoe storage shelf
    // shoe storage organizer
    public async Task<List<string>> GetPids(
        string productName = "shoestorage",
        int page = 100,
        int size = 100,
        int addMarkStatus = 1 // Free Shipping
        )
    {
        var skus = new List<string>();

        for (int currentPage = 1; currentPage <= page; currentPage++)
        {
            Console.WriteLine($"Scanning page {currentPage}...");

            var response = await GetAsync<CjProductListV2Response>($"product/listV2?" +
                                                                $"keyWord={productName}&" + 
                                                                $"page={currentPage}&" +
                                                                "countryCode=US&" +
                                                                $"addMarkStatus={addMarkStatus}&" +
                                                                $"size={size}&" +
                                                                "verifiedWarehouse=1&" +
                                                                "startWarehouseInventory=20&" 
                                                                );

            if (response?.Data?.Content[0].ProductList == null || response.Data.Content[0].ProductList.Count == 0)
            {
                Console.WriteLine("No products returned. Possibly end of category.");
                break;
            }
            // Check if product is likely a shoe organizer
            var productList = response.Data.Content[0].ProductList;
            foreach (var product in productList)
            {
                var productNameEn = product.NameEn;
                if (Helper.IsLikelyShoeOrganizer(productNameEn))
                {
                    skus.Add(product.Sku);
                } 
            }
            // save to the database
            await Task.Delay(600);
        }

        return skus;
    }

    public async Task<CjStockBySkuResponse> GetStockBySkuAsync(string variantSku)
    {
        return await GetAsync<CjStockBySkuResponse>($"product/stock/queryBySku?sku={variantSku}");
    }


}
