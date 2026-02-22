using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;

public class CreateInventoryUseCase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EbayInventoryApiClient _ebayInventoryApiClient;
    private readonly ILogger<CreateInventoryUseCase> _logger;
    private readonly int batchSize = 20;

    public CreateInventoryUseCase(IServiceScopeFactory scopeFactory, EbayInventoryApiClient ebayInventoryApiClient, ILogger<CreateInventoryUseCase> logger)
    {
        _scopeFactory = scopeFactory;
        _ebayInventoryApiClient = ebayInventoryApiClient;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        while (true)
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
    }
    private async Task ProcessSingle(int skuId)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sku = await appDbContext.Skus.FindAsync(skuId);
            var itemSpecifics = JsonConvert.DeserializeObject<Dictionary<string, string>>(sku.ItemSpecifics)!.ToDictionary(kvp => kvp.Key, kvp => new List<string> { kvp.Value });
            var result = await _ebayInventoryApiClient.CreateOrUpdateInventoryItem(
                                sku: sku.SkuCode,
                                sku.Title,
                                sku.Description,
                                sku.ImageUrls.ToList(),
                                itemSpecifics,
                                10);


            switch (result.Outcome)
            {
                case OperationOutcome.Success:
                case OperationOutcome.AlreadyExists:
                    sku.SkuStatus = SkuStatuses.InventoryCreatedid;
                    break;

                case OperationOutcome.InvalidData:
                    sku.SkuStatus = SkuStatuses.Failed;
                    Log.Information("SKU {Sku} invalid: {Message}", sku.SkuCode, result.RawMessage);
                    break;

                case OperationOutcome.RetryableFailure:
                    Log.Information("Temporary failure for {Sku}. Will retry later.", sku.SkuCode);
                    return; // DO NOT change status
            }
            Log.Information($"Created/Updated inventoryItem successfully for {sku.SkuCode}");
            await appDbContext.SaveChangesAsync();
        }
    }
}