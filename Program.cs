using EbayAutomationService.Services;
using DotNetEnv;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;
using EbayAutomationService.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using EbayAutomationService.Helper;
using EbayAutomationService.Domain;
using Npgsql;
// 43506clear

class Program
{
    static async Task Main(string[] args)
    {
        // Create a global logger that will automatically log everything to a file on top of outputting to the console
        Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().
        WriteTo.File("logs/log-.txt",rollingInterval: RollingInterval.Day,retainedFileCountLimit: 7).CreateLogger();

        // Host.CreateDefaultBuilder(args) -> Load json from appsettings.json into memory as a Dictionary.
        // context give you configuration + environment info. Ex: context.Configuration["ConnectionStrings:DefaultConnection"]
        // services is a dependency injection container
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register the database context
                // Whenever AppDbContext is needed, created using this connection string 'context.Configuration.GetConnectionString("DefaultConnection")'
                services.AddDbContext<AppDbContext>(options => options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));
                // Add a custom service DatabaseTestService.
                // Whenever a DatabaseTestService object is needed, automatically create it
                services.AddScoped<DatabaseTestService>();
                // Load environment variables
                Env.Load("/Users/nhck3001/Documents/GitHub/EbayAutomationService/file.env");
                // Register all  services with DI container
                // Services will look for dependencies and inject them automatically
                // For example, will inject CjAuthService to CjTokenManager automatically
                services.AddScoped<CjAuthService>(sp => new CjAuthService(Environment.GetEnvironmentVariable("CJ_REFRESH_TOKEN")!));

                services.AddScoped<CjTokenManager>();
                services.AddHttpClient<CJApiClient>();
                services.AddHttpClient<EbayApiClient>(); // HttpClient automatically managed

                services.AddScoped<EbayAuthService>(sp =>
                    new EbayAuthService(
                        Environment.GetEnvironmentVariable("APP_ID")!,
                        Environment.GetEnvironmentVariable("CLIENT_SECRET")!,
                        Environment.GetEnvironmentVariable("EBAY_REFRESH_TOKEN")!
                    ));

                services.AddScoped<EbayTokenManager>();
                services.AddScoped<EbayCategoryService>();
                services.AddScoped<DeepSeekClient>(sp => new DeepSeekClient(Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")!));

                services.AddScoped<EbayInventoryService>();
                services.AddScoped<EbayOfferService>();
                services.AddScoped<EbayPolicyService>();
                services.AddScoped<DatabaseTestService>();
            })
            .Build();

