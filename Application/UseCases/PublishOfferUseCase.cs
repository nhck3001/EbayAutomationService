using EbayAutomationService.Domain;
using EbayAutomationService.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using Serilog;

public class PublishOfferUseCase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EbayOfferApiClient _ebayOfferApiClient;

    private readonly int batchSize = 20;

    public PublishOfferUseCase(IServiceScopeFactory scopeFactory,EbayOfferApiClient ebayOfferApiClient)
    {
        _scopeFactory = scopeFactory;
        _ebayOfferApiClient = ebayOfferApiClient;
    }

    public async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {

        List<int> offerItemIds;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            offerItemIds = await db.OfferItems
                .Where(o => o.Status == OfferStatus.Pending)
                .OrderBy(o => o.Id)
                .Take(batchSize)
                .Select(o => o.Id)
                .ToListAsync(stoppingToken);
        }
        if (offerItemIds.Count == 0)
        {
            Log.Information("No more offers to publish.");
            return;
        }
        foreach (var offerItemId in offerItemIds)
        {
            await ProcessSingle(offerItemId, stoppingToken);
        }

        
    }

    private async Task ProcessSingle(int offerItemId, CancellationToken stoppingToken)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var offerItem = await appDbContext.OfferItems
                .Include(o => o.Inventory)
                .FirstOrDefaultAsync(o => o.Id == offerItemId,stoppingToken);

            if (offerItem == null)
                return;

            Log.Information($"Publishing offer {offerItem.OfferId}");

            var result = await _ebayOfferApiClient.publishOffer(offerItem.OfferId, stoppingToken);

            try
            {

                // If publish succeeded, create Listing entity (1:1 model)
                if (result.Outcome == OperationOutcome.Success || result.Outcome == OperationOutcome.AlreadyExists)
                {
                    offerItem.Status = OfferStatus.ListingCreated;
                    var exists = await appDbContext.Listings.AnyAsync(l => l.OfferId == offerItem.Id, stoppingToken);
                    if (!exists)
                    {
                        var listingEntity = new Listing
                        {
                            listingId = result.Value, // eBay listingId
                            OfferId = offerItem.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        appDbContext.Listings.Add(listingEntity);        
                    }
                    Log.Information($"Published offer {offerItem.OfferId} successfully");
                    await appDbContext.SaveChangesAsync(stoppingToken);
                }
                else if (result.Outcome == OperationOutcome.InvalidData)
                {
                    offerItem.Status = OfferStatus.Failed;
                    Log.Information($"Publish offer failed for {offerItem.OfferId}. {result.RawMessage}");
                    await appDbContext.SaveChangesAsync(stoppingToken);

                }
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                Log.Information($" SKip duplicate listing {result.Value}");
                return;
            }
            catch (DbUpdateException ex)
            {
                Log.Warning(ex.Message);
                throw;
            }
        }

    }
}