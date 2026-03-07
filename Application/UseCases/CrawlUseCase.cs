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
    // Get akk the category Ids
    // Process each category
    public async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
        // Get the list of categorieIds
        List<int> categoryIds = null;
        using (var scope = _scopeFactory.CreateScope())
        {
            var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            categoryIds = await appDbContext.Categories.OrderBy(c => c.EbayCategoryId).Select(row => row.EbayCategoryId).ToListAsync(stoppingToken);
        }
        // Process category by category
        foreach (var categoryId in categoryIds)
        {
            await ProcessCategoryAsync(categoryId, stoppingToken);
        }
    }
    // Process each keyword of each category
    public async Task ProcessCategoryAsync(int categoryId,CancellationToken stoppingToken)
    {
        List<string> categoryKeyword = [];
        using (var scope = _scopeFactory.CreateScope())
        {
            var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = await appDbContext.Categories.Where(c => c.EbayCategoryId == categoryId).FirstOrDefaultAsync(stoppingToken);
            categoryKeyword = category.Keyword;
            var isPopulated = category.IsPopulated;
            Log.Information($"-----------------------Looping category {categoryId}. IS POPULATED {isPopulated}-----------------------");
            // Process keyword by keyword
            foreach (var keyword in categoryKeyword)
            {
                await ProcessKeywordAsync(keyword, categoryId, category.IsPopulated, stoppingToken);
            }
            // After process all keywords for the first time, mark as populated
            if (!category.IsPopulated)
            {
                category.IsPopulated = true;
                await appDbContext.SaveChangesAsync(stoppingToken);
            }
            Log.Information("Finished category {CategoryId}", categoryId);
        }

    }
    // Process all retunred pages of each keyword
    public async Task ProcessKeywordAsync(string keyword, int categoryId, bool isPopulated,CancellationToken stoppingToken)
    {
        Log.Information($"-----------------------Looping keyword {keyword}-----------------------");
        var filter = _crawlHelper.GetFilter(categoryId); // Get filter
        // If 15 duplciates in a row, skip keyword
        int streakCount = 0;
        // Process page by page
        for (int currentPage = 1; currentPage <= maxPages; currentPage++)
        {

            var (shouldContinue, updatedStreak) = await ProcessPageAsync(
                keyword,
                currentPage,
                categoryId,
                isPopulated,
                filter,
                streakCount,stoppingToken);
            streakCount = updatedStreak;
            if (!shouldContinue)
                // Will skip the current keyword
                break;
            
        }
    }
    private async Task<(bool shouldContinue, int streakCount)> ProcessPageAsync(string keyword, int currentPage, int categoryId,
        bool isPopulated, Func<string, bool> filter, int streakCount, CancellationToken stoppingToken)
    {
        try
        {
            var response = await _cjApiClient.GetCjProductListAsync(keyword, currentPage, pageSize, addMarkStatus: 1, isPopulated, stoppingToken);
            var products = response?.Data?.Content[0]?.ProductList;
            // If no products returned => move on to next keyword
            if (products == null || products.Count == 0)
            {
                Log.Information("No more products for {Keyword}", keyword);
                return (false, streakCount); // Will skip the current keyword
            }
            // Process each product 1 by 1 
            foreach (var product in products)
            {
                // Move on to the next product if can't pass filter
                if (!filter(product.NameEn))
                {
                    continue;
                }
                // If pass, save it to DirtySkus
                var success = await SaveDirtySkuAsync(product.Sku, categoryId, stoppingToken);
                // Reset counter
                if (success)
                {
                    streakCount = 0;
                }
                else
                {
                    streakCount++;
                    if (streakCount >= 15)
                    {
                        Log.Information("15 duplicates reached. Stopping.");
                        return (false, streakCount); // Will skip the current keyword
                    }
                }
            }
            return (true, streakCount);
        }
        catch (CjDailyLimitException)
        {
            Log.Information("Daily limit reached. Throwing up to worker");
            throw;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error crawling page {Page}", currentPage);
            return (false, streakCount);
        } 
    }

    private async Task<bool> SaveDirtySkuAsync(string sku, int categoryId, CancellationToken stoppingToken)
    {

        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {

                var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var exists = await appDbContext.DirtySkus.AnyAsync(d => d.Sku == sku,stoppingToken);
                if (!exists)
                {
                    var dirtySku = new DirtySku
                    {
                        Sku = sku,
                        EbayCategoryId = categoryId, // CategoryId is Fk to Category table 
                        Processed = false
                    };
                    appDbContext.DirtySkus.Add(dirtySku);
                    await appDbContext.SaveChangesAsync(stoppingToken);
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