        // Parse arguments and decide which job to run
        if (args.Length == 0)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("dotnet run crawl   -> discover new products");
            Console.WriteLine("dotnet run skus    -> extract variant SKUs");
            return;
        }

        var job = args[0].ToLower();
        switch (job)
        {
            case "crawl":
                // Create a scope for a service
                // Instances will last till the end of the scope, then disposed
                using (var scope = host.Services.CreateScope())
                {
                    var cjApiClient = scope.ServiceProvider.GetRequiredService<CJApiClient>();
                    await RunCatalogCrawler(cjApiClient, 1, "shoe organizer");
                }
                break;

            //Get SKU with good price + in the US
            case "cleansku":
                using (var scope = host.Services.CreateScope())
                {
                    var cjApiClient = scope.ServiceProvider.GetRequiredService<CJApiClient>();
                    var deepSeekClient = scope.ServiceProvider.GetRequiredService<DeepSeekClient>();
                    var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await CleanSku(cjApiClient, deepSeekClient, appDbContext, "43506");
                }
                break;


            case "ebay":
            //var filePath = "listingCandidates.jsonl";
            //if (!File.Exists(filePath))
            //{
            //    Console.WriteLine($"File path doesn't exist {filePath}");
            //}
            //var lines = await File.ReadAllLinesAsync(filePath);
            //for (int i = 35; i < lines.Length; i++)
            //{
            //    var product = JObject.Parse(lines[i]);
            //    var title = (string)product["title"]!;
            //    var description = (string)product["description"];
            //    var images = product["images"]?.ToObject<List<string>>() ?? new List<string>();
            //
            //    var required = product["requiredFields"]?.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>();
            //    var recommended = product["recommendedFields"]?.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>();
            //
            //    var allFields = required.Concat(recommended).ToDictionary(kv => kv.Key, kv => new List<String> { kv.Value });
            //
            //    await ebayInventoryService.CreateOrUpdateInventoryItem(sku: i.ToString(), title: title, description: description, images: images,
            //                                      itemSpecifics: allFields, 10);
            //    var price = product["sellPrice"]!.ToObject<decimal>();
            //    var offerId = await ebayOfferSerivce.CreateOffer(i.ToString(), "43506", price + 3, "DEFAULT_LOCATION", "255527709013", "255527791013", "255527716013");
            //    await ebayOfferSerivce.publishOffer(offerId);
            //}
            //break;

            case "test":
                break;
        }

    }

    // These extract Variant Sku from PIDs.
    static async Task CleanSku(CJApiClient cjClient, DeepSeekClient deepSeekClient, AppDbContext context, string ebayCategoryId, int batchSize = 20, int? maxBatches = null)
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore  
        };

        // Get required and recommended aspects
        var requiredAspects = await Helper.LoadAspectsForPrompt(ebayCategoryId: ebayCategoryId, aspect:"RequiredAspects");
        var recommendedAspects = await Helper.LoadAspectsForPrompt(ebayCategoryId: ebayCategoryId, aspect:"RecommendedAspects");
        var categoryName = await Helper.GetEbayCategoryName(ebayCategoryId);
        
        int batchNumber = 0;
        bool hasMore = true;
        
        while (hasMore && (maxBatches == null || batchNumber < maxBatches))
        {
            batchNumber++;
            Log.Information($"Processing batch {batchNumber}...");
            
            // Get next batch of unprocessed SKUs
            var dirtySkus = await context.DirtySkus
                .Where(sku => sku.Processed == false)
                .OrderBy(sku => sku.Id)  // Add ordering for consistent paging
                .Take(batchSize)
                .ToListAsync();
            
            if (dirtySkus.Count == 0)
            {
                hasMore = false;
                Log.Information("Has processed all dirty Skus/batches");
                break;
            }
            
            await ProcessBatch(dirtySkus, cjClient, deepSeekClient, context, requiredAspects, recommendedAspects, categoryName, settings);
        }
    }
