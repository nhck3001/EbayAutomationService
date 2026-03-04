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
            categoryIds = await appDbContext.Categories.OrderBy(c => c.EbayCategoryId).Select(row => row.EbayCategoryId).ToListAsync();

            // Loop through each categoryId
            foreach (var categoryId in categoryIds)
            {
                var category = await appDbContext.Categories.Where(c => c.EbayCategoryId == categoryId).FirstOrDefaultAsync();
                var isPopulated = category.IsPopulated;
                Log.Information($"-----------------------Looping category {categoryId}. IS POPULATED {isPopulated}-----------------------");
                var filterFunction = _crawlHelper.GetFilter(categoryId);
                // Querry keyword by keyword
                foreach (var keyWord in category.Keyword)
                {
                    Log.Information($"-----------------------Looping keyword {keyWord}-----------------------");
                    // If 15 duplciates in a row, skip keyword
                    int streakCount = 0;
                    for (int currentPage = 1; currentPage <= maxPages; currentPage++)
                    {
                        try
                        {
                            var response = await _cjApiClient.GetCjProductListAsync(keyWord, currentPage, pageSize, addMarkStatus: 1, isPopulated);

                            if (response?.Data?.Content[0]?.ProductList == null || response.Data.Content[0].ProductList.Count == 0)
                            {
                                Log.Information($"No more products found for {keyWord}");
                                break;
                            }

                            foreach (var product in response.Data.Content[0].ProductList)
                            {
                                if (filterFunction(product.NameEn))
                                {
                                    var success = await SaveDirtySkuAsync(product.Sku, categoryId);
                                    // Reset counter
                                    if (success)
                                    {
                                        streakCount = 0;
                                    }
                                    else
                                    {
                                        streakCount++;
                                        if (streakCount == 15)
                                        {
                                            Log.Information("15 consecutive duplicates for keyword {Keyword}. Skipping remaining pages.", keyWord);
                                            break;
                                        }
                                    }
                                }
                            }
                            // skip remaining page will go to here. Now break 1 more time 
                            if (streakCount == 15)
                            {
                                break;
                            }           
                        }
                        catch (CjDailyLimitException cjEx)
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
                // After looping through all keyword in category, mark as populated if not
                if (category.IsPopulated == false)
                {
                    category.IsPopulated = true;
                    await appDbContext.SaveChangesAsync();        
                }
                Log.Information($"Finish all keywords for category {categoryId}");
            }
        }
    }


    private async Task<bool> SaveDirtySkuAsync(string sku, int categoryId)
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
                    return true;
                }
                Log.Information($"Skip duplicate sku");
                return false;
            }
        }

        catch (DbUpdateException ex)
        {
            // 23505 = duplicate key violation
            // Safe to ignore
            if (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                Log.Information("Duplicate key. Move on");
                return false;
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