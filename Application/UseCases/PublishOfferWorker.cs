using Microsoft.Extensions.Hosting;
using Serilog;

public class PublishOfferWorker : BackgroundService
{
    private readonly PublishOfferUseCase _processor;

    public PublishOfferWorker(PublishOfferUseCase processor)
    {
        _processor = processor;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("PublishOfferWorker started.");

        try
        {
            // Worker will execute every 10 seconds
            while (!stoppingToken.IsCancellationRequested)
            {
                await _processor.ProcessBatchAsync();
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
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