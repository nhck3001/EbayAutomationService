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

    private readonly CJApiClient _cjApiClient;
    private readonly DeepSeekClient _deepSeekClient;
    private static readonly int batchSize = 20;
    private static readonly int? maxBatches = null;
    public CleanSkuUseCase(IServiceScopeFactory scopeFactory, CJApiClient cjApiClient, DeepSeekClient deepSeekClient)
    {
        _scopeFactory = scopeFactory;
        _cjApiClient = cjApiClient;
        _deepSeekClient = deepSeekClient;

    }
    public async Task ProcessBatchAsync()
    {
        try
        {
            bool hasMore = true;
            Log.Information($"-----------------------Processing batch -----------------------");
            // Get next batch of unprocessed SKUs
            List<int> dirtySkuIds = new List<int>();
            using (var scope = _scopeFactory.CreateScope())
            {
                var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dirtySkuIds = await appDbContext.DirtySkus
                .Where(sku => sku.Processed == false)
                .OrderBy(sku => sku.Id)  // Add ordering for consistent paging
                .Take(batchSize)
                .Select(s => s.Id)
                .ToListAsync();
            }
            if (dirtySkuIds.Count == 0)
            {
                hasMore = false;
                Log.Information("Has processed all dirty Skus/batches");
                return;
            }
            // Process batch
            foreach (var dirtySkuId in dirtySkuIds)
            {
                await ProcessSingleSku(_scopeFactory, dirtySkuId, _cjApiClient, _deepSeekClient);
                // Mark as processeed
                using (var scope = _scopeFactory.CreateScope())
                {
                    var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var dirtySku = await appDbContext.DirtySkus.FindAsync(dirtySkuId);
                    dirtySku.Processed = true;
                    await appDbContext.SaveChangesAsync();
                }
            }
        }
        
        catch (CjDailyLimitException)
        {
            Log.Information($"Cleaning sku and daily limit reached for cj. Exit gracefully");
            throw;
        }
    }

    private static async Task ProcessSingleSku(IServiceScopeFactory scopeFactory, int dirtySkuId, CJApiClient cjClient, DeepSeekClient deepSeekClient)
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
        productInfo = await cjClient.GetProductDetailAsync( "CJSB2415367", isProductSku: true);
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
            await ProcessVariant(scopeFactory, variant, ebayCategoryId, productInfo, cjClient, deepSeekClient);
            break; // Only process 1 variant per product       
        }   
    }



    private static async Task ProcessVariant(IServiceScopeFactory scopeFactory, CjVariant variant, int ebayCategoryId,CjProductDetailResponse productInfo, CJApiClient cjClient, DeepSeekClient deepSeekClient)
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

            // Build DeepSeek input
            var deepSeekInput = DeepSeekInput.Build(productInfo.Data, variant);
            if (deepSeekInput == null)
            {
                Log.Information($"Reject sku {variant.VariantSku} failed DeepSeekInput.Build");
                return;
            }

            // Get AI validation
            // Get AI validation
            var inputString = JsonConvert.SerializeObject(deepSeekInput, Formatting.Indented, settings);
            var inputName = deepSeekInput.Name ?? "";
            var inputDescription = deepSeekInput.Description ?? "";

            var categoryPrompt = DeepSeekService.BuildCategoryPrompt(inputName, inputDescription, CrawlHelper.getCandidateCategories(ebayCategoryId.ToString()));
            var categoryResult = JsonConvert.DeserializeObject<CategorySelectionResult>(await deepSeekClient.SendPrompt(categoryPrompt));
            if (categoryResult == null)
            {
                Log.Information($"{variant.VariantSku} cannot deserialize AI response");
                return;
            }

            if (!categoryResult.Valid)
            {
                Log.Information($"{variant.VariantSku} AI rejected: {categoryResult.RejectReason}");
                return;
            }
            // If reach here, there must be a suitable category
            // Re-set the ebay category based on result from deepseek
            var finalCategoryId =  categoryResult.CategoryId;
            var recommendedAspects = await Helper.LoadAspectsForPrompt(ebayCategoryId: finalCategoryId, aspect: "RecommendedAspects");
            var requiredAspects = await Helper.LoadAspectsForPrompt(ebayCategoryId: finalCategoryId, aspect: "RequiredAspects");
            // Ask deepSeek to verify fields
            var deepSeekPrompt = DeepSeekService.BuildPrompt(inputString,requiredAspects, recommendedAspects);
            var deepSeekResult = await deepSeekClient.SendPrompt(deepSeekPrompt);
            var aiResult = JsonConvert.DeserializeObject<AiEnrichmentResult>(deepSeekResult);
            // If pass, save
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
            await SaveValidatedSku(scopeFactory, variant, aiResult, int.Parse(finalCategoryId), usStock);
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

            Log.Error($"Error processing variant {variant.VariantSku}: {ex.Message}");
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