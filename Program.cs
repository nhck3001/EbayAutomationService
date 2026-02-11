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
    static async Task Main(string[] args)
    {
        // Load .env file
        Env.Load("/Users/nhck3001/Documents/GitHub/EbayAutomationService/file.env");
        var cjRefreshToken = Environment.GetEnvironmentVariable("CJ_REFRESH_TOKEN");
        var CjAuthService = new CjAuthService(cjRefreshToken!);
        var cjTokenManager = new CjTokenManager(CjAuthService);
        var cjClient = new CJApiClient(cjTokenManager);
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
                await RunCatalogCrawler(cjClient);
                break;

            case "skus":
                await ExtractVariantSku(cjClient);
                break;
            
            case "getUsSku":
                await GetUsSku(cjClient);
                break;


            default:
                Console.WriteLine("Unknown command");
                break;
        }

    }
    public static async Task GetUsSku(CJApiClient cjClient)
    {
        var inputFile = "/Users/nhck3001/Documents/GitHub/EbayAutomationService/variantSku.txt";
        var outputFile = "/Users/nhck3001/Documents/GitHub/EbayAutomationService/qualifiedSkus.txt";
        var progressFile = "/Users/nhck3001/Documents/GitHub/EbayAutomationService/qualified_progress.txt";

        if (!File.Exists(inputFile))
        {
            Console.WriteLine("variantSku.txt not found.");
            return;
        }

        var allSkus = await File.ReadAllLinesAsync(inputFile);

        // Load existing qualified SKUs (avoid duplicates)
        var qualifiedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(outputFile))
        {
            foreach (var line in await File.ReadAllLinesAsync(outputFile))
            {
                var s = line.Trim();
                if (!string.IsNullOrWhiteSpace(s))
                    qualifiedSet.Add(s);
            }
        }

        // Resume support
        int startIndex = 0;
        if (File.Exists(progressFile))
            int.TryParse(await File.ReadAllTextAsync(progressFile), out startIndex);

        Console.WriteLine($"Starting at index {startIndex}");

        for (int i = startIndex; i < allSkus.Length; i++)
        {
            var sku = allSkus[i].Trim();

            if (string.IsNullOrWhiteSpace(sku))
                continue;

            try
            {
                Console.WriteLine($"[{i+1}/{allSkus.Length}] Checking {sku}");

                var stock = await cjClient.GetStockBySkuAsync(sku);

                if (stock?.Data == null)
                {
                    Console.WriteLine("Null stock response — retrying...");
                    await Task.Delay(2500);
                    i--;
                    continue;
                }

                // QUALIFICATION RULE
                bool hasUsStock = stock.Data.Any(w =>
                    w.CountryCode == "US" &&
                    w.TotalInventoryNum.HasValue &&
                    w.TotalInventoryNum.Value > 0);

                if (hasUsStock && qualifiedSet.Add(sku))
                {
                    await File.AppendAllLinesAsync(outputFile, new[] { sku });
                    Console.WriteLine($"US STOCK ✔ {sku}");
                }

                // checkpoint every 20
                if (i % 20 == 0)
                    await File.WriteAllTextAsync(progressFile, i.ToString());

                await Task.Delay(850); // CJ safe rate
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error on {sku}: {ex.Message}");
                await Task.Delay(5000);
                i--;
            }
        }

        Console.WriteLine("Qualification job complete.");
    }

    // These extract Variant Sku from PIDs. Only variants within 15-80 is allowed
    static async Task ExtractVariantSku(CJApiClient cjClient)
    {
        var pidFile = "/Users/nhck3001/Documents/GitHub/EbayAutomationService/productPid.txt";
        var progressFile = "/Users/nhck3001/Documents/GitHub/EbayAutomationService/skuProgress.txt";

        if (!File.Exists(pidFile))
        {
            Console.WriteLine("No PID file found.");
            return;
        }

        var existingVariantSkus = await LoadVariantSkusAsync();
        var allPids = await File.ReadAllLinesAsync(pidFile);

        int startIndex = 0;
        if (File.Exists(progressFile))
            int.TryParse(await File.ReadAllTextAsync(progressFile), out startIndex);

        for (int i = startIndex; i < allPids.Length; i++)
        {
            var pid = allPids[i].Trim();
            if (string.IsNullOrWhiteSpace(pid))
                continue;

            try
            {
                Console.WriteLine($"[{i + 1}/{allPids.Length}] Fetching {pid}");

                var response = await cjClient.GetProductDetailAsync(pid);

                if (response?.Data?.Variants == null)
                {
                    await Task.Delay(3000);
                    i--;
                    continue;
                }

                var goodSkus = response.Data.Variants
                    .Where(v =>
                        v != null &&
                        !string.IsNullOrWhiteSpace(v.VariantSku) &&
                        v.VariantSellPrice >= 15m &&
                        v.VariantSellPrice <= 80m)
                    .Select(v => v!.VariantSku!);

                await SaveNewVariantSkusAsync(goodSkus, existingVariantSkus);


                await File.WriteAllTextAsync(progressFile, i.ToString());

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                await Task.Delay(5000);
                i--;
            }
        }
    }


    static async Task RunCatalogCrawler(CJApiClient cjClient)
    {
        var productPidPath = "/Users/nhck3001/Documents/GitHub/EbayAutomationService/productPid.txt";
        var statePath = "/Users/nhck3001/Documents/GitHub/EbayAutomationService/crawlState.json";

        var state = await CrawlState.LoadCrawlStateAsync(statePath);

        int startPage = state.LastEndPage + 1;
        int endPage = startPage + 49;

        Console.WriteLine($"Crawling pages {startPage} → {endPage}");

        var pids = await cjClient.Get2500Pids(startPage,endPage,
            async (completedPage) =>
            {
                state.LastEndPage = completedPage;
                await CrawlState.SaveCrawlStateAsync(statePath, state);
            },
            async (discoveredPagePids) =>
            {
                await AppendNewPidsAsync(productPidPath, discoveredPagePids);
            });
    }


    public static async Task<HashSet<string>> LoadVariantSkusAsync()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists("/Users/nhck3001/Documents/GitHub/EbayAutomationService/variantSku.txt"))
            return set;

        var lines = await File.ReadAllLinesAsync("/Users/nhck3001/Documents/GitHub/EbayAutomationService/variantSku.txt");

        foreach (var line in lines)
        {
            var sku = line.Trim();
            if (!string.IsNullOrWhiteSpace(sku))
                set.Add(sku);
        }

        Console.WriteLine($"Loaded {set.Count} existing variant SKUs");
        return set;
    }
    public static async Task SaveNewVariantSkusAsync(IEnumerable<string> skus,HashSet<string> existingSet)
    {
        var newOnes = new List<string>();

        foreach (var sku in skus)
        {
            if (string.IsNullOrWhiteSpace(sku))
                continue;

            var clean = sku.Trim();

            // HashSet.Add returns true only if not already present
            if (existingSet.Add(clean))
                newOnes.Add(clean);
        }

        if (newOnes.Count == 0)
            return;

        await File.AppendAllLinesAsync("/Users/nhck3001/Documents/GitHub/EbayAutomationService/variantSku.txt", newOnes);

        Console.WriteLine($"Saved {newOnes.Count} new variant SKUs");
    }

    public static async Task AppendNewPidsAsync(string filePath, IEnumerable<string> newPids)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        // Load existing PIDs into a Hashset to allow fast look up and remove duplicate
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(filePath))
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            foreach (var line in lines)
            {
                var pid = line.Trim();
                if (!string.IsNullOrWhiteSpace(pid))
                    existing.Add(pid);
            }
        }

        // Find only new PIDs
        var toAppend = new List<string>();
        // Look through newly found Pids, append them if they are not already appended
        foreach (var pid in newPids)
        {
            if (string.IsNullOrWhiteSpace(pid))
                continue;
            var clean = pid.Trim();
            if (existing.Add(clean)) // HashSet.Add returns false if duplicate
                toAppend.Add(clean);
        }

        if (toAppend.Count == 0)
        {
            Console.WriteLine("No new PIDs discovered.");
            return;
        }

        await File.AppendAllLinesAsync(filePath, toAppend);

        Console.WriteLine($"Added {toAppend.Count} new PIDs (Total known: {existing.Count})");
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
    public static async Task ExportProductSkusAsync(List<CjProductDetail> products, string filePath)
    {
        if (products == null || products.Count == 0)
            return;

        // remove nulls + duplicates
        var skus = products
            .Where(p => !string.IsNullOrWhiteSpace(p.ProductSku))
            .Select(p => p.ProductSku!.Trim())
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        // ensure directory exists
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // write file
        await File.WriteAllLinesAsync(filePath, skus);

        Console.WriteLine($"Exported {skus.Count} SKUs to {filePath}");
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



