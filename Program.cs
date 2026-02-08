using EbayAutomationService.Services;
using DotNetEnv;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualBasic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

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
        var ebauAuthTOken = Environment.GetEnvironmentVariable("EBAY_AUTH_TOKEN");
        var ebayDeveloperId = Environment.GetEnvironmentVariable("DEVELOPER_ID");
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

        foreach (var itemId in list1)
        {
            try
            {
                // Convert REST itemId → legacy numeric ID
                var legacyItemId = itemId.Contains("|") ? itemId.Split('|')[1] : itemId;

                var itemData = await GetItemViaTradingApi(legacyItemId, appId!, certId!, ebayDeveloperId!, ebauAuthTOken!);

                var images = itemData["Images"]?.ToObject<List<string>>() ?? new List<string>();

                var researchItem = new EbayItemResearchData
                {
                    ItemId = itemData["ItemID"]?.ToString(),
                    Title = itemData["Title"]?.ToString(),
                    CategoryId = itemData["CategoryID"]?.ToString(),
                    Currency = itemData["Currency"]?.ToString(),
                    Image = images.FirstOrDefault(),
                    AdditionalImages = images.Skip(1).ToList(),
                    ItemSpecifics = itemData["ItemSpecifics"]?.ToObject<Dictionary<string, List<string>>>() ?? new Dictionary<string, List<string>>()
                };

                if (decimal.TryParse(itemData["Price"]?.ToString(), out var price))
                {
                    researchItem.Price = price;
                }

                researchResults.Add(researchItem);

                // Be polite to Trading API
                await Task.Delay(300);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch item {itemId}: {ex.Message}");
            }
        }

        var outputPath = "/Users/nhck3001/Documents/GitHub/EbayAutomationService/item.json";

        var json = JsonConvert.SerializeObject(researchResults, Formatting.Indented);

        File.WriteAllText(outputPath, json);

        Console.WriteLine($"Saved {researchResults.Count} items to {outputPath}");


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
    public static async Task<JObject> GetItemViaTradingApi(string itemId, string appId, string certId, string devId, string ebayAuthToken)
    {
        var endpoint = "https://api.ebay.com/ws/api.dll";

        var requestXml = $@"
    <?xml version=""1.0"" encoding=""utf-8""?>
    <GetItemRequest xmlns=""urn:ebay:apis:eBLBaseComponents"">
    <RequesterCredentials>
        <eBayAuthToken>{ebayAuthToken}</eBayAuthToken>
    </RequesterCredentials>
    <ItemID>{itemId}</ItemID>
    <DetailLevel>ReturnAll</DetailLevel>
    <IncludeItemSpecifics>true</IncludeItemSpecifics>
    </GetItemRequest>";

        using var client = new HttpClient();

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(requestXml, Encoding.UTF8, "text/xml")
        };

        // Required Trading API headers
        request.Headers.Add("X-EBAY-API-CALL-NAME", "GetItem");
        request.Headers.Add("X-EBAY-API-SITEID", "0"); // 0 = US
        request.Headers.Add("X-EBAY-API-COMPATIBILITY-LEVEL", "967");
        request.Headers.Add("X-EBAY-API-DEV-NAME", devId);
        request.Headers.Add("X-EBAY-API-APP-NAME", appId);
        request.Headers.Add("X-EBAY-API-CERT-NAME", certId);

        var response = await client.SendAsync(request);
        var responseXml = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Trading API error: {responseXml}");
        }

        return ConvertGetItemResponseToJson(responseXml);
    }

    private static JObject ConvertGetItemResponseToJson(string xml)
    {
        var doc = XDocument.Parse(xml);
        XNamespace ns = "urn:ebay:apis:eBLBaseComponents";

        var item = doc.Descendants(ns + "Item").FirstOrDefault();
        if (item == null)
            throw new Exception("Item node not found");

        var result = new JObject
        {
            ["ItemID"] = item.Element(ns + "ItemID")?.Value,
            ["Title"] = item.Element(ns + "Title")?.Value,
            ["Description"] = item.Element(ns + "Description")?.Value,
            ["CategoryID"] = item.Element(ns + "PrimaryCategory")?.Element(ns + "CategoryID")?.Value,
            ["Price"] = item.Element(ns + "SellingStatus")?.Element(ns + "CurrentPrice")?.Value,
            ["Currency"] = item.Element(ns + "SellingStatus")?.Element(ns + "CurrentPrice")?.Attribute("currencyID")?.Value
        };

        // Item Specifics
        var specifics = new JObject();
        foreach (var nv in item.Descendants(ns + "NameValueList"))
        {
            var name = CleanUnicode(nv.Element(ns + "Name")?.Value);

            var values = nv.Elements(ns + "Value").Select(v => CleanUnicode(v.Value)).ToList();

            if (!string.IsNullOrEmpty(name))
                specifics[name] = JArray.FromObject(values);
        }

        result["ItemSpecifics"] = specifics;

        // Images
        var images = item.Descendants(ns + "PictureURL").Select(p => p.Value).ToList();
        result["Images"] = JArray.FromObject(images);

        return result;
    }
    
    public static string CleanUnicode(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder(input.Length);

        foreach (var ch in input)
        {
            var category = Char.GetUnicodeCategory(ch);

            // Remove formatting/control characters like U+200E
            if (category != UnicodeCategory.Format)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Trim();
    }


}



