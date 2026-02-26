using EbayAutomationService.Domain;
using EbayAutomationService.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using Serilog;

public class PublishOfferUseCase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EbayOfferApiClient _ebayOfferApiClient;

    private static readonly int batchSize = 20;
    private static readonly int? maxBatches = null;
    public PublishOfferUseCase(IServiceScopeFactory scopeFactory, EbayOfferApiClient ebayOfferApiClient)
    {
        _scopeFactory = scopeFactory;
        _ebayOfferApiClient = ebayOfferApiClient;

    }
    public async Task ExecuteAsync()
        {

        int batchNumber = 0;
        bool hasMore = true;

        while (hasMore && (maxBatches == null || batchNumber < maxBatches))
        { 

            batchNumber++;
            Log.Information($"Processing InvetoryCreated SKU batch #{batchNumber}...");
            // Get next batch of unprocessed SKUs
            List<int> offerCreatedSkuIds = new List<int>();
            using (var scope = _scopeFactory.CreateScope())
            {
                var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                offerCreatedSkuIds = await appDbContext.Skus
                .Where(sku => sku.SkuStatus == SkuStatuses.OfferCreated)
                .OrderBy(sku => sku.Id)  // Add ordering for consistent paging
                .Take(batchSize)
                .Select(s => s.Id)
                .ToListAsync();
            }
            if (offerCreatedSkuIds.Count == 0)
            {
                hasMore = false;
                Log.Information("Has processed all OfferCreated Skus/batches");
                break;
            }
            // Try to create OfferItem. 
            // if successful, INVETORYCREATED -> OfferCreated
            // If not either PENDING -> REJECTED or PENDING -> FAILED denpending on failure reasons
            foreach (var skuId in offerCreatedSkuIds)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var sku = await appDbContext.Skus.FindAsync(skuId);
                    var result = await _ebayOfferApiClient.publishOffer(sku.OfferId, sku.SkuCode);
                    // Mark SKU after publising offer
                    sku.SkuStatus = result;
                    await appDbContext.SaveChangesAsync();
                }
            }
        }
    }
    
}