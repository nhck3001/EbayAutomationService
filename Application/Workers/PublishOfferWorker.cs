using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

public class PublishOfferWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PublishOfferWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("PublishOfferWorker started.");

        try
        {
            // Worker will execute every 10 seconds
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                { 
                    var processor = scope.ServiceProvider.GetRequiredService<PublishOfferUseCase>();
                    await processor.ProcessBatchAsync(stoppingToken);
                }
                await Task.Delay(TimeSpan.FromSeconds(120), stoppingToken);
            }
        }
        // Allow Ctrl+c to shut down gracefully
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception in PublishOfferWorker.");
        }
        finally
        {
            Log.Information("PublishOfferWorker stopping.");
        }
    }
}