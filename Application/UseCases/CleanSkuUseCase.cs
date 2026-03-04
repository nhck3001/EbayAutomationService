using EbayAutomationService.Domain;
using EbayAutomationService.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using Serilog;

public class CleanSkuUseCase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EbayOfferApiClient _ebayOfferApiClient;

    private readonly CJApiClient _cjApiClient;
    private readonly DeepSeekClient _deepSeekClient;
    private static readonly int batchSize = 20;
    private static readonly int? maxBatches = null;
    public CleanSkuUseCase(IServiceScopeFactory scopeFactory, EbayOfferApiClient ebayOfferApiClient, CJApiClient cjApiClient, DeepSeekClient deepSeekClient)
    {
        _scopeFactory = scopeFactory;
        _ebayOfferApiClient = ebayOfferApiClient;
        _cjApiClient = cjApiClient;
        _deepSeekClient = deepSeekClient;

    }
    public async Task ExecuteAsync()
    {
        try
        {
            // Get the list of categorieIds
            List<string> categoryIds = null;
            using (var scope = _scopeFactory.CreateScope())
            {
                var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                categoryIds = await appDbContext.Categories.Select(row => row.EbayCategoryId.ToString()).ToListAsync();
            }
            foreach (var categoryId in categoryIds)
            {
                var requiredAspects = await Helper.LoadAspectsForPrompt(ebayCategoryId: categoryId, aspect: "RequiredAspects");
                var recommendedAspects = await Helper.LoadAspectsForPrompt(ebayCategoryId: categoryId, aspect: "RecommendedAspects");
                string categoryName = null;
                using (var scope = _scopeFactory.CreateScope())
                {
                    var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    categoryName = appDbContext.Categories.Where(c => c.EbayCategoryId == int.Parse(categoryId)).Select(s => s.EbayCategoryName).First();
                }
                int batchNumber = 0;
                bool hasMore = true;
                Log.Information($"-----------------------Clean category {categoryId}-----------------------");
                while (hasMore && (maxBatches == null || batchNumber < maxBatches))
                {
                    batchNumber++;
                    Log.Information($"-----------------------Processing batch {batchNumber}-----------------------");

                    // Get next batch of unprocessed SKUs
                    List<int> dirtySkuIds = new List<int>();
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        dirtySkuIds = await appDbContext.DirtySkus
                        .Where(sku => sku.Processed == false && sku.EbayCategoryId == int.Parse(categoryId))
                        .OrderBy(sku => sku.Id)  // Add ordering for consistent paging
                        .Take(batchSize)
                        .Select(s => s.Id)
                        .ToListAsync();
                    }

                    if (dirtySkuIds.Count == 0)
                    {
                        hasMore = false;
                        Log.Information("Has processed all dirty Skus/batches");
                        break;
                    }

                    await ProcessBatch(_scopeFactory, dirtySkuIds, _cjApiClient, _deepSeekClient, requiredAspects, recommendedAspects, categoryName);
                }
                Log.Information($"Finish cleaning category {categoryId}...");

            }
        }
        catch (CjDailyLimitException)
        {
            Log.Information($"Cleaning sku and daily limit reached for cj. Exit gracefully");
            return;
        }
    }

    private static async Task ProcessBatch(IServiceScopeFactory scopeFactory, List<int> dirtySkuIds, CJApiClient cjClient, DeepSeekClient deepSeekClient, string requiredAspects, string recommendedAspects, string categoryName)
    {

        foreach (var dirtySkuId in dirtySkuIds)
        {
            await ProcessSingleSku(scopeFactory, dirtySkuId, cjClient, deepSeekClient, requiredAspects, recommendedAspects, categoryName);
            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var dirtySku = await context.DirtySkus.FindAsync(dirtySkuId);
                dirtySku.Processed = true;
                await context.SaveChangesAsync();
            }
        }
               
    }
    private static async Task ProcessSingleSku(IServiceScopeFactory scopeFactory, int dirtySkuId, CJApiClient cjClient, DeepSeekClient deepSeekClient, string requiredAspectsForPrompt, string recommendedAspectsForPrompt, string categoryName)
    {

        CjProductDetailResponse productInfo = new CjProductDetailResponse();
        var ebayCategoryId = 0;
        DirtySku dirtySku = null;
        using (var scope = scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dirtySku = await context.DirtySkus.FindAsync(dirtySkuId);
        }

            Log.Information($"Fetching product info for sku {dirtySku.Sku}");
            productInfo = await cjClient.GetProductDetailAsync(dirtySku.Sku, isProductSku: true);
            ebayCategoryId = dirtySku.EbayCategoryId;
            if (productInfo == null)
            {
                Log.Information($"Skip sku {dirtySku.Sku}. Product has been removed from shell");
                return;
            }
            else if (productInfo.Data.Variants == null)
            {
                Log.Information($"Skip sku {dirtySku.Sku} - no variants");
                return;
            }

            foreach (var variant in productInfo.Data.Variants)
            {
                await ProcessVariant(scopeFactory, variant, ebayCategoryId, productInfo, cjClient, deepSeekClient, requiredAspectsForPrompt, recommendedAspectsForPrompt, categoryName);
                break; // Only process 1 variant per product       
            }   
    }



    private static async Task ProcessVariant(IServiceScopeFactory scopeFactory, CjVariant variant, int ebayCategoryId,
        CjProductDetailResponse productInfo, CJApiClient cjClient, DeepSeekClient deepSeekClient,
         string requiredAspectsForPrompt, string recommendedAspectsForPrompt,
        string categoryName)

    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        try
        {
            // Check US availability
            var stockResult = await cjClient.GetStockBySkuAsync(variant.VariantSku!);
            var usStock = stockResult.Data.Where(w => w.CountryCode.Contains("US")).FirstOrDefault()?.CjInventoryNum ?? 0;
            if (usStock == 0)
            {
                Log.Information($"Reject sku {variant.VariantSku} not available in US");
                return;
            }
            // Check if there's stock in US warehouse
            

            // Build DeepSeek input
            var deepSeekInput = DeepSeekInput.Build(productInfo.Data, variant);
            if (deepSeekInput == null)
            {
                Log.Information($"Reject sku {variant.VariantSku} failed DeepSeekInput.Build");
                return;
            }

            // Get AI validation
            var deepSeekPrompt = DeepSeekService.BuildPrompt(
                JsonConvert.SerializeObject(deepSeekInput, Formatting.Indented, settings),
                requiredAspectsForPrompt, recommendedAspectsForPrompt, categoryName);

            var deepSeekResult = await deepSeekClient.SendPrompt(deepSeekPrompt);
            var aiResult = JsonConvert.DeserializeObject<AiEnrichmentResult>(deepSeekResult);

            if (aiResult == null)
            {
                Log.Information($"{variant.VariantSku} cannot deserialize AI response");
                return;
            }

            if (!aiResult.Valid)
            {
                Log.Information($"{variant.VariantSku} AI rejected: {aiResult.Message}");
                return;
            }

            // Save to database
            await SaveValidatedSku(scopeFactory, variant, aiResult,ebayCategoryId, usStock);
        }
        // Catch database exception first
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException pg)
            {
                Log.Error($@"
                            POSTGRES ERROR
                            Code: {pg.SqlState}
                            Message: {pg.MessageText}
                            Detail: {pg.Detail}
                            Where: {pg.Where}
                            Constraint: {pg.ConstraintName}
                            Column: {pg.ColumnName}
                            Table: {pg.TableName}
                            ");
            }
            else
            {
                Log.Error(ex, "Unknown database error");
            }
        }
        catch (Exception ex)
        {

            Log.Information($"Progaram.cs Error processing variant {variant.VariantSku}: {ex.Message}");
            return;
        }
    }

    private static async Task SaveValidatedSku(IServiceScopeFactory scopeFactory,CjVariant variant, AiEnrichmentResult aiResult,int ebayCategoryId, int usStock)
    {
        var itemSpecifics = aiResult.RequiredFields.Concat(aiResult.RecommendedFields).ToDictionary(x => x.Key, x => x.Value);
        var skuEntity = new Sku
        {
            SkuCode = variant.VariantSku!,
            Title = aiResult.Title,
            Description = aiResult.Description,
            Ebay_Category_Id = ebayCategoryId,
            ImageUrls = aiResult.Images?.ToArray() ?? Array.Empty<string>(),
            ItemSpecifics = JsonConvert.SerializeObject(itemSpecifics),
            SellPrice = aiResult.Sellprice,
            SkuStatus = SkuStatuses.Pending,
            CreatedAt = DateTime.UtcNow,
            availableInventory = usStock,
        };
        // Add to Sku table
        try
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Skus.Add(skuEntity);
                await context.SaveChangesAsync();
                Log.Information($"Save qualified Sku {variant.VariantSku} successfully");

                return;
            }
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            Log.Information($"Duplicate SKU {variant.VariantSku} - skipping");
            return;
        }

        catch (DbUpdateException ex)
        {
            Log.Warning(ex.Message);
            throw;
        }

    }

    
}