private static async Task ProcessBatch(List<DirtySku> dirtySkus, CJApiClient cjClient, DeepSeekClient deepSeekClient, AppDbContext context, string requiredAspects, string recommendedAspects, string categoryName, JsonSerializerSettings settings)
    {
        foreach (var dirtySku in dirtySkus)
        {
            await ProcessSingleSku(dirtySku, cjClient, deepSeekClient, context,requiredAspects, recommendedAspects, categoryName, settings);
        }
    }
    private static async Task ProcessSingleSku(DirtySku dirtySku, CJApiClient cjClient, DeepSeekClient deepSeekClient, AppDbContext context, string requiredAspectsForPrompt, string recommendedAspectsForPrompt, string categoryName, JsonSerializerSettings settings)
    {
        Log.Information($"Fetching product info for sku {dirtySku.Sku}");
        var productInfo = await cjClient.GetProductDetailAsync(dirtySku.Sku,isProductSku:true);
        
        if (productInfo.Data.Variants == null)
        {
            Log.Information($"Skip sku {dirtySku.Sku} - no variants");
            dirtySku.Processed = true;
            await context.SaveChangesAsync();
            return;
        }
        
        bool processedSuccessfully = false;
        
        foreach (var variant in productInfo.Data.Variants)
        {
            if (await ProcessVariant(variant, dirtySku, productInfo, cjClient, deepSeekClient,
                context, requiredAspectsForPrompt, recommendedAspectsForPrompt, categoryName, settings))
            {
                processedSuccessfully = true;
                break; // Successfully processed one variant
            }
        }
        
        if (!processedSuccessfully)
        {
            // Mark as processed even if no valid variants found
            dirtySku.Processed = true;
            await context.SaveChangesAsync();
        }
    }

    private static async Task<bool> ProcessVariant(CjVariant variant, DirtySku dirtySku,
        CjProductDetailResponse productInfo, CJApiClient cjClient, DeepSeekClient deepSeekClient,
        AppDbContext context, string requiredAspectsForPrompt, string recommendedAspectsForPrompt,
        string categoryName, JsonSerializerSettings settings)
    {
        try
        {
            // Check US availability
            var stockResult = await cjClient.GetStockBySkuAsync(variant.VariantSku!);
            var isAvailableUs = stockResult.Data?.Any(w => w.CountryCode.Contains("US")) ?? false;

            if (!isAvailableUs)
            {
                Log.Information($"Reject sku {variant.VariantSku} not available in US");
                return false;
            }

            // Build DeepSeek input
            var deepSeekInput = DeepSeekInput.Build(productInfo.Data, variant);
            if (deepSeekInput == null)
            {
                Log.Information($"Reject sku {variant.VariantSku} failed DeepSeekInput.Build");
                return false;
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
                return false;
            }

            if (!aiResult.Valid)
            {
                Log.Information($"{variant.VariantSku} AI rejected: {aiResult.Message}");
                return false;
            }

            // Save to database
            return await SaveValidatedSku(variant, dirtySku, productInfo, aiResult, context);
        }
        catch (Exception ex)
        {
            Log.Information($"Error processing variant {variant.VariantSku}: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> SaveValidatedSku(CjVariant variant, DirtySku dirtySku, CjProductDetailResponse productInfo, AiEnrichmentResult aiResult, AppDbContext context)
    {
        var itemSpecifics = aiResult.RequiredFields.Concat(aiResult.RecommendedFields).ToDictionary(x => x.Key, x => x.Value);
        var skuEntity = new Sku
        {
            SkuCode = variant.VariantSku!,
            Pid = productInfo.Data.Pid!,
            Title = aiResult.Title,
            Description = aiResult.Description,
            ImageUrls = aiResult.Images?.ToArray() ?? Array.Empty<string>(),
            ItemSpecifics = JsonConvert.SerializeObject(itemSpecifics),
            SellPrice = aiResult.Sellprice,
            Processed = false,
            CreatedAt = DateTime.UtcNow
        };
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Skus.Add(skuEntity);
            dirtySku.Processed = true;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            Log.Information($"Saved {dirtySku.Sku} successfully");
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            await transaction.RollbackAsync();
            Log.Information($"Duplicate SKU {variant.VariantSku} - skipping");
            return false;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Log.Information($"Error saving SKU {variant.VariantSku}: {ex.Message}");
            return false;
        }
    }
    static async Task RunCatalogCrawler(CJApiClient cjClient, int ebayCategoryId, string productNameEn)
    {
        var pids = await cjClient.GetPids(ebayCategoryId, productNameEn, Helper.IsLikelyShoeOrganizer);
    }
}

    public class EbayCategoryNode
    {
        public string CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<EbayCategoryNode> Children { get; set; } = new();
        public static EbayCategoryNode ParseNode(JToken node)
        {
            var category = node["category"];

            var result = new EbayCategoryNode
            {
                CategoryId = category["categoryId"]?.ToString(),
                CategoryName = category["categoryName"]?.ToString()
            };

            var children = node["childCategoryTreeNodes"];

            if (children != null)
            {
                foreach (var child in children)
                {
                    result.Children.Add(ParseNode(child));
                }
            }

            return result;
        }

        public static EbayCategoryNode? FindCategory(EbayCategoryNode node, string name)
        {
            if (node.CategoryName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return node;

            foreach (var child in node.Children)
            {
                var found = FindCategory(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        public static void PrintAllChildren(EbayCategoryNode node, int depth = 0)
        {
            string indent = new string(' ', depth * 2);

            Console.WriteLine($"{indent}{node.CategoryName} ({node.CategoryId})");

            foreach (var child in node.Children)
            {
                PrintAllChildren(child, depth + 1);
            }
        }

        public static (EbayCategoryNode? node, EbayCategoryNode? parent)
        FindById(EbayCategoryNode current, string id, EbayCategoryNode? parent = null)
        {
            if (current.CategoryId == id)
                return (current, parent);

            foreach (var child in current.Children)
            {
                var result = FindById(child, id, current);
                if (result.node != null)
                    return result;
            }

            return (null, null);
        }


    }
    





