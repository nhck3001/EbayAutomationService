using EbayAutomationService.Services;
using DotNetEnv;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualBasic;
using System.Linq;
using Newtonsoft.Json;

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
        var ebayTokenManager = new EbayTokenManager(ebayAuthService);
        var httpClient = new HttpClient();
        var ebayAPIClient = new EbayApiClient(httpClient, ebayTokenManager);
        var ebayBrowseService = new EbayBrowseApiService(ebayAPIClient);
        var list1 = await ebayBrowseService.GetItemIdsBySeller("parwazcollections");
        var list2 = await ebayBrowseService.GetItemIdsBySeller("parwazcollections", limit: 50, offset: 50);
        var list3 = await ebayBrowseService.GetItemIdsBySeller("parwazcollections", offset: 100);
        var list4 = await ebayBrowseService.GetItemIdsBySeller("parwazcollections", offset: 150);
        var list5 = await ebayBrowseService.GetItemIdsBySeller("parwazcollections", offset: 200);
        // Get all itemId in a list
        var totalList = list1.Concat(list2).Concat(list3).Concat(list4).Concat(list5).Distinct().ToList();
        var researchResults = new List<EbayItemResearchData>();

        foreach (var itemId in totalList)
        {
            var itemJson = await ebayBrowseService.GetItemByItemId(itemId);

            var researchData = EbayItemResearchData.ExtractResearchData(itemJson);
            researchResults.Add(researchData);

            await Task.Delay(200); // rate-limit safety
        }

        var outputPath = "/Users/nhck3001/Documents/GitHub/EbayAutomationService/item.json";
        var json = JsonConvert.SerializeObject(researchResults,Formatting.Indented);

        File.WriteAllText(outputPath, json);
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

