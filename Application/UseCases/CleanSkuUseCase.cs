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
    // Process a batch of 20 skus
    public async Task ProcessBatchAsync()
    {
        try
        {
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
            if (!dirtySkuIds.Any())
            {
                Log.Information("Has processed all dirty Skus/batches");
                return;
            }
            // Process batch
            foreach (var dirtySkuId in dirtySkuIds)
            {
                await ProcessSingleSku(_scopeFactory, dirtySkuId, _cjApiClient, _deepSeekClient);
                await MarkSkuProcessed(dirtySkuId);
            }
        }

        catch (CjDailyLimitException)
        {
            Log.Information($"Clean Sku. Daily limit reached for Cj. Exit gracefully");
            throw;
        }
    }
    private async Task ProcessSingleSku(IServiceScopeFactory scopeFactory, int dirtySkuId, CJApiClient cjClient, DeepSeekClient deepSeekClient)
    {

        CjProductDetailResponse productInfo;
        DirtySku dirtySku = null;
        using (var scope = scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dirtySku = await context.DirtySkus.FindAsync(dirtySkuId);
        }

        Log.Information($"Fetching product info for sku {dirtySku.Sku}");
        productInfo = await cjClient.GetProductDetailAsync(dirtySku.Sku, isProductSku: true);
        // Check if cj return productInfo
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
        // Only process the first variant of the product
        var variant = productInfo.Data.Variants.First();
        await ProcessVariant(scopeFactory, variant, dirtySku.EbayCategoryId, productInfo, cjClient, deepSeekClient);

    }

    // Function to process each variant
    private async Task ProcessVariant(IServiceScopeFactory scopeFactory, CjVariant variant, int ebayCategoryId, CjProductDetailResponse productInfo, CJApiClient cjClient, DeepSeekClient deepSeekClient)
    {
        try
        {
            // Check US availability
            var usStock = await CjHelper.GetUsStock(variant.VariantSku!, cjClient);
            if (usStock == 0)
            {
                Log.Information($"Reject {variant.VariantSku} not available in US");
                return;
            }

            // Build DeepSeek input
            var deepSeekInput = DeepSeekInput.Build(productInfo.Data, variant);
            if (deepSeekInput == null)
            {
                Log.Information($"Reject {variant.VariantSku} failed DeepSeekInput.Build");
                return;
            }

            // find out if there is a suitable category
            var categoryResult = await verifyCategory(deepSeekInput, ebayCategoryId, variant.VariantSku);

            if (categoryResult == null)
            {
                return;
            }
            if (!categoryResult.Valid)
            {
                Log.Information($"{variant.VariantSku} AI rejected: {categoryResult.RejectReason}");
                return;
            }

            // If reach here, there must be a suitable category
            var listingResult = await verifyListing(deepSeekInput, categoryResult.CategoryId, variant.VariantSku);
            if (listingResult == null)
            {
                Log.Information($"{variant.VariantSku} cannot deserialize AI response");
                return;
            }

            if (!listingResult.Valid)
            {
                Log.Information($"{variant.VariantSku} AI rejected: {listingResult.Message}");
                return;
            }

            // Save to database
            await SaveValidatedSku(scopeFactory, variant, listingResult, int.Parse(categoryResult.CategoryId), usStock);
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

    private async Task SaveValidatedSku(IServiceScopeFactory scopeFactory, CjVariant variant, AiEnrichmentResult aiResult, int ebayCategoryId, int usStock)
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
    // Take in dirtyskuId, mark the sku as processed
    private async Task MarkSkuProcessed(int dirtySkuId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sku = await context.DirtySkus.FindAsync(dirtySkuId);
        sku.Processed = true;

        await context.SaveChangesAsync();
    }

    private async Task<CategorySelectionResult?> verifyCategory(DeepSeekInput input, int ebayCategoryId, string variantSku)
    {
        var prompt = DeepSeekService.BuildCategoryPrompt(input.Name ?? "", input.Description ?? "", CrawlHelper.getCandidateCategories(ebayCategoryId.ToString()));

        var response = await _deepSeekClient.SendPrompt(prompt);
        try
        {
            return JsonConvert.DeserializeObject<CategorySelectionResult>(response);
        }

        catch (Exception ex)
        {
            Log.Error($"CATEGORY. Error processing variant {variantSku}: {ex.Message} : deepseekResult {response}");
            return null; // Return null instead of throw => simply skip the current variant sku
        }
    }
    private async Task<AiEnrichmentResult?> verifyListing(DeepSeekInput input, string categoryId, string variantSku)
    {

        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        var inputString = JsonConvert.SerializeObject(input, Formatting.Indented, settings);
        var recommendedAspects = await Helper.LoadAspectsForPrompt(categoryId, "RecommendedAspects");
        var requiredAspects = await Helper.LoadAspectsForPrompt(categoryId, "RequiredAspects");
        var prompt = DeepSeekService.BuildPrompt(inputString, requiredAspects, recommendedAspects);
        var response = await _deepSeekClient.SendPrompt(prompt);
        try
        {
            return JsonConvert.DeserializeObject<AiEnrichmentResult>(response);
        }
        catch (Exception ex)
        {
            Log.Error($"Listing. Error processing variant {variantSku}: {ex.Message} : deepseekResult {response}");
            return null; // Return null instead of throw => simply skip the current variant sku
        }

    }
}            