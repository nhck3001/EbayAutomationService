using System.Net.Http.Headers;
using System.Text.Json;
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

            result = JsonConvert.DeserializeObject<T>(body)!;
        });

        return result;
    }


    // ---------- READ-ONLY ENDPOINTS ----------


    /// <summary>
    /// Get full product detail by pid
    /// </summary>
    public Task<CjProductSingleResponse> GetProductDetailAsync(string pid)
    {
        var endpoint = $"product/query?pid={pid}";
        return GetAsync<CjProductSingleResponse>(endpoint);
    }
    public Task<CjProductListResponse> GetCategoryTreeAsync()
    {
        var endpoint = $"product/getCategory";
        return GetAsync<CjProductListResponse>(endpoint);
    }
    // By Default, Loop through 50 page, 50 products each page to LOOK for PIDs only
    public async Task<List<string>> Get2500Pids(
        int startPage,
        int endPage,
        Func<int, Task>? saveCompletedPage = null,
        Func<List<string>, Task>? savedDiscoveredPids = null,
        int pageSize = 50,
        string categoryId = "87CF251F-8D11-4DE0-A154-9694D9858EB3")
    {
        var pids = new List<string>();

        for (int page = startPage; page <= endPage; page++)
        {
            Console.WriteLine($"Scanning page {page}...");

            var response = await GetAsync<CjProductListResponse>($"product/list?warehouseCode=US&categoryId={categoryId}&pageNum={page}&pageSize={pageSize}");

            if (response?.Data?.List == null || response.Data.List.Count == 0)
            {
                Console.WriteLine("No products returned. Possibly end of category.");
                break;
            }

            var pageUsProducts = response.Data.List.Where(p => p.ShippingCountryCodes != null && p.ShippingCountryCodes.Contains("US"));

            foreach (var product in pageUsProducts)
            {
                if (!string.IsNullOrWhiteSpace(product.Pid))
                    pids.Add(product.Pid);
            }
            if (savedDiscoveredPids != null)
                await savedDiscoveredPids(pids);
            // checkpoint after each page
            if (saveCompletedPage != null)
                await saveCompletedPage(page);

            // small delay protects CJ token
            await Task.Delay(600);
        }

        return pids;
    }

    public async Task<CjStockBySkuResponse> GetStockBySkuAsync(string variantSku)
    {
        return await GetAsync<CjStockBySkuResponse>($"product/stock/queryBySku?sku={variantSku}");
    }


}
