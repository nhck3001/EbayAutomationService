using System.Net.Http.Headers;
using System.Text.Json;

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
        T result = default!;

        await ExecuteWithCjRateLimitAsync(async () =>
        {
            await AddAuthAsync(); // token refresh is now protected

            var response = await _httpClient.GetAsync(endpoint);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"CJ HTTP error: {body}");

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("code", out var codeEl))
            {
                var code = codeEl.GetInt32();
                if (code != 200)
                    throw new Exception($"CJ API error {code}: {body}");
            }

            result = JsonSerializer.Deserialize<T>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            )!;
        });

        return result;
    }


    // ---------- READ-ONLY ENDPOINTS ----------

    /// <summary>
    /// List products from US warehouse only
    /// </summary>
    public Task<CjProductListResponse> GetUsWarehouseProductsAsync(int pageNum = 200, int pageSize = 50)
    {
        var endpoint = $"product/list?warehouseCode=US&pageNum={pageNum}&pageSize={pageSize}";

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
    public Task<CjProductListResponse> GetCategoryTreeAsync()
    {
        var endpoint = $"product/getCategory";
        return GetAsync<CjProductListResponse>(endpoint);
    }
    // For now, we will harcode the categoryId as HomeOfficeStorage
    public async Task<List<CjProductDetail>> GetProductBasedOnCategoryAsync(int maxPages = 1, int pageSize = 50, string categoryId = "87CF251F-8D11-4DE0-A154-9694D9858EB3")
    {
        var usProducts = new List<CjProductDetail>();
        // Loop through each page
        for (int page = 1; page <= maxPages; page++)
        {
            Console.WriteLine($"Scanning page {page}...");
            var response = await GetAsync<CjProductListResponse>($"product/list?warehouseCode=US&categoryId={categoryId}&pageNum={page}&pageSize={pageSize}");

            // Filter for products that are shipped from the USA
            var pageUsProducts = response.Data.List
            .Where(p =>
                p.ShippingCountryCodes != null &&
                p.ShippingCountryCodes.Contains("US"))
            .ToList();

            usProducts.AddRange(pageUsProducts);
            Console.WriteLine($"Page {page}: {pageUsProducts.Count} US products found (total: {usProducts.Count})");
        }

        return usProducts;
    }
    
    // Function t

        
        


}
