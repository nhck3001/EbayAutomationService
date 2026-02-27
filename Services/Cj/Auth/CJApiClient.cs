using System.Net.Http.Headers;
using System.Text.Json;
using EbayAutomationService.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;

public class CJApiClient
{
    private readonly HttpClient _httpClient;
    private readonly CjTokenManager _tokenManager;
    private const string BaseUrl = "https://developers.cjdropshipping.com/api2.0/v1/";
    private CJRateLimiter _rateLimiter;

    public CJApiClient(HttpClient httpClient, CjTokenManager tokenManager, CJRateLimiter rateLimiter)
    {
        _tokenManager = tokenManager;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _rateLimiter = rateLimiter;
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
    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action)
    {
        const int maxRetries = 5;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (CjRateLimitException)
            {
                if (attempt == maxRetries)
                    throw;

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                Log.Warning("Rate limit. Retrying in {Delay}s", delay.TotalSeconds);
                await Task.Delay(delay);
            }
        }

        throw new Exception("Unexpected retry exit");
    }
    private async Task<T> GetAsync<T>(string endpoint)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            HttpResponseMessage response = null!;
            string body = null!;

            await _rateLimiter.ExecuteWithCjRateLimitAsync(async () =>
            {
                await AddAuthAsync();
                response = await _httpClient.GetAsync(endpoint);
            });

            body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new CjApiException((int)response.StatusCode, body);

            using var doc = JsonDocument.Parse(body);

            if (doc.RootElement.TryGetProperty("code", out var codeEl))
            {
                var code = codeEl.GetInt32();
                // Throw up to ExecuteWithRetryAsync
                if (code == 1600200)
                    throw new CjRateLimitException();
                // Throw up to ExecuteWithRetryAsync
                if (code != 200)
                    throw new CjApiException(code, body);
            }

            return JsonConvert.DeserializeObject<T>(body)!;
        });
    }

    // ---------- READ-ONLY ENDPOINTS ----------

    /// <summary>
    /// Get full product detail by sku
    /// </summary>
    public async Task<CjProductDetailResponse> GetProductDetailAsync(string sku, bool isProductSku = true)
    {
        // Default is variantSku
        var endpoint = $"product/query?variantSku={sku}&countryCode=US";
        if (isProductSku == true)
        {
            endpoint = $"product/query?productSku={sku}&countryCode=US";
        }
        try
        {
            var result = await GetAsync<CjProductDetailResponse>(endpoint);
            return result;
        }
        // Product has been removed from cell. 
        catch (CjApiException ex)
        {
            if (ex.Message.Contains("Product has been removed from shelves"))
            {
                return null;
            }
            // Catch other exception, do not swallow it
            else
            {
                Log.Warning(ex, "Network error when fetching product {Sku}", sku);
                throw;
            }
        }
    }
    // By Default, Loop through 50 page, 50 products each page to LOOK for PIDs only

    public async Task<CjProductListV2Response> GetCjProductListAsync(string keyword, int page, int size, int addMarkStatus)
    {
        try
        {
            var response = await GetAsync<CjProductListV2Response>($"product/listV2?" +
                                                   $"keyWord={keyword}&" +
                                                   $"page={page}&" +
                                                   "countryCode=US&" +
                                                   $"addMarkStatus={addMarkStatus}&" +
                                                   $"size={size}&" +
                                                   "verifiedWarehouse=1&" +
                                                   "startWarehouseInventory=20&"
                                                   );
            return response;

        }
        catch (CjApiException ex)
        {
            Log.Warning(ex, $" error in GetPids {ex.Message}");
            throw;
        }
    }

    public async Task<CjStockBySkuResponse> GetStockBySkuAsync(string variantSku)
    {
        return await GetAsync<CjStockBySkuResponse>($"product/stock/queryBySku?sku={variantSku}");
    }
        public async Task ForceQpsAsync()
        {
            var tasks = new List<Task>();

            for (int i = 0; i < 50; i++)
            {
                tasks.Add(GetCjProductListAsync("shoe rack", 1, 100, 1));
            }

            await Task.WhenAll(tasks);
        }
}
