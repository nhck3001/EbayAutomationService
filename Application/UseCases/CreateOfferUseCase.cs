using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using Serilog;

public class CreateOfferUseCase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EbayOfferApiClient _ebayOfferApiClient;
    private readonly int batchSize = 20;


    private readonly string FULFILLMENT_POLICY_ID = "255527791013";
    private readonly string PAYMENT_POLICY_ID = "255527709013";
    private readonly string RETURN_POLICY_ID = "255527716013";

    public CreateOfferUseCase(IServiceScopeFactory scopeFactory, EbayOfferApiClient ebayOfferApiClient)
    {
        _scopeFactory = scopeFactory;
        _ebayOfferApiClient = ebayOfferApiClient;
    }

    public async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {

        List<int> inventoryItemIds = new List<int>();
        using (var scope = _scopeFactory.CreateScope())
        {
            var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            inventoryItemIds = await appDbContext.InventoryItems
            .Where(inventory => inventory.Status == InventoryStatus.Pending)
            .OrderBy(sku => sku.Id)  // Add ordering for consistent paging
            .Take(batchSize)
            .Select(s => s.Id)
            .ToListAsync(stoppingToken);
        }
        if (inventoryItemIds.Count == 0)
        {
            Log.Information("No more InventoryCreated SKUs.");
            return;
        }
        foreach (var inventoryId in inventoryItemIds)
        {
            await ProcessSingle(inventoryId,stoppingToken);
        }    
    }
    private async Task ProcessSingle(int inventoryId, CancellationToken stoppingToken)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var inventoryItem = await appDbContext.InventoryItems.Include(i => i.sku).FirstOrDefaultAsync(i => i.Id == inventoryId);
            // Log process
            Log.Information($"Creating an OfferItem for {inventoryItem.sku.SkuCode}");
            var itemSpecifics = JsonConvert.DeserializeObject<Dictionary<string, string>>(inventoryItem.sku.ItemSpecifics)!.ToDictionary(kvp => kvp.Key, kvp => new List<string> { kvp.Value });
            var result = await _ebayOfferApiClient.CreateOffer(
                                                    sku: inventoryItem.sku.SkuCode,
                                                    categoryId: inventoryItem.sku.Ebay_Category_Id.ToString(),
                                                    inventoryItem.sku.SellPrice,
                                                    "DEFAULT_LOCATION",
                                                    PAYMENT_POLICY_ID,
                                                    FULFILLMENT_POLICY_ID,
                                                    RETURN_POLICY_ID,
                                                    stoppingToken);

            try
            {
                // Add to the new Inventory table
                if (result.Outcome == OperationOutcome.Success || result.Outcome == OperationOutcome.AlreadyExists)
                {
                    inventoryItem.Status = InventoryStatus.OfferCreated;
                    var exists = await appDbContext.OfferItems.AnyAsync(o => o.InventoryId == inventoryItem.Id,stoppingToken);
                    if (!exists)
                    {
                        var offerEntity = new OfferItem
                        {
                            OfferId = result.Value,
                            InventoryId = inventoryItem.Id,
                            Quantity = 1,
                            Ebay_Category_Id = inventoryItem.sku.Ebay_Category_Id,
                            Status = OfferStatus.Pending,
                            CreatedAt = DateTime.UtcNow,
                            SellPrice = inventoryItem.sku.SellPrice
                        };
                        appDbContext.OfferItems.Add(offerEntity);
                    }
                    await appDbContext.SaveChangesAsync(stoppingToken);
                    Log.Information($"Created OfferItem {inventoryItem.sku.SkuCode} successfully");
                }
                else if (result.Outcome == OperationOutcome.InvalidData)
                {
                    inventoryItem.Status = InventoryStatus.Failed;
                    await appDbContext.SaveChangesAsync(stoppingToken);
                    Log.Information($"Offer creation failed for {inventoryItem.sku.SkuCode}. {result.RawMessage}");
                }

            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                Log.Information($"Duplicate SKU {inventoryItem.sku.SkuCode} - skipping");
                return;
            }

            catch (DbUpdateException ex)
            {
                Log.Warning(ex.Message);
                throw;
            }
        }
    }
}