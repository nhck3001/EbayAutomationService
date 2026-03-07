using EbayAutomationService.Domain;
using EbayAutomationService.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using Serilog;

// This class will look at orders on ebay
// Check if Cj has inventory of the sku
// If it does, update listing quantity =>1. Will automatically update quantity, don't neede to republish
public class RefreshInventory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EbayOfferApiClient _ebayOfferApiClient;
    private readonly CJApiClient _cjApiClient;

    private readonly int batchSize = 20;

    public RefreshInventory(IServiceScopeFactory scopeFactory, EbayOfferApiClient ebayOfferApiClient, CJApiClient cJApiClient)
    {
        _scopeFactory = scopeFactory;
        _ebayOfferApiClient = ebayOfferApiClient;
        _cjApiClient = cJApiClient;
    }

    // Right now, the function will take in an sku
    // In the future, will automatically trigger using webhook
    public async Task ProcessBatchAsync(string sku, CancellationToken stoppingToken)
    {
        Log.Information($"Refresh Inventory offer {sku}. Checking US availability");
        // Check if product is available in the us
        var usStock = await CjHelper.GetUsStock(sku, _cjApiClient, stoppingToken);

        if (usStock == 0)
        {
            Log.Information($"Can't refresh Inventory {sku}. OUT OF STOCK");
            return;
        }
        else
        {
            Log.Information($"Current US Stock {sku} {usStock}");

            using (var scope = _scopeFactory.CreateScope())
            {
                var ebayOfferClient = scope.ServiceProvider.GetRequiredService<EbayOfferApiClient>();
                var ebayInventoryClient = scope.ServiceProvider.GetRequiredService<EbayInventoryApiClient>();
                // Get the offer and inventory object as Jtoken
                var offerObject = await ebayOfferClient.getOffers(sku, stoppingToken);
                var inventoryItem = await ebayInventoryClient.GetSku(sku,stoppingToken);
                // Update quantity
                offerObject["availableQuantity"] = 1;
                // product description is required even if not changed
                var productDescription = inventoryItem["product"]["description"];
                offerObject["listingDescription"] = productDescription;

                var jsonBody = offerObject.ToString(Formatting.None);
                await ebayOfferClient.updateOffer(offerObject["offerId"].ToString(), jsonBody,stoppingToken);
                Log.Information($"Refresh Inventory {sku} successfully.");
            }        
        }
    }




}