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
        WriteTo.File(
            "logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
        .CreateLogger();
        // Host.CreateDefaultBuilder(args) -> Load json from appsettings.json into memory as a Dictionary.
        // context give you configuration + environment info. Ex: context.Configuration["ConnectionStrings:DefaultConnection"]
        // services is a dependency injection container

        var host = Host.CreateDefaultBuilder(args)

            .ConfigureServices((context, services) =>
            {

                // Register the database context
                // Whenever AppDbContext is needed, created using this connection string 'context.Configuration.GetConnectionString("DefaultConnection")'
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection"));
                });
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

                services.AddScoped<EbayInventoryApiClient>();
                services.AddScoped<EbayOfferService>();
                services.AddScoped<EbayPolicyService>();
                services.AddScoped<DatabaseTestService>();
                services.AddScoped<CreateInventoryUseCase>();
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
                    var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
                    var cjApiClient = scope.ServiceProvider.GetRequiredService<CJApiClient>();
                    await RunCatalogCrawler(scopeFactory,cjApiClient, 1, "shoe tower");
                }
                break;

            //Get SKU with good price + in the US
            case "cleansku":
                using (var scope = host.Services.CreateScope())
                {
                    var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
                    var cjApiClient = scope.ServiceProvider.GetRequiredService<CJApiClient>();
                    var deepSeekClient = scope.ServiceProvider.GetRequiredService<DeepSeekClient>();
                    var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await CleanSku(scopeFactory,cjApiClient, deepSeekClient, "43506");
                }
                break;


            case "createinventoryitem":
                Log.Information("Start process sku from PENDING -> create ebay InventoryItem");
                using (var scope = host.Services.CreateScope())
                {
                    var useCase = scope.ServiceProvider.GetRequiredService<CreateInventoryUseCase>();
                    await useCase.ExecuteAsync();
                }
                break;

            case "createoffer":
                Log.Information("Start process sku from InventoryCreated -> OfferCreated");
                using (var scope = host.Services.CreateScope())
                {
                    var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
                    var ebayOfferService = scope.ServiceProvider.GetRequiredService<EbayOfferService>();
                    await CreateOffer(scopeFactory, ebayOfferService,"43506");
                }
                break;

            case "publishoffer":
                Log.Information("Start to publish offer");
                using (var scope = host.Services.CreateScope())
                {
                    var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
                    var ebayOfferService = scope.ServiceProvider.GetRequiredService<EbayOfferService>();
                    await PublishOffer(scopeFactory, ebayOfferService,"43506");
                }
                break;          
            case "test":
                using (var scope = host.Services.CreateScope())
                {
                    var cjApiClient = scope.ServiceProvider.GetRequiredService<CJApiClient>();
                    var x = await cjApiClient.GetProductDetailAsync("CJCC26903710001", isProductSku:false);
                }
                break;
        }
    }
    static async Task PublishOffer(IServiceScopeFactory scopeFactory, EbayOfferService ebayOfferService, string categoryId, int batchSize = 20, int? maxBatches = null)
    {
        int batchNumber = 0;
        bool hasMore = true;

        while (hasMore && (maxBatches == null || batchNumber < maxBatches))
        {

            batchNumber++;
            Log.Information($"Processing InvetoryCreated SKU batch #{batchNumber}...");
            // Get next batch of unprocessed SKUs
            List<int> offerCreatedSkuIds = new List<int>();
            using (var scope = scopeFactory.CreateScope())
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
                using (var scope = scopeFactory.CreateScope())
                {
                    var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var sku = await appDbContext.Skus.FindAsync(skuId);
                    var result = await ebayOfferService.publishOffer(sku.OfferId, sku.SkuCode);
                    // Mark SKU after publising offer
                    sku.SkuStatus = result;
                    await appDbContext.SaveChangesAsync();
                }
            }
        }
    }
    static async Task CreateOffer(IServiceScopeFactory scopeFactory, EbayOfferService ebayOfferService, string categoryId, int batchSize = 20, int? maxBatches = null)
    {
        int batchNumber = 0;
        bool hasMore = true;

        while (hasMore && (maxBatches == null || batchNumber < maxBatches))
        {

            batchNumber++;
            Log.Information($"Processing InvetoryCreated SKU batch #{batchNumber}...");
            // Get next batch of unprocessed SKUs
            List<int> inventoryCreatedSkuIds = new List<int>();
            using (var scope = scopeFactory.CreateScope())
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
                hasMore = false;
                Log.Information("Has processed all InventoryCreated Skus/batches");
                break;
            }
            // Try to create OfferItem. 
            // if successful, INVETORYCREATED -> OfferCreated
            // If not either PENDING -> REJECTED or PENDING -> FAILED denpending on failure reasons
            foreach (var skuId in inventoryCreatedSkuIds)
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var sku = await appDbContext.Skus.FindAsync(skuId);
                    var result = await ebayOfferService.CreateOffer(sku: sku.SkuCode, categoryId,sku.SellPrice,"DEFAULT_LOCATION","255527709013","255527791013","255527716013");
                    // Mark SKU after createing offer
                    if (result.Status == SkuStatuses.Failed)
                    {
                        sku.SkuStatus = SkuStatuses.Failed;
                        await appDbContext.SaveChangesAsync();
                    }
                    else if (result.Status == SkuStatuses.OfferCreated)
                    {
                        sku.SkuStatus = SkuStatuses.OfferCreated;
                        sku.OfferId = result.OfferId;
                        await appDbContext.SaveChangesAsync();
                    }
                }
            }
        }
    }


        // These extract Variant Sku from PIDs.
        static async Task CleanSku(IServiceScopeFactory scopeFactory, CJApiClient cjClient, DeepSeekClient deepSeekClient, string ebayCategoryId, int batchSize = 20, int? maxBatches = null)
        {

            // Get required and recommended aspects
            var requiredAspects = await Helper.LoadAspectsForPrompt(ebayCategoryId: ebayCategoryId, aspect: "RequiredAspects");
            var recommendedAspects = await Helper.LoadAspectsForPrompt(ebayCategoryId: ebayCategoryId, aspect: "RecommendedAspects");
            var categoryName = await Helper.GetEbayCategoryName(ebayCategoryId);

            int batchNumber = 0;
            bool hasMore = true;

            while (hasMore && (maxBatches == null || batchNumber < maxBatches))
            {
                batchNumber++;
                Log.Information($"Processing batch {batchNumber}...");

                // Get next batch of unprocessed SKUs
                List<int> dirtySkuIds = new List<int>();
                using (var scope = scopeFactory.CreateScope())
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
                    break;
                }

                await ProcessBatch(scopeFactory, dirtySkuIds, cjClient, deepSeekClient, requiredAspects, recommendedAspects, categoryName);
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
            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var dirtySku = await context.DirtySkus.FindAsync(dirtySkuId);
                Log.Information($"Fetching product info for sku {dirtySku.Sku}");
                productInfo = await cjClient.GetProductDetailAsync(dirtySku.Sku, isProductSku: true);

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
            }                
            foreach (var variant in productInfo.Data.Variants)
            {
                await ProcessVariant(scopeFactory, variant, dirtySkuId, productInfo, cjClient, deepSeekClient, requiredAspectsForPrompt, recommendedAspectsForPrompt, categoryName);       
                break; // Only process 1 variant per product processed      
            }
                
        
        }   
        
    

    private static async Task ProcessVariant(IServiceScopeFactory scopeFactory,CjVariant variant, int dirtySkuId,
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
            var isAvailableUs = stockResult.Data?.Any(w => w.CountryCode.Contains("US")) ?? false;

            if (!isAvailableUs)
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
            await SaveValidatedSku(scopeFactory, variant, dirtySkuId, productInfo, aiResult);
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

    private static async Task SaveValidatedSku(IServiceScopeFactory scopeFactory,CjVariant variant, int dirtySkuId, CjProductDetailResponse productInfo, AiEnrichmentResult aiResult)
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
            SkuStatus = SkuStatuses.Pending,
            CreatedAt = DateTime.UtcNow
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
    static async Task RunCatalogCrawler(IServiceScopeFactory scopeFactory,CJApiClient cjClient, int ebayCategoryId, string productNameEn)
    {
        var pids = await cjClient.GetPids(scopeFactory,ebayCategoryId, productNameEn, Helper.IsLikelyShoeOrganizer);
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
    





