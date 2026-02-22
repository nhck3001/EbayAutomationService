using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;

public class CreateOfferUseCase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EbayOfferApiClient _ebayOfferApiClient;
    private readonly ILogger<CreateInventoryUseCase> _logger;
    private readonly int batchSize = 20;


    private readonly string FULFILLMENT_POLICY_ID = "255527791013";
    private readonly string PAYMENT_POLICY_ID = "255527709013";
    private readonly string RETURN_POLICY_ID = "255527716013";

    public CreateOfferUseCase(IServiceScopeFactory scopeFactory, EbayOfferApiClient ebayOfferApiClient, ILogger<CreateInventoryUseCase> logger)
    {
        _scopeFactory = scopeFactory;
        _ebayOfferApiClient = ebayOfferApiClient;
        _logger = logger;
    }

    public async Task ExecuteAsync(string ebayCategoryId)
    {
        int batchNumber = 0;
        while (true)
        {
            _logger.LogInformation($"Processing batch {batchNumber}...");

            List<int> inventoryCreatedSkuIds = new List<int>();
            using (var scope = _scopeFactory.CreateScope())
            {
                var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                inventoryCreatedSkuIds = await appDbContext.Skus
                .Where(sku => sku.SkuStatus == SkuStatuses.InventoryCreatedid)
                .OrderBy(sku => sku.Id)  // Add ordering for consistent paging
                .Take(batchSize)
                .Select(s => s.Id)
                .ToListAsync();
            }


            if (inventoryCreatedSkuIds.Count == 0)
            {
                Log.Information("No more InventoryCreated SKUs.");
                return;
            }

            foreach (var skuId in inventoryCreatedSkuIds)
            {
                await ProcessSingle(skuId, ebayCategoryId);
            }
            // Update batch#
            batchNumber++;
        }
    }
    private async Task ProcessSingle(int skuId, string ebayCategoryId)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sku = await appDbContext.Skus.FindAsync(skuId);
            // Log process
            Log.Information($"Trying to create an offer object for skuId {sku.SkuCode}");
            var itemSpecifics = JsonConvert.DeserializeObject<Dictionary<string, string>>(sku.ItemSpecifics)!.ToDictionary(kvp => kvp.Key, kvp => new List<string> { kvp.Value });
            var result = await _ebayOfferApiClient.CreateOffer(
                                                    sku: sku.SkuCode,
                                                    categoryId: ebayCategoryId,
                                                    sku.SellPrice,
                                                    "DEFAULT_LOCATION",
                                                    PAYMENT_POLICY_ID,
                                                    FULFILLMENT_POLICY_ID,
                                                    RETURN_POLICY_ID);

            switch (result.Outcome)
            {
                case OperationOutcome.Success:
                    Log.Information($"Create offer successfully {sku.SkuCode}");
                    sku.SkuStatus = SkuStatuses.OfferCreated;
                    sku.OfferId = result.Value;
                    break;
                case OperationOutcome.AlreadyExists:
                    Log.Information($"Offer already created. Using existing offerId {result.Value}");
                    sku.SkuStatus = SkuStatuses.OfferCreated;
                    sku.OfferId = result.Value;
                    break;

                case OperationOutcome.InvalidData:
                    Log.Information($"Offer creation failed for {sku.SkuCode}. {result.RawMessage}");
                    sku.SkuStatus = SkuStatuses.Failed;
                    break;

                case OperationOutcome.RetryableFailure:
                    Log.Information("Temporary failure for {Sku}. Will retry later.", sku.SkuCode);
                    return; // DO NOT change status
            }
            await appDbContext.SaveChangesAsync();
        }
    }
}