using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

public class CrawlWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CrawlWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("CrawlWorker started.");

        try
        {
            // Worker will execute every 10 seconds
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var processor = scope.ServiceProvider.GetRequiredService<CrawlerUseCase>();
                    await processor.ProcessBatchAsync(stoppingToken);
                }
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
        catch (CjDailyLimitException)
        {
            Log.Warning("Daily limit reached. Stopping CrawlWorker.");
            return; // exit worker cleanly
        }
        // Allow Ctrl+c to shut down gracefully
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception in InventoryCreationWorker.");
        }
        finally
        {
            Log.Information("CrawlWorker stopping.");
        }
    }
}