using EbayAutomationService.Helper;
using EbayAutomationService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Serilog;

public class FetchOrderUseCase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EbayFulfillmentApi _ebayFulfillmentApi;

    public FetchOrderUseCase(IServiceScopeFactory scopeFactory,EbayFulfillmentApi ebayFulfillmentApi)
    {
        _scopeFactory = scopeFactory;
        _ebayFulfillmentApi = ebayFulfillmentApi;
    }
    // Fetch order in batch of 20
    public async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {

        try
        {
            var pendingOrders = await _ebayFulfillmentApi.GetPendingOrders(stoppingToken);
            // Process each order
            foreach (var order in pendingOrders)
            {
                await ProcessSingleOrderAsync(order, stoppingToken);
            }
        }
        
        catch (CjDailyLimitException)
        {
            Log.Information("Daily limit reached. Throwing up to worker");
            throw;
        }

    }
    // Fetch details of fields
    private async Task ProcessSingleOrderAsync(EbayOrderResponse order, CancellationToken stoppingToken)
    {

            var now = DateTime.UtcNow;
            var ebayOrder = new EbayOrder
            {
                EbayOrderId = order.OrderId,
                PurchaseDate = order.CreationDate,
                BuyerUsername = order.Buyer.Username,
                BuyerFullName = order.GetBuyerFullName,
                AddressLine1 = order.GetAddressLine1,
                AddressLine2 = order.GetAddressLine2,
                City = order.GetCity,
                State = order.GetStateOrProvince,
                PostalCode = order.GetPostalCode,
                Country = order.GetCountryCode,
                Phone = order.GetPhoneNumber,
                Email = order.GetEmail,
                OrderPaymentStatus = order.OrderPaymentStatus,
                OrderFulfillmentStatus = order.OrderFulfillmentStatus,
                Status = "NEW",
                CreatedAt = now,
                UpdatedAt = now
            };
        var lineItems = new List<LineItem>();
        foreach (var item in order.LineItems)
        {
            var lineItem = new LineItem
            {
                Order = ebayOrder,
                LineItemId = item.LineItemId,
                Sku = item.Sku,
                Quantity = item.Quantity,
                Status = LineItemStatus.Pending
            };
            lineItems.Add(lineItem);
        }
        ebayOrder.OrderItems = lineItems;

        await SaveSingleOrderAsync(ebayOrder, stoppingToken);
        // Save order
    }

    private async Task SaveSingleOrderAsync(EbayOrder order, CancellationToken stoppingToken)
    {

        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {

                var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var exists = await appDbContext.EbayOrders.AnyAsync(o => o.EbayOrderId == order.EbayOrderId, stoppingToken);
                if (!exists)
                {
                    appDbContext.EbayOrders.Add(order);
                    await appDbContext.SaveChangesAsync(stoppingToken);

                    Log.Information($"Save Order Info {order.EbayOrderId} successfully");
                }
                else
                {
                    Log.Information($"Skip duplicate Order {order.EbayOrderId}");                    
                }
            }
        }

        catch (DbUpdateException ex)
        {
            // 23505 = duplicate key violation
            // Safe to ignore
            if (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                Log.Information("Duplicate key. Move on");
            }
            else
            {
                Log.Information($"{ex.InnerException.Message}");
                Log.Information($"{ex.InnerException.Data}");
                throw;
            }
        }
    }
}