using Newtonsoft.Json.Linq;
using Serilog;

namespace EbayAutomationService.Helper;

public class CjHelper
{
    
    /// <summary>
    /// Check if cj has us stock for a variantSku
    /// </summary>
    /// <param name="variantSku"></param>
    /// <returns>Number of us stock. If None, return 0</returns>
    public static  async Task<int> GetUsStock(string variantSku, CJApiClient cJApiClient)
    {
        var stockResult = await cJApiClient.GetStockBySkuAsync(variantSku);

        return stockResult.Data.FirstOrDefault(x => x.CountryCode.Contains("US"))?.CjInventoryNum ?? 0;
    }
    
}
