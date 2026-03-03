using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using Serilog;

public class CreateInventoryUseCase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EbayInventoryApiClient _ebayInventoryApiClient;
    private readonly int batchSize = 20;

    public CreateInventoryUseCase(IServiceScopeFactory scopeFactory, EbayInventoryApiClient ebayInventoryApiClient)
    {
        _scopeFactory = scopeFactory;
        _ebayInventoryApiClient = ebayInventoryApiClient;
    }
    // Process 1 batch of Pending Sku
    public async Task ProcessBatchAsync()
    {

        List<int> penndingSkuIds = new List<int>();    
        using (var scope = _scopeFactory.CreateScope())
        {
            var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            penndingSkuIds = await appDbContext.Skus
            .Where(sku => sku.SkuStatus == SkuStatuses.Pending)
            .OrderBy(sku => sku.Id)  // Add ordering for consistent paging
            .Take(batchSize)
            .Select(s => s.Id)
            .ToListAsync();
        }
        if (penndingSkuIds.Count == 0)
        {
            Log.Information("No more pending SKUs.");
            return;
        }
        foreach (var skuId in penndingSkuIds)
        {
            await ProcessSingle(skuId);
        }
        
    }
    private async Task ProcessSingle(int skuId)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sku = await appDbContext.Skus.FindAsync(skuId);
            if (sku == null)
            {
                Log.Warning("Sku {SkuId} not found.", skuId);
                return;
            }
            // Log process
            Log.Information($"Trying to create an InventoryItem for skuId {sku.SkuCode}");
            Dictionary<string, List<string>>? itemSpecifics;
            try
            {
                itemSpecifics = JsonConvert.DeserializeObject<Dictionary<string, string>>(sku.ItemSpecifics).ToDictionary(kvp => kvp.Key, kvp => new List<string> { kvp.Value });

            }
            catch (Exception ex)
            {
                Log.Warning("Invalid ItemSpecifics for {Sku}: {Error}", sku.SkuCode, ex.Message);
                sku.SkuStatus = SkuStatuses.Failed;
                await appDbContext.SaveChangesAsync();
                return;
            }
            var result = await _ebayInventoryApiClient.CreateOrUpdateInventoryItem(
                                sku: sku.SkuCode,
                                sku.Title,
                                sku.Description,
                                sku.ImageUrls.ToList(),
                                itemSpecifics,
                                sku.availableInventory);


            switch (result.Outcome)
            {
                case OperationOutcome.InvalidData:
                    sku.SkuStatus = SkuStatuses.Failed;
                    Log.Information($"SKU {sku.SkuCode} invalid: {result.RawMessage}");
                    break;

                case OperationOutcome.RetryableFailure:
                    Log.Information($"Temporary failure for {sku.SkuCode}. Will retry later.");
                    return; // DO NOT change status
            }

            try
            {
                // Add to the new Inventory table
                if (result.Outcome == OperationOutcome.Success || result.Outcome == OperationOutcome.AlreadyExists)
                {
                    sku.SkuStatus = SkuStatuses.InventoryCreatedid;
                    var exists = await appDbContext.InventoryItems.AnyAsync(i => i.SkuId == sku.Id);
                    // Check if sku alreadyy exist
                    if (!exists)
                    {
                        appDbContext.InventoryItems.Add(new InventoryItem
                        {
                            SkuId = sku.Id,
                            Status = InventoryStatus.Pending,
                            CreatedAt = DateTime.UtcNow,
                            AvailableInventory = sku.availableInventory,
                        });
                    }
                    await appDbContext.SaveChangesAsync();
                    Log.Information($"Created InventoryItem {sku.SkuCode} successfully");

                }
            }

            catch (DbUpdateException ex)
            {
                Log.Warning(ex.Message);
                throw;
            }
        }
    }
}