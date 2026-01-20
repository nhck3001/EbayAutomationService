using EbayAutomationService.Services;
using DotNetEnv;
using System.ComponentModel.DataAnnotations;
class Program
{
    static async Task Main()
    {
        // Load .env file
        Env.Load("/Users/nhck3001/Documents/EbayProject/EbayAutomationService/file.env");
        // Load environment variables
        var appId = Environment.GetEnvironmentVariable("APP_ID");
        var certId = Environment.GetEnvironmentVariable("CLIENT_SECRET");
        var refreshToken = Environment.GetEnvironmentVariable("EBAY_REFRESH_TOKEN");

        // Get the access token by using the EbayAuthService object
        Console.WriteLine("\n----------Requesting access token...----------");
        var ebayAuthService = new EbayAuthService(appId!, certId!, refreshToken!);
        var tokenManager = new EbayTokenManager(ebayAuthService);
        var httpClient = new HttpClient();
        var ebayAPIClient = new EbayApiClient(httpClient, tokenManager);

        // Making call to the EbayUserService API to retrieve user info
        var userService = new EbayUserService(ebayAPIClient);
        var userInfo = await userService.GetUserInfo();

        // Making call to the EbayListingService to retrieve/edit listing info
        // If the lisitng count is 0, create 500 listing for testing purposes
        var accountService = new EbayAccountService(ebayAPIClient);
        var categoryService = new EbayCategoryService(ebayAPIClient);
        var policyService = new EbayPolicyService(ebayAPIClient);
        var inventoryService = new EbayInventoryService(ebayAPIClient);
        var offerService = new EbayOfferService(ebayAPIClient);

        //var defaultCategoryTreeId = await categoryService.getDefaultCategoryTreeID();
        //var suggesstedCategoryId = await categoryService.getSuggesstedCategory(defaultCategoryTreeId, "Iphone6");
        //
        //var fulfillmentPolicyId = await policyService.getFulfillmentPolicyIDs();
        //var paymentPolicyId = await policyService.getPaymentPolicyIDByName("MyPaymentPolicy");
        //var returnPolicyId = await policyService.getReturnPolicyIDByName("m********e");
        //var allSku = await inventoryService.getAllSku();
        ////for (int x = 0; x <= 50; x++)
        ////{
        ////    await listingService.CreateInventoryItem(x.ToString());
        ////}
        ////10592561010
        //var merchangeLocationKey = await inventoryService.getInventoryLocations();
        //await offerService.updateOffer("10592561010", fulfillmentPolicyId, paymentPolicyId, returnPolicyId, merchangeLocationKey);
        //await offerService.publishOffer("10592561010");
        //var x = await offerService.getOffers("1");
        var x = await accountService.getOptedInPrograms();
    }   
}

