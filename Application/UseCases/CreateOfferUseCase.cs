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

    public async Task ExecuteAsync()
    {
        int batchNumber = 0;
        while (true)
        {
            Log.Information($"Processing batch {batchNumber}...");

            List<int> inventoryItemIds = new List<int>();
            using (var scope = _scopeFactory.CreateScope())
            {
                var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                inventoryItemIds = await appDbContext.InventoryItems
                .Where(inventory => inventory.Status == InventoryStatus.Pending)
                .OrderBy(sku => sku.Id)  // Add ordering for consistent paging
                .Take(batchSize)
                .Select(s => s.Id)
                .ToListAsync();
            }


            if (inventoryItemIds.Count == 0)
            {
                Log.Information("No more InventoryCreated SKUs.");
                return;
            }

            foreach (var inventoryId in inventoryItemIds)
            {
                await ProcessSingle(inventoryId);
            }
            // Update batch#
            batchNumber++;
        }
    }
    private async Task ProcessSingle(int inventoryId)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var inventoryItem = await appDbContext.InventoryItems.Include(i => i.sku).FirstOrDefaultAsync(i => i.Id == inventoryId);
            // Log process
            Log.Information($"Trying to create an offer object for skuId {inventoryItem.sku.SkuCode}");
            var itemSpecifics = JsonConvert.DeserializeObject<Dictionary<string, string>>(inventoryItem.sku.ItemSpecifics)!.ToDictionary(kvp => kvp.Key, kvp => new List<string> { kvp.Value });
            var result = await _ebayOfferApiClient.CreateOffer(
                                                    sku: inventoryItem.sku.SkuCode,
                                                    categoryId: inventoryItem.sku.Ebay_Category_Id.ToString(),
                                                    inventoryItem.sku.SellPrice,
                                                    "DEFAULT_LOCATION",
                                                    PAYMENT_POLICY_ID,
                                                    FULFILLMENT_POLICY_ID,
                                                    RETURN_POLICY_ID);

            switch (result.Outcome)
            {
                case OperationOutcome.Success:
                    Log.Information($"Create offer successfully {inventoryItem.sku.SkuCode}");
                    inventoryItem.Status = InventoryStatus.OfferCreated;
                    break;
                case OperationOutcome.AlreadyExists:
                    Log.Information($"Offer already created. Using existing offerId {result.Value}");
                    inventoryItem.Status = InventoryStatus.OfferCreated;
                    break;

                case OperationOutcome.InvalidData:
                    Log.Information($"Offer creation failed for {inventoryItem.sku.SkuCode}. {result.RawMessage}");
                    inventoryItem.Status = InventoryStatus.Failed;
                    break;

                case OperationOutcome.RetryableFailure:
                    Log.Information("Temporary failure for {Sku}. Will retry later.", inventoryItem.sku.SkuCode);
                    return; // DO NOT change status
            }
            try
            {
                // Add to the new Inventory table
                if (result.Outcome == OperationOutcome.Success || result.Outcome == OperationOutcome.AlreadyExists)
                {
                    var offerEntity = new OfferItem
                    {
                        OfferId = result.Value,
                        InventoryId = inventoryItem.Id,
                        Quantity = 1,
                        Ebay_Category_Id = inventoryItem.sku.Ebay_Category_Id,
                        Status = InventoryStatus.Pending,
                        CreatedAt = DateTime.UtcNow,
                        SellPrice = inventoryItem.sku.SellPrice
                    };
                    appDbContext.OfferItems.Add(offerEntity);
                }
                await appDbContext.SaveChangesAsync();
                Log.Information($"Created OfferItem {inventoryItem.sku.SkuCode} successfully");
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