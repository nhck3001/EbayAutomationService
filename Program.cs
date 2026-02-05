using EbayAutomationService.Services;
using DotNetEnv;
using System.ComponentModel.DataAnnotations;

class Program
{
    static async Task Main()
    {
        // Load .env file
        Env.Load("/Users/nhck3001/Documents/GitHub/EbayAutomationService/file.env");
        // Load environment variables
        var cjRefreshToken = Environment.GetEnvironmentVariable("CJ_REFRESH_TOKEN");
        //var appId = Environment.GetEnvironmentVariable("APP_ID");
        //var certId = Environment.GetEnvironmentVariable("CLIENT_SECRET");
        //var refreshToken = Environment.GetEnvironmentVariable("EBAY_REFRESH_TOKEN");
        //
        //// Get the access token by using the EbayAuthService object
        //Console.WriteLine("\n----------Requesting access token...----------");
        //var ebayAuthService = new EbayAuthService(appId!, certId!, refreshToken!);
        //var tokenManager = new EbayTokenManager(ebayAuthService);
        //var httpClient = new HttpClient();
        //var ebayAPIClient = new EbayApiClient(httpClient, tokenManager);
        //
        //// Making call to the EbayUserService API to retrieve user info
        //var userService = new EbayUserService(ebayAPIClient);
        //var userInfo = await userService.GetUserInfo();
        //
        //// Making call to the EbayListingService to retrieve/edit listing info
        //// If the lisitng count is 0, create 500 listing for testing purposes
        //var accountService = new EbayAccountService(ebayAPIClient);
        //var categoryService = new EbayCategoryService(ebayAPIClient);
        //var policyService = new EbayPolicyService(ebayAPIClient);
        //var inventoryService = new EbayInventoryService(ebayAPIClient);
        //var offerService = new EbayOfferService(ebayAPIClient);

        // for CJ
        var cjAuthService = new CjAuthService(cjRefreshToken!);
        var cjTokenManager = new CjTokenManager(cjAuthService);
        var cjApiClient = new CJApiClient(cjTokenManager);
        var products = await cjApiClient.GetUsWarehouseProductsAsync();

        var first = products.Data.List.First();

        //Console.WriteLine($"PID: {first.Pid}");
        //Console.WriteLine($"Name: {first.ProductName}");
//
        //var detail = await cjApiClient.GetProductDetailAsync(first.Pid);
        //var stock = await cjApiClient.GetProductStockAsync(first.Pid);
//
        //Console.WriteLine($"Variants: {stock.Data.Count}");
    }
}

