using EbayAutomationService.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Serilog;

public class CrawlerUseCase
{
    private readonly CJApiClient _cjApiClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CrawlHelper _crawlHelper;
    private readonly int maxPages = 100;
    private readonly int pageSize = 100;
    public CrawlerUseCase(IServiceScopeFactory scopeFactory, CJApiClient cjApiClient, CrawlHelper crawlHelper)
    {
        _scopeFactory = scopeFactory;
        _cjApiClient = cjApiClient;
        _crawlHelper = crawlHelper;
    }

    public async Task CrawlProductsAsync()
    {
        // Get the list of categorieIds
        List<int> categoryIds = null;
        using (var scope = _scopeFactory.CreateScope())
        {
            var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            categoryIds = await appDbContext.Categories.Select(row => row.EbayCategoryId).ToListAsync();

            // Loop through each categoryId
            foreach (var categoryId in categoryIds)
            {
                List<string> keyWords = null;
                keyWords = await appDbContext.Categories.Where(c => c.EbayCategoryId == categoryId).Select(c => c.Keyword).FirstOrDefaultAsync();

                Log.Information($"-----------------------Looping category {categoryId}-----------------------");
                var filterFunction = _crawlHelper.GetFilter(categoryId);
                // Querry keyword by keyword
                foreach (var keyWord in keyWords)
                {
                    Log.Information($"-----------------------Looping keyword {keyWord}-----------------------");

                    for (int currentPage = 1; currentPage <= maxPages; currentPage++)
                    {
                        try
                        {
                            var response = await _cjApiClient.GetCjProductListAsync(keyWord, currentPage, pageSize, addMarkStatus: 1);

                            if (response?.Data?.Content[0]?.ProductList == null || response.Data.Content[0].ProductList.Count == 0)
                            {
                                Log.Information($"No more products found for {keyWord}");
                                break;
                            }

                            foreach (var product in response.Data.Content[0].ProductList)
                            {
                                if (filterFunction(product.NameEn))
                                {
                                    await SaveDirtySkuAsync(product.Sku, categoryId);
                                }
                            }
                        }
                        catch (CjRateLimitException cjEx)
                        {
                            Log.Information($"Daily request for crawling reached. Exit gracefully");
                            return;
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Error crawling page {Page}", currentPage);
                        }
                    }
                }
                Log.Information($"Finish all keywords for category {categoryId}");
            }
        }
    }


    private async Task SaveDirtySkuAsync(string sku, int categoryId)
    {

        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
            
                var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var exists = await appDbContext.DirtySkus.AnyAsync(d => d.Sku == sku);
                if (!exists)
                {
                    var dirtySku = new DirtySku
                    {
                        Sku = sku,
                        EbayCategoryId = categoryId, // CategoryId is Fk to Category table 
                        Processed = false
                    };
                    appDbContext.DirtySkus.Add(dirtySku);
                    await appDbContext.SaveChangesAsync();
                    Log.Information($"Save dirtysku {sku} successfully");
                    return;
                }
                Log.Information($"Skip duplicate sku");
            }
        }

        catch (DbUpdateException ex)
        {
            // 23505 = duplicate key violation
            // Safe to ignore
            if (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                Log.Information("Duplicate key. Move on");
            }
            else
            {
                Log.Information($"{ex.InnerException.Message}");   
                Log.Information($"{ex.InnerException.Data}");
                throw;
            }
        }
    }
}