using System.Net.Http.Headers;
using System.Text.Json;
using EbayAutomationService.Helper;
using EbayAutomationService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Npgsql;
using Serilog;

public class CJApiClient
{
    private readonly HttpClient _httpClient;
    private readonly CjTokenManager _tokenManager;
    // Semaphore lock to ensure 1 thead can call API at a time
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private const string BaseUrl = "https://developers.cjdropshipping.com/api2.0/v1/";
    public AppDbContext _appDbContext;

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

    public CJApiClient(HttpClient httpClient, CjTokenManager tokenManager, AppDbContext appDbContext)
    {
        _tokenManager = tokenManager;
        _appDbContext = appDbContext;

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
            try
            {
                await AddAuthAsync(); // token refresh is now protected

                var response = await _httpClient.GetAsync(endpoint);
                var body = await response.Content.ReadAsStringAsync();
                // Catch HttpRequestException
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"CJ HTTP error: {body}");
                }
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("code", out var codeEl))
                {
                    var code = codeEl.GetInt32();
                    if (code != 200)
                        throw new HttpRequestException($"CJ API error {code}: {body}");
                    else if (code == 1600200)
                    {
                        throw new InvalidOperationException($"CJ API rate limit exceeded {body}");
                    }
                }

                try
                {
                    result = JsonConvert.DeserializeObject<T>(body)!;
                }
                catch (Newtonsoft.Json.JsonException ex)
                {
                    // Log the error
                    Log.Warning($"[ERROR] Failed to deserialize response to {typeof(T).Name}: {ex.Message}");
                    Log.Warning($"[ERROR] Response body: {body}");

                    // Return default value for T (null for reference types)
                    result = default!;

                    // Or throw a more meaningful exception
                    throw new Exception($"Failed to parse CJ API response to {typeof(T).Name}", ex);
                }
            }
            catch (HttpRequestException ex)
            {
                Log.Warning(ex, $"HttpRequest fail for endpoint {endpoint}");
                throw; // Throw up to parent to handle
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("rate limit"))
                {
                    Log.Information("Rate limit hit. Waiting 2 seconds and retrying once");
                    await Task.Delay(2000);
                    await ExecuteWithCjRateLimitAsync(async () =>
                            {
                                await AddAuthAsync();
                                var response = await _httpClient.GetAsync(endpoint);
                                var body = await response.Content.ReadAsStringAsync();
                                
                                if (!response.IsSuccessStatusCode)
                                {
                                    throw new HttpRequestException($"CJ HTTP error on retry: {body}");
                                }
                                
                                using var doc = JsonDocument.Parse(body);
                                if (doc.RootElement.TryGetProperty("code", out var codeEl))
                                {
                                    var code = codeEl.GetInt32();
                                    if (code != 200)
                                    {
                                        throw new HttpRequestException($"CJ API error {code} on retry: {body}");
                                    }
                                }

                                result = JsonConvert.DeserializeObject<T>(body)!;
                            });
                            
                            Log.Information("Retry successful");
                }
            }

        });

        return result;
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
        catch (HttpRequestException ex)
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
        // Catch rate limit error. After retry, simply return null
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("rate limit exceeded"))
            {
                return null;
            }
            return null;
        }
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
        IServiceScopeFactory scopeFactory,
        int ebayCategoryId,
        string productName,
        Func<string, bool> productFilter,
        int page = 100,
        int size = 100,
        int addMarkStatus = 1 // Free Shipping
        )
    {
        var skus = new List<string>();

        for (int currentPage = 1; currentPage <= page; currentPage++)
        {
            Console.WriteLine($"Scanning page {currentPage}...");
            try
            {
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
                    Log.Information("No products returned. Possibly end of category.");
                    break;
                }

                // Check if product is likely a {productName)
                var productList = response.Data.Content[0].ProductList;
                foreach (var product in productList)
                {
                    var productNameEn = product.NameEn;
                    if (productFilter(productNameEn))
                    {
                        // If the product is likely a {productName}
                        // Save it to the database
                        using (var scope = scopeFactory.CreateScope())
                        {
                            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                            await SaveDirtySkuAsync(product.Sku, ebayCategoryId);

                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Log.Warning(ex, "HttpRequest error in GetPids");
                // Continue. A failed request to Cj doesn't stop crawling
                await Task.Delay(5000);
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

    private async Task SaveDirtySkuAsync(string sku, int categoryIdFromCategoryTable)
    {
        // Each field of dirtySku maps to a column in the table
        var dirtySku = new DirtySku
        {
            Sku = sku,
            CategoryId = categoryIdFromCategoryTable, // CategoryId is Fk to Category table 
            Processed = false
        };

        try
        {
            _appDbContext.DirtySkus.Add(dirtySku);
            await _appDbContext.SaveChangesAsync();
        }

        catch(DbUpdateException ex)
        {
            // 23505 = duplicate key violation
            // Safe to ignore
            if (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                Log.Information("Duplicate key. Move on");
            } 
        }
    }



}
