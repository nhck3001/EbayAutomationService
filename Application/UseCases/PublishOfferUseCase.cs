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

    public async Task ExecuteAsync()
    {
        int batchNumber = 0;

        while (true)
        {
            Log.Information($"Processing PublishOffer batch {batchNumber}...");

            List<int> offerItemIds;

            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                offerItemIds = await db.OfferItems
                    .Where(o => o.Status == OfferStatus.Pending)
                    .OrderBy(o => o.Id)
                    .Take(batchSize)
                    .Select(o => o.Id)
                    .ToListAsync();
            }

            if (offerItemIds.Count == 0)
            {
                Log.Information("No more offers to publish.");
                return;
            }

            foreach (var offerItemId in offerItemIds)
            {
                await ProcessSingle(offerItemId);
            }

            batchNumber++;
        }
    }

    private async Task ProcessSingle(int offerItemId)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var offerItem = await appDbContext.OfferItems
                .Include(o => o.Inventory)
                .FirstOrDefaultAsync(o => o.Id == offerItemId);

            if (offerItem == null)
                return;

            Log.Information($"Publishing offer {offerItem.OfferId}");

            var result = await _ebayOfferApiClient.publishOffer(offerItem.OfferId);

            switch (result.Outcome)
            {
                case OperationOutcome.Success:
                    Log.Information($"Published successfully: {offerItem.OfferId}");
                    offerItem.Status = OfferStatus.ListingCreated;
                    break;

                case OperationOutcome.AlreadyExists:
                    Log.Information($"Listing already exists for offer {offerItem.OfferId}");
                    offerItem.Status = OfferStatus.ListingCreated;
                    break;

                case OperationOutcome.InvalidData:
                    Log.Information($"Publish failed for {offerItem.OfferId}. {result.RawMessage}");
                    offerItem.Status = OfferStatus.Failed;
                    break;

                case OperationOutcome.RetryableFailure:
                    Log.Information($"Temporary failure for {offerItem.OfferId}. Will retry.");
                    return; // do NOT change status

            }
            await appDbContext.SaveChangesAsync();
            try
            {
                // If publish succeeded, create Listing entity (1:1 model)
                if (result.Outcome == OperationOutcome.Success || result.Outcome == OperationOutcome.AlreadyExists)
                {
                    var listingEntity = new Listing
                    {
                        listingId = result.Value, // eBay listingId
                        OfferId = offerItem.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    appDbContext.Listings.Add(listingEntity);
                    await appDbContext.SaveChangesAsync();
                }
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                Log.Information($"Duplicate listing {result.Value} - skipping");
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