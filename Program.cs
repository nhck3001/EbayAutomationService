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
        var appId = Environment.GetEnvironmentVariable("APP_ID");
        var certId = Environment.GetEnvironmentVariable("CLIENT_SECRET");
        var refreshToken = Environment.GetEnvironmentVariable("EBAY_REFRESH_TOKEN");
        
        // Get the access token by using the EbayAuthService object
        Console.WriteLine("\n----------Requesting access token...----------");
        var ebayAuthService = new EbayAuthService(appId!, certId!, refreshToken!);
        var x = 10;
        
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

    }




}

