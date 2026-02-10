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
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

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
        var credentials = $"{appId}:{certId}";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        Console.WriteLine(base64);
        // Get the access token by using the EbayAuthService object
        Console.WriteLine("\n----------Requesting access token...----------");
        var ebayAuthService = new EbayAuthService(appId!, certId!, refreshToken!);
        var ebayTokenManager = new EbayTokenManager(ebayAuthService);
        await ebayTokenManager.ForceRefreshAsync();
        var httpClient = new HttpClient();
        var ebayAPIClient = new EbayApiClient(httpClient, ebayTokenManager);
        var ebayBrowseService = new EbayBrowseApiService(ebayAPIClient);
        var ebayPolicyService = new EbayPolicyService(ebayAPIClient);
        var ebayInventoryService = new EbayInventoryService(ebayAPIClient);
        var ebayOfferService = new EbayOfferService(ebayAPIClient);
        var paymentPolicyId = await ebayPolicyService.GetPaymentPolicyId("PaymentPolicy");
        var fulfillmentPolicyId = await ebayPolicyService.GetFulfillmentPolicyId("ShippingPolicy");
        var returnPolicyId = await ebayPolicyService.GetReturnPolicyId("ReturnPolicyBuyerPay");

        var list1 = await ebayBrowseService.GetItemIdsBySeller("parwazcollections");
        //var list2 = await ebayBrowseService.GetItemIdsBySeller("parwazcollections", limit: 50, offset: 50);
        //var list3 = await ebayBrowseService.GetItemIdsBySeller("parwazcollections", offset: 100);
        //var list4 = await ebayBrowseService.GetItemIdsBySeller("parwazcollections", offset: 150);
        //var list5 = await ebayBrowseService.GetItemIdsBySeller("parwazcollections", offset: 200);
        //// Get all itemId in a list
        //var totalList = list1.Concat(list2).Concat(list3).Concat(list4).Concat(list5).Distinct().ToList();

        var jsonPath = "/Users/nhck3001/Documents/GitHub/EbayAutomationService/item.json";
        
        var allItems = JsonConvert.DeserializeObject<List<EbayItemResearchData>>(
            File.ReadAllText(jsonPath)
        );
        
        var testItems = allItems.Take(10).ToList();
        foreach (var item in testItems)
        {
            var sku = item.ItemId; // using itemId as ASIN / SKU
        
            // Create / Update Inventory Item
            await ebayInventoryService.CreateOrUpdateInventoryItem(
                sku,
                item.Title,
                item.Description,
                item.Images,
                item.ItemSpecifics,
                quantity: 5   // inventory pool
            );
        
            // 2️Create Offer
            var offerId = await ebayOfferService.CreateOffer(
                sku: sku,
                categoryId: item.CategoryId,
                price: item.Price,
                paymentPolicyId: paymentPolicyId,
                fulfillmentPolicyId: fulfillmentPolicyId,
                returnPolicyId: returnPolicyId,
                merchantLocationKey: "DEFAULT_LOCATION"
            );
        
            // Publish Offer
            await ebayOfferService.publishOffer(offerId);
            Console.WriteLine($"Published listing for SKU: {sku}");
        }

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
    public static string CleanEbayDescription(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Remove span tags but keep inner text
        html = Regex.Replace(html, @"<\/?span[^>]*>", "", RegexOptions.IgnoreCase);

        // Remove class attributes
        html = Regex.Replace(html, @"\sclass=""[^""]*""", "", RegexOptions.IgnoreCase);

        // Remove style attributes
        html = Regex.Replace(html, @"\sstyle=""[^""]*""", "", RegexOptions.IgnoreCase);

        // Remove unicode directional markers
        html = html
            .Replace("\u200E", "")
            .Replace("\u200F", "")
            .Replace("\u202A", "")
            .Replace("\u202B", "")
            .Replace("\u202C", "");

        return html.Trim();
    }
}



