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
using static EbayAutomationService.Helper.Helper;
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
                services.AddScoped<EbayOfferApiClient>();
                services.AddScoped<EbayPolicyService>();
                services.AddScoped<CreateInventoryUseCase>();
                services.AddScoped<CreateOfferUseCase>();
                services.AddScoped<CrawlerUseCase>();
                services.AddScoped<CleanSkuUseCase>();
                services.AddScoped<PublishOfferUseCase>();
                services.AddScoped<CJRateLimiter>();

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
                    var crawlUseCase = scope.ServiceProvider.GetRequiredService<CrawlerUseCase>();
                    await crawlUseCase.CrawlProductsAsync();
                }
                break;

            //Get SKU with good price + in the US
            case "cleansku":
                using (var scope = host.Services.CreateScope())
                {
                    var CleanSkuUseCase = scope.ServiceProvider.GetRequiredService<CleanSkuUseCase>();
                    await CleanSkuUseCase.ExecuteAsync();
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
                    var useCase = scope.ServiceProvider.GetRequiredService<CreateOfferUseCase>();
                    await useCase.ExecuteAsync("45306");
                }
                break;

            case "publishoffer":
                Log.Information("Start to publish offer");
                using (var scope = host.Services.CreateScope())
                {
                    var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
                    var publishOfferUseCase = scope.ServiceProvider.GetRequiredService<PublishOfferUseCase>();
                    await publishOfferUseCase.ExecuteAsync();
                }
                break;
            case "test":
                List<int> skuId = [];
                CJApiClient cjClient = null;
                using (var scope = host.Services.CreateScope())
                {
                    cjClient = scope.ServiceProvider.GetRequiredService<CJApiClient>();
                }
                using (var scope = host.Services.CreateScope())
                {

                    var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    skuId = await appDbContext.Skus
                    .Where(sku => sku.SkuStatus == SkuStatuses.Pending)
                    .OrderBy(sku => sku.Id)  // Add ordering for consistent paging
                    .Select(s => s.Id)
                    .ToListAsync();
                }
                    foreach (var id in skuId)
                    {
                    using (var scope = host.Services.CreateScope())
                    {
                        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var sku = await appDbContext.Skus.FindAsync(id);
                        var stockResult = await cjClient.GetStockBySkuAsync(sku.SkuCode!);
                        var cjInventory = stockResult.Data.Where(w => w.CountryCode.Contains("US")).FirstOrDefault().CjInventoryNum ?? 0;
                        sku.avilableInventory = cjInventory;
                        await appDbContext.SaveChangesAsync();                 
                        }                   
                    }
                
                break;
        }

    }

}


    





