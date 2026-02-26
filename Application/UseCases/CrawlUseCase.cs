using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Serilog;

public class CrawlerUseCase
{
    private readonly CJApiClient _cjApiClient;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly int maxPages = 100;
    private readonly int pageSize = 100;
    public CrawlerUseCase(IServiceScopeFactory scopeFactory, CJApiClient cjApiClient)
    {
        _scopeFactory = scopeFactory;
        _cjApiClient = cjApiClient;
    }

    public async Task CrawlProductsAsync(string ebayCategoryId)
    {
        var keyWords = GetKeyWord(ebayCategoryId);
        var filterFunction = GetFilter(ebayCategoryId);
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
                            await SaveDirtySkuAsync(product.Sku, int.Parse(ebayCategoryId));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error crawling page {Page}", currentPage);
                }
            }
        }
        Log.Information($"Finish all keywords");

    }
    // Check if a product is likely a shoe organizer
    public static Func<string, bool> GetFilter(string ebayCategoryId)
    {
        switch (ebayCategoryId)
        {
            // Shoe organizer
            case "43506":
                return IsLikelyShoeOrganizer;
            case "22656":
                return IsLikelyCoatAndHatRack;
        }
        // SHould never reach here
        return null;
    }

    public static List<string> GetKeyWord(string ebayCategoryId)
    {
        switch (ebayCategoryId)
        {
            // Shoe organizer
            case "43506":
                return ["shoe rack", "shoe organizer", "shoe storage", "shoe cabinet", "shoe shelf", "shoe stand", "shoe tower", "shoe bench",];
            // Coat & Hat Rack
            case "22656":
                return ["coat rack", "hat rack", "coat and hat rack", "hall tree", "valet stand",];
        }
        // SHould never reach here
        return null;
    }

    public static bool IsLikelyShoeOrganizer(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        name = name.ToLowerInvariant();

        if (!name.Contains("shoe"))
            return false;

        string[] storageKeywords =
        {
            "rack",
            "cabinet",
            "organizer",
            "storage",
            "shelf",
            "stand",
            "tower",
            "bench",
            "cupboard",
            "closet"
        };

        return storageKeywords.Any(k => name.Contains(k));
    }
    public static bool IsLikelyCoatAndHatRack(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }
        name = name.ToLowerInvariant();

        // Must contain "coat" or "hat" l
        if (!name.Contains("coat") && !name.Contains("hat"))
        {
            return false;
        }
        string[] rackKeywords =
        {
            "rack",
            "stand",
            "hook",
            "tree",
            "valet",
            "hanger",
            "organizer",
            "storage"
        };

        return rackKeywords.Any(k => name.Contains(k));
    }

    private async Task SaveDirtySkuAsync(string sku, int categoryId)
    {


        // Each field of dirtySku maps to a column in the table
        var dirtySku = new DirtySku
        {
            Sku = sku,
            EbayCategoryId =categoryId, // CategoryId is Fk to Category table 
            Processed = false
        };

        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                appDbContext.DirtySkus.Add(dirtySku);
                await appDbContext.SaveChangesAsync();
                Log.Information($"Save dirtysku {sku} successfully");

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
            Log.Information($"{ex.Message}");

        }

    }